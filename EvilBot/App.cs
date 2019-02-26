using System;
using EvilBot.TwitchBot.Interfaces;
using Serilog;

namespace EvilBot
{
	internal class App : IApplication
	{
		private readonly ITwitchChatBot _twitchChatBot;
		private readonly ITwitchConnections _twitchConnection;

		public App(ITwitchConnections twitchConnections, ITwitchChatBot twitchChatBot)
		{
			_twitchChatBot = twitchChatBot;

			_twitchConnection = twitchConnections;
		}

		public void Run()
		{
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
		}

		public void Stop()
		{
			_twitchChatBot.Disconnect();
			_twitchConnection.Disconnect();
			Log.CloseAndFlush();
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
			Log.CloseAndFlush();
		}
	}
}
