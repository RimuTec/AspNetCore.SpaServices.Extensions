using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RimuTec.AspNetCore.SpaServices.WebpackDevelopmentServer;

namespace Web
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
            services.AddSpaStaticFiles(configuration =>
            {
                // This is where files will be served from in non-Development environments
                configuration.RootPath = "wwwroot/dist";
            });
            // END webpack-dev-server
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public static void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            // Note that the sequence in this method is relevnt, as handlers will be added to the request
            // pipline in the order they are listed here. This is by design in ASP.NET Core.

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            // BEGIN webpack-dev-server
            app.UseSpa(spa =>
            {
                spa.Options.SourcePath = "wwwroot/src";

                if (env.IsDevelopment()) // "Development", not "Debug" !!
                {
                    // IMPORTANT: UseWebpackDevelopmentServer() must be called before UseSpaStaticFiles() !!
                    spa.UseWebpackDevelopmentServer(npmScriptName: "start");
                }
            });
            // END webpack-dev-server

            // As the following two are terminal request handlers, they need to be added to the request 
            // pipeline AFTER the WebpackDevelopmentServer(). For more details about the request pipeline
            // and terminal handlers, see documentation at
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-3.1
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
