using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace KestrelDynamicPort
{
    class Program
    {
        private static readonly int _threads = Environment.ProcessorCount;
        private static readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private static readonly object _lock = new object();

        static void Main(string[] args)
        {
            var targetPort = int.Parse(args[0]);

            var threads = new Thread[_threads];
            for (var i= 0; i < _threads; i++)
            {
                var j = i;
                threads[i] = new Thread(() =>
                {
                    while (true)
                    {
                        TestPort(targetPort, j);
                    }
                });
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }
        }

        private static void TestPort(int targetPort, int thread)
        {
            // Block all threads if target port has been hit
            lock (_lock) { };

            using (var host = CreateWebHost())
            {
                host.Start();

                var port = host.GetPort();
                Console.WriteLine($"[{_stopwatch.Elapsed}] [{thread}] {port} ");

                if (port == targetPort)
                {
                    lock (_lock)
                    {
                        Console.WriteLine();
                        Console.WriteLine($"Kestrel was assigned port {targetPort}.  Press Enter to continue...");
                        Console.ReadLine();
                    }
                }
            }
        }

        private static IWebHost CreateWebHost()
        {
            return new WebHostBuilder()
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 0);
                })
                .Configure(app => app.Run(context =>
                {
                    return context.Response.WriteAsync("Hello World!");
                }))
                .Build();
        }
    }
}
