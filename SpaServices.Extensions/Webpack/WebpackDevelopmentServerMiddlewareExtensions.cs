using System;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;

namespace SpaServices.Extensions.Webpack
{
    public static class WebpackDevelopmentServerMiddlewareExtensions
    {
        private const string LogCategoryName = nameof(SpaServices) + "." + nameof(Extensions) + "." + nameof(Webpack) + "." + nameof(WebpackDevelopmentServerMiddlewareExtensions);
    
        public static void UseWebpackDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScript
            //,WebpackDevelopmentServerMiddlewareOptions options
            )
        {
            if (spaBuilder == null)
            {
                throw new ArgumentException(nameof(spaBuilder));
            }

            var spaOptions = spaBuilder.Options;

            if(string.IsNullOrEmpty(spaOptions.SourcePath))
            {
                throw new InvalidOperationException($"To use {nameof(UseWebpackDevelopmentServer)}, you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of {nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
            }

            WebpackDevelopmentServerMiddleware.Attach(spaBuilder, npmScript);
        }
    }
}
