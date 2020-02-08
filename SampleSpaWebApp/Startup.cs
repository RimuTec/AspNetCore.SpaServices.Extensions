using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();

            // BEGIN react-multi-hmr
            services.AddSpaStaticFiles(configuration =>
            {
                // This is where files will be served from in non-Development environments
                configuration.RootPath = "wwwroot/dist";
            });
            // END react-multi-hmr
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // BEGIN react-multi-hmr
            app.UseStaticFiles();
            app.UseSpaStaticFiles();
            // END react-multi-hmr

            app.UseRouting();

            app.UseAuthorization();

            // BEGIN react-multi-hmr
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot/src";

                if (env.IsDevelopment()) // "Development", not "Debug" !!
                {
                    spa.UseWebpackDevelopmentServer(npmScriptName: "start");
                }
            });
            // END react-multi-hmr

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
