using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace SSO
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
              .ConfigureWebHostDefaults(webBuilder =>
              {
                  webBuilder.UseStartup<Startup>();
              });
    }
    //public static IHostBuilder CreateHostBuilder(string[] args)
    //    {
    //        var x509ca = new X509Certificate2(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "aspnetapp.pfx")), "123456");
    //        return Host.CreateDefaultBuilder(args)
    //                .ConfigureWebHostDefaults(webBuilder =>
    //                {
    //                    webBuilder.UseKestrel(option => option.ListenAnyIP(3000, config => config.UseHttps(x509ca)))
    //                    .UseStartup<Startup>();
    //                });
    //    }
    //}
}
