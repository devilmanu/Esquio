﻿using Esquio.AspNetCore.Mvc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using Xunit;

namespace UnitTests.Seedwork
{
    public class ServerFixture
    {
        public TestServer TestServer { get; set; }

        public ServerFixture()
        {
            var testServer = new TestServer();

            var host = Host.CreateDefaultBuilder()
                 .UseContentRoot(Directory.GetCurrentDirectory())
                 .ConfigureAppConfiguration((_, cfg) =>
                 {
                     cfg.AddJsonFile("appsettings.json", optional: false);
                 })
                 .ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder
                     .UseServer(testServer)
                     .UseStartup<TestStartup>();
                 }).Build();

            host.StartAsync().Wait();

            TestServer = host.GetTestServer();
        }
    }

    [CollectionDefinition(nameof(AspNetCoreServer))]
    public class AspNetCoreServer
        : IClassFixture<ServerFixture>
    {

    }

    class TestStartup
    {
        private IConfiguration _configuration;

        public TestStartup(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc()
                .Services
                .AddEsquio()
                .AddMvcFallbackAction((context) =>
                {
                    var actionName = context.ActionDescriptor.RouteValues["action"];

                    if (actionName != "ActionWithNoActiveFlag")
                    {
                        return new RedirectResult("/controller/action");
                    }
                    else
                    {
                        return new NotFoundResult();
                    }
                })
                .AddConfigurationStore(_configuration, "Esquio");
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseAuthorization();
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapEsquio("esquio");
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }

    public class TestController : Controller
    {
        [ActionName("ActionWithFlagSwitch")]
        [FeatureSwitch(Product = "TestApp", Names = "Sample1")]
        public IActionResult Sample1()
        {
            return Content("Active");
        }
        [ActionName("ActionWithFlagSwitch")]
        public IActionResult Sample11()
        {
            return Content("NonActive");
        }
        [ActionName("ActionWithFlagSwitchNoActive")]
        [FeatureSwitch(Product = "TestApp", Names = "Sample2")]
        public IActionResult Sample2()
        {
            return Content("Active");
        }
        [ActionName("ActionWithFlagSwitchNoActive")]
        public IActionResult Sample22()
        {
            return Content("NonActive");
        }
        [FeatureFilter(Names = "Sample1")]
        public IActionResult ActionWithActiveFlag()
        {
            return Ok();
        }

        [FeatureFilter(Names = "Sample1,Sample1")]
        public IActionResult ActionWithMultipleActiveFlag()
        {
            return Ok();
        }
        [FeatureFilter(Names = "Sample1,Sample2")]
        public IActionResult ActionWithMultipleFlagAndNotActive()
        {
            return Ok();
        }

        [FeatureFilter(Names = "Sample2")]
        public IActionResult ActionWithNoActiveFlag()
        {
            return Ok();
        }

        [FeatureFilter(Names = "Sample2")]
        public IActionResult ActionWithNoActiveFlagAndFallbackAction()
        {
            return Ok();
        }
    }
}