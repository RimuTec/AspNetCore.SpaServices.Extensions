using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.NodeServices;
using Microsoft.AspNetCore.SpaServices;
using Microsoft.Extensions.Logging;
using SpaServices.Extensions.Npm;
using SpaServices.Extensions.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SpaServices.Extensions.Webpack
{
    internal static class WebpackDevelopmentServerMiddleware
    {
        private const string LogCategoryName = nameof(SpaServices) + "." + nameof(Extensions) + "." + nameof(Webpack) + "." + nameof(WebpackDevelopmentServerMiddleware);
        private static TimeSpan RegexMatchTimeout = TimeSpan.FromSeconds(5); // This is a development-time only feature, so a very long timeout is fine

        public static void Attach(
            ISpaBuilder spaBuilder,
            string npmScriptName)
        {
            var sourcePath = spaBuilder.Options.SourcePath;
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(sourcePath));
            }

            if (string.IsNullOrEmpty(npmScriptName))
            {
                throw new ArgumentException("Cannot be null or empty", nameof(npmScriptName));
            }

            // Start webpack-dev-server and attach to middleware pipeline
            var appBuilder = spaBuilder.ApplicationBuilder;
            var logger = LoggerFinder.GetOrCreateLogger(appBuilder, LogCategoryName);

            Task<int> portTask = null;
            Task<Uri> targetUriTask = null;
            appBuilder.Use(async (context, next) =>
            {
                if(portTask == null)
                {
                    // Get port number of webapp first before we start webpack-dev-server
                    var request = context.Request;
                    int socketPortNumber = request.Host.Port.Value;
                    string hostName = request.Host.Host;
                    string scheme = request.Scheme;
                    // here we can also get the host name and others that might be useful for the socket to be used by webpack bundles
                    portTask = StartWebpackDevServerAsync(sourcePath, npmScriptName, logger, socketPortNumber);
                    targetUriTask = portTask.ContinueWith(task =>
                    {
                        //Uri uri = new UriBuilder(scheme, "localhost", task.Result).Uri;
                        // "https" here doesn't work as the webpack-dev-server expects request via "http"
                        Uri uri = new UriBuilder("http", "localhost", task.Result).Uri; 
                        return uri;
                    });
                }

                await next();
            });
          
            
            //var portTask = StartWebpackDevelopmentServerAsync(sourcePath, npmScriptName, logger);

            // Everything we proxy is hardcoded to target http://localhost because:
            // - the requests are always from the local machine (we're not accepting remote
            //   requests that go directly to the webpack-dev-server server)
            // - given that, there's no reason to use https, and we couldn't even if we
            //   wanted to, because in general the webpack-dev-server server has no certificate
            //var targetUriTask = portTask.ContinueWith(task => 
            //    { 
            //        return new UriBuilder("http", "localhost", task.Result).Uri; 
            //    });

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

            var arguments = $"--port {portNumber} --sockPort {socketPortNumber}";
            //var arguments = $"--port {portNumber} --public 'localhost:{portNumber}' ";
            var envVars = new Dictionary<string, string>
            {
                //{ "PORT", "55555" },
                //{ "PORT", portNumber.ToString() },
                //{ "BROWSER", "none" }, // We don't want create-react-app to open its own extra browser window pointing to the internal dev server port
            };
            var npmScriptRunner = new NpmScriptRunner(
                sourcePath, npmScriptName, 
                arguments,
                //null, 
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

                    await npmScriptRunner.StdOut.WaitForMatch(
                        new Regex("Project is running at", RegexOptions.None, RegexMatchTimeout));
                    //await npmScriptRunner.StdOut.WaitForMatch(
                    //    new Regex("Starting the development server", RegexOptions.None, RegexMatchTimeout));
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
