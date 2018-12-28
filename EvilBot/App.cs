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
			SetConsoleMode();
			var stop = false;
			do
			{
				var keyPress = Console.ReadKey();
				if (keyPress.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift) &&
				    keyPress.Key == ConsoleKey.Oem3) stop = true;
			} while (!stop);

			_twitchChatBot.Disconnect();
			_twitchConnection.Disconnect();
		}

		private static void SetConsoleMode()
		{
			Console.Title = @"EvilBot v0.4.17.4beta for Twitch by M0rtuary";
			Console.TreatControlCAsInput = true;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
		}
	}
}