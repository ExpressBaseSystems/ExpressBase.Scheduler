using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ExpressBase.Scheduler
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(7);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseUrls(urls: "http://*:41300/")
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
