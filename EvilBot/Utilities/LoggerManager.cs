﻿using Microsoft.Extensions.Logging;
using Serilog;
using TwitchLib.Client;

namespace EvilBot
{
    public class LoggerManager : ILoggerManager
    {
        public ILogger<TwitchClient> ClientLogger { get; set; }
        public ILoggerFactory APILoggerFactory { get; set; }

        public LoggerManager()
        {
            Serilog.ILogger clientSerilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
            ClientLogger = new LoggerFactory()
                .AddSerilog(logger: clientSerilogLogger)
                .CreateLogger<TwitchClient>();
            Serilog.ILogger apiSerilogLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Source", "TwitchAPI", true)
                .WriteTo.Seq("http://localhost:5341")
                .WriteTo.File("logfile.log", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Debug()
                .CreateLogger();
            APILoggerFactory = new LoggerFactory()
                .AddSerilog(logger: apiSerilogLogger);

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