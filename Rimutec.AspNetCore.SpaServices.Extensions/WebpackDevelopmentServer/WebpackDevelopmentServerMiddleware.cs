// Copyright 2020 (c) RimuTec Ltd. All rights reserved.
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// This file has been modified by Rimutec Ltd to use it for WebDevelopmentServer support in development.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;
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
    /// This class is loosely based on the code base of class ReactDevelopmentServerMiddleware
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

            // Start webpack-dev-server and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);

            Task<int> portTask = null;
            Task<Uri> targetUriTask = null;
            appBuilder.Use(async (context, next) =>
            {
                if (portTask == null)
                {
                    // Get port number of webapp first before we start webpack-dev-server, so that
                    // webpack can use the port number of the webapp for the websocket configuration.
                    var request = context.Request;
                    int socketPortNumber = request.Host.Port.Value;
                    portTask = StartWebpackDevServerAsync(sourcePath, npmScriptName, logger, socketPortNumber);
                    // Everything we **proxy** is hardcoded to target http://localhost because:
                    // - the requests are always from the local machine (we're not accepting remote
                    //   requests that go directly to the webpack-dev-server server)
                    // - given that, there's no reason to use https, and we couldn't even if we
                    //   wanted to, because in general the webpack-dev-server server has no certificate
#pragma warning disable CA2008 // Do not create tasks without passing a TaskScheduler
                    targetUriTask = portTask.ContinueWith(task =>
                    {
                        // "https" here doesn't work as the webpack-dev-server expects request via "http"
                        Uri uri = new UriBuilder("http", "localhost", task.Result).Uri;
                        return uri;
                    });
#pragma warning restore CA2008 // Do not create tasks without passing a TaskScheduler
                }

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await next();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
            });

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

        public static void PackBundles(
            ISpaBuilder spaBuilder,
            string npmScriptName
            )
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

            // Attach to middleware pipeline so that upon first invocation bundles are built:
            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);

            bool firstInvocation = true;
            appBuilder.Use(async (context, next) =>
            {
                if (firstInvocation)
                {
                    firstInvocation = false;
                    // Get port number of webapp first before we start webpack-dev-server, so that
                    // webpack can use the port number of the webapp for the websocket configuration.
                    var request = context.Request;
                    int socketPortNumber = request.Host.Port.Value;

                    var arguments = $"";
                    var envVars = new Dictionary<string, string>();
                    var npmScriptRunner = new NpmScriptRunner(
                        sourcePath,
                        npmScriptName,
                        arguments,
                        envVars);
                    npmScriptRunner.AttachToLogger(logger);

                    using (var stdErrReader = new EventedStreamStringReader(npmScriptRunner.StdErr))
                    {
                        try
                        {
                            await npmScriptRunner.StdOut.WaitForMatch(
                                new Regex("Hash: ", RegexOptions.None, RegexMatchTimeout));
                        }
                        catch (EndOfStreamException ex)
                        {
                            throw new InvalidOperationException(
                                $"The NPM script '{npmScriptName}' exited without indicating that the " +
                                $"webpack bundles were successfully built. The error output was: " +
                                $"{stdErrReader.ReadAsString()}", ex);
                        }
                    }
                }

#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task
                await next();
#pragma warning restore CA2007 // Consider calling ConfigureAwait on the awaited task
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
            var envVars = new Dictionary<string, string>
            {
            };
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
                        new Regex("Project is running at", RegexOptions.None, RegexMatchTimeout));
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
    }
}
