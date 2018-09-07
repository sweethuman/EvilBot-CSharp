using Microsoft.Extensions.Logging;
using Serilog;
using TwitchLib.Client;

namespace EvilBot
{
    internal class LoggerManager
    {
        public ILogger<TwitchClient> Logger { get; set; }

        public LoggerManager()
        {
            Serilog.ILogger serilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
            Logger = new LoggerFactory()
                .AddSerilog(logger: serilogLogger)
                .CreateLogger<TwitchClient>();

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Source", "TwitchChatBot", true)
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
        }
    }
}