using EvilBot.Utilities.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using TwitchLib.Client;

namespace EvilBot.Utilities
{
    public class LoggerManager : ILoggerManager
    {
        public ILogger<TwitchClient> ClientLogger { get; set; }
        public ILoggerFactory ApiLoggerFactory { get; set; }

        public LoggerManager()
        {
            //NOTE remove logging to seq
            Serilog.ILogger clientSerilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .Enrich.WithProperty("Source", "TwitchClient", true)
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day, shared: true)
                .MinimumLevel.Debug()
                .CreateLogger();
            ClientLogger = new LoggerFactory()
                .AddSerilog(clientSerilogLogger)
                .CreateLogger<TwitchClient>();
            Serilog.ILogger apiSerilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Source", "TwitchAPI", true)
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day, shared: true)
                .MinimumLevel.Debug()
                .CreateLogger();
            ApiLoggerFactory = new LoggerFactory()
                .AddSerilog(apiSerilogLogger);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Source", "TwitchChatBot", true)
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}", shared: true)
                .MinimumLevel.Verbose()
                .CreateLogger();
        }
    }
}