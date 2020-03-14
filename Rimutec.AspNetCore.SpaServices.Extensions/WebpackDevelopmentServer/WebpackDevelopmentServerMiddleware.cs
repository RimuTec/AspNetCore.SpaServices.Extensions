// Copyright 2020 (c) RimuTec Ltd. All rights reserved.
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// This file has been modified by Rimutec Ltd to use it for WebDevelopmentServer support in development.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.AspNetCore.SpaServices.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RimuTec.AspNetCore.SpaServices.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RimuTec.AspNetCore.SpaServices.WebpackDevelopmentServer
{
    /// <summary>
    /// This class is very loosely based on the code base of class ReactDevelopmentServerMiddleware
    /// in NuGet package Microsoft.AspNetCore.SpaServices.Extensions
    /// </summary>
    internal static class WebpackDevelopmentServerMiddleware
    {
        private const string LogCategoryName = nameof(RimuTec)
                                        + "." + nameof(AspNetCore)
                                        + "." + nameof(SpaServices)
                                        + "." + nameof(WebpackDevelopmentServerMiddleware);
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException("Must set ISpaBuilder.Options.SourcePath before calling this method.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            if (string.IsNullOrEmpty(npmScriptName))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException("Cannot be null or empty", nameof(npmScriptName));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);

            // Clear files in distribution directory aka spaStaticFileOptions.RootPath:
            var spaStaticFileProvider = spaBuilder.ApplicationBuilder.ApplicationServices.GetService(typeof(ISpaStaticFileProvider)) as ISpaStaticFileProvider;
            ClearSpaRootPath(spaStaticFileProvider.FileProvider, logger);

            // Start webpack-dev-server once the application has started
            var hostApplicationLifetime = spaBuilder.ApplicationBuilder.ApplicationServices.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime;
            Task<int> portTask = null;
            Task<Uri> targetUriTask = null;
            var socketPortNumber = 0;
            hostApplicationLifetime.ApplicationStarted.Register(() =>
            {
                // When this is called the request pipeline configuration has completed. Only now the addresses
                // at which requests are served are available. We use any address/port combination but use HTTPs
                // if is configured for the project.
                var addressFeature = spaBuilder.ApplicationBuilder.ServerFeatures.Get<IServerAddressesFeature>();
                foreach (var addr in addressFeature.Addresses)
                {
                    var uri = new Uri(addr);
                    socketPortNumber = uri.Port;
                    if (uri.Scheme == "https")
                    {
                        break;
                    }
                }

                portTask = StartWebpackDevServerAsync(sourcePath, npmScriptName, logger, socketPortNumber);
                // Everything we **proxy** is hardcoded to target http://localhost because:
                // - the requests are always from the local machine (we're not accepting remote
                //   requests that go directly to the webpack-dev-server)
                // - given that, there's no reason to use https when forwarding the request to the webpack
                //   dev server, and we couldn't even if we wanted to, because in general the webpack-dev-server 
                //   has no certificate
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                targetUriTask = portTask.ContinueWith(task =>
                {
                    // "https" here doesn't work as the webpack-dev-server expects request via "http"
                    Uri uri = new UriBuilder("http", "localhost", task.Result).Uri;
                    return uri;
                });
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
            });

            // Configure proxying. By the time a request comes in, the webpack dev server will be running,
            // so it is fine to configure proxying before the webpack-dev-server has been started.
            SpaProxyingExtensions.UseProxyToSpaDevelopmentServer(spaBuilder, () =>
            {
                // On each request, we create a separate startup task with its own timeout. That way, even if
                // the first request times out, subsequent requests could still work.
                var timeout = spaBuilder.Options.StartupTimeout;
                return targetUriTask.WithTimeout(timeout,
                    $"The webpack-dev-server did not start listening for requests " +
                    $"within the timeout period of {timeout.Seconds} seconds. " +
                    $"Check the log output for error information.");
            });
        }

        private static async Task<int> StartWebpackDevServerAsync(
            string sourcePath, string npmScriptName, ILogger logger, int socketPortNumber)
        {
            var portNumber = TcpPortFinder.FindAvailablePort();
            logger.LogInformation($"Starting webpack-dev-server on port {portNumber}...");

            // Use option "--stdin" so nodejs process with webpack-dev-server stops when the webapp stops:
            // https://webpack.js.org/configuration/dev-server/#devserverstdin---cli-only
            var arguments = $"--port {portNumber} --sockPort {socketPortNumber} --stdin";
            var envVars = new Dictionary<string, string> {};
            var npmScriptRunner = new NpmScriptRunner(
                sourcePath, npmScriptName,
                arguments,
                envVars);
            npmScriptRunner.AttachToLogger(logger);

            using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
            {
                try
                {
                    // Although the webpack-dev-server may eventually tell us the URL it's listening on,
                    // it doesn't do so until it's finished compiling, and even then only if there were
                    // no compiler warnings. So instead of waiting for that, consider it ready as soon
                    // as it starts listening for requests.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                    await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("Project is running at", 
                        RegexOptions.None, RegexMatchTimeout));
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
                }
                catch (EndOfStreamException ex)
                {
                    throw new InvalidOperationException(
                        $"The NPM script '{npmScriptName}' exited without indicating that the " +
                        $"webpack-development server was listening for requests. The error output was: " +
                        $"{stdErrReader.ReadAsString()}", ex);
                }
            }

            return portNumber;
        }

        private static void ClearSpaRootPath(IFileProvider spaStaticFileProvider, ILogger logger)
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            logger.LogInformation("Deleting SPA static file root content...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            foreach (var item in spaStaticFileProvider.GetDirectoryContents(string.Empty))
            {
                logger.LogInformation($"Deleting SPA static file or directory {item.PhysicalPath}");
                Console.WriteLine($"Item: {item.PhysicalPath}");
                if (item.IsDirectory)
                {
                    ClearFolder(item.PhysicalPath);
                }
                else
                {
                    var fi = new FileInfo(item.PhysicalPath);
                    fi.Delete();
                }
            }
        }

        private static void ClearFolder(string dist)
        {
            var dir = new DirectoryInfo(dist);

            foreach(var fi in dir.EnumerateFiles()) 
            {
                fi.Delete();
            }
            foreach(var di in dir.EnumerateDirectories())
            {
                ClearFolder(di.FullName);
                di.Delete();
            }
        }
    }
}
