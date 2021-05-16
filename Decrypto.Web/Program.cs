using Decrypto;
using Decrypto.Hosting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

try
{
    await Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webHost => webHost.UseStartup<Startup>())
        .ConfigureSerilog()
        .Build()
        .RunAsync();

    return 0;
}
catch(Exception exception)
{
    var foreground = Console.ForegroundColor;

    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine(exception);
    Console.ForegroundColor = foreground;

    return 1;
}