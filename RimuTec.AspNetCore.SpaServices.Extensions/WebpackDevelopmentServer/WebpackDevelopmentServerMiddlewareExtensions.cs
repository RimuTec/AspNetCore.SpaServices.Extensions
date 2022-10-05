// Copyright 2020-2022 (c) RimuTec Ltd. All rights reserved.
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// This file has been modified by Rimutec Ltd to use it for WebDevelopmentServer support in development.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SpaServices;

namespace RimuTec.AspNetCore.SpaServices.WebpackDevelopmentServer
{
    /// <summary>
    /// This class is loosely based on the code base of class ReactDevelopmentServerMiddlewareExtensions
    /// in NuGet package Microsoft.AspNetCore.SpaServices.Extensions
    /// </summary>
    public static class WebpackDevelopmentServerMiddlewareExtensions
    {
        public static void UseWebpackDevelopmentServer(
            this ISpaBuilder spaBuilder,
            string npmScriptName,
            int? devServerPortNumber = null
            )
        {
            if (spaBuilder == null)
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new ArgumentException($"Parameter {nameof(spaBuilder)} cannot be null.", nameof(spaBuilder));
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            var spaOptions = spaBuilder.Options;

            if (string.IsNullOrEmpty(spaOptions.SourcePath))
            {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                throw new InvalidOperationException($"To use {nameof(UseWebpackDevelopmentServer)}, " +
                    $"you must supply a non-empty value for the {nameof(SpaOptions.SourcePath)} property of " +
                    $"{nameof(SpaOptions)} when calling {nameof(SpaApplicationBuilderExtensions.UseSpa)}.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            }

            WebpackDevelopmentServerMiddleware.Attach(spaBuilder, npmScriptName, devServerPortNumber);
        }
    }
}