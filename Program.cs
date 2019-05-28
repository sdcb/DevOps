using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using System;
using System.IO;

namespace DevOps
{
    public class Program
    {
        public static readonly string BaseDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DevOps");

        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseStartup<Startup>();
    }
}
