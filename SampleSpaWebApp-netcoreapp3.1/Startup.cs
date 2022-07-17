// Copyright 2020-2022 (c) RimuTec Ltd. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RimuTec.AspNetCore.SpaServices.WebpackDevelopmentServer;

namespace SampleSpaWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // BEGIN webpack-dev-server
            services.AddSpaStaticFiles(spaStaticFileOptions =>
            {
                // This is where files will be served from in non-Development environments
                spaStaticFileOptions.RootPath = "MyApp/dist";
            });
            // END webpack-dev-server
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder appBuilder, IWebHostEnvironment webHostEnv)
        {
            // Note that the sequence in this method is relevant, as handlers will be added to the request
            // pipeline in the order they are listed here. In ASP.NET Core this is by design.
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1

            if (webHostEnv.IsDevelopment())
            {
                appBuilder.UseDeveloperExceptionPage();
            }
            else
                {
                appBuilder.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                appBuilder.UseHsts();
                }

            // Regarding UseHttpsRedirection() please also see the following article:
            // https://docs.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-3.1&tabs=visual-studio
            appBuilder.UseHttpsRedirection();
            // For details regarding static files in ASP.NET Core, see
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/static-files?view=aspnetcore-3.1
            appBuilder.UseStaticFiles();
            appBuilder.UseSpaStaticFiles();

            appBuilder.UseRouting();

            appBuilder.UseAuthorization();

            appBuilder.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // BEGIN webpack-dev-server
            appBuilder.UseSpa(spaBuilder =>
            {
                spaBuilder.Options.SourcePath = "MyApp/src";

                if (webHostEnv.IsDevelopment()) // "Development", not "Debug" !!
                {
                    spaBuilder.UseWebpackDevelopmentServer(npmScriptName: "start");
                }
            });
            // END webpack-dev-server
        }
    }
}
