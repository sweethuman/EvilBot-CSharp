using EvilBot.Utilities.Interfaces;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using TwitchLib.Client;
using ILogger = Serilog.ILogger;

namespace EvilBot.Utilities
{
	public class LoggerUtility : ILoggerUtility
	{
		public LoggerUtility()
		{
			ILogger clientSerilogLogger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Source", "TwitchClient", true)
				.WriteTo.Seq("http://localhost:5341")
				.WriteTo.Async(a => a.File("logs/logfile.log", rollingInterval: RollingInterval.Day, shared: true))
				.WriteTo.Async(a => a.Console())
				.WriteTo.Sentry(o =>
				{
					// Debug and higher are stored as breadcrumbs (default is Information)
					o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
					// Warning and higher is sent as event (default is Error)
					o.MinimumEventLevel = LogEventLevel.Error;
				})
				.MinimumLevel.Debug()
				.CreateLogger();
			ClientLogger = new LoggerFactory()
				.AddSerilog(clientSerilogLogger)
				.CreateLogger<TwitchClient>();
			ILogger apiSerilogLogger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Source", "TwitchAPI", true)
				.WriteTo.Seq("http://localhost:5341")
				.WriteTo.Async(a => a.File("logs/logfile.log", rollingInterval: RollingInterval.Day, shared: true))
				.WriteTo.Async(a => a.Console())
				.WriteTo.Sentry(o =>
				{
					// Debug and higher are stored as breadcrumbs (default is Information)
					o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
					// Warning and higher is sent as event (default is Error)
					o.MinimumEventLevel = LogEventLevel.Error;
				})
				.MinimumLevel.Debug()
				.CreateLogger();
			ApiLoggerFactory = new LoggerFactory()
				.AddSerilog(apiSerilogLogger);

			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.Enrich.WithProperty("Source", "TwitchChatBot", true)
				.WriteTo.Seq("http://localhost:5341")
				.WriteTo.Async(a => a.File("logs/logfile.log", rollingInterval: RollingInterval.Day,
					outputTemplate:
					"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
					shared: true))
				.WriteTo.Async(a => a.Console())
				.WriteTo.Sentry(o =>
				{
					// Debug and higher are stored as breadcrumbs (default is Information)
					o.MinimumBreadcrumbLevel = LogEventLevel.Debug;
					// Warning and higher is sent as event (default is Error)
					o.MinimumEventLevel = LogEventLevel.Error;
				})
				.MinimumLevel.Verbose()
				.CreateLogger();
		}

		public ILogger<TwitchClient> ClientLogger { get; set; }

		public ILoggerFactory ApiLoggerFactory { get; set; }
	}
}
