using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace App
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseWebRoot(Path.Combine(Directory.GetCurrentDirectory(), "..", "public"))
                .UseStartup<Startup>()
                .UseUrls("http://localhost:8080")
                .Build();

            host.Run();
        }
    }
}
