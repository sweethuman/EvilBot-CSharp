using System;
using EvilBot.Resources;
using EvilBot.TwitchBot;
using EvilBot.TwitchBot.Interfaces;
using Serilog;

namespace EvilBot
{
	internal class App : IApplication
	{
		private readonly ITwitchChatBot _twitchChatBot;
		private readonly ITwitchConnections _twitchConnection;
		// ReSharper disable once NotAccessedField.Local
		private readonly CommandsContainer _commandsContainer;

		public App(ITwitchConnections twitchConnections, ITwitchChatBot twitchChatBot, CommandsContainer commandsContainer)
		{
			_twitchChatBot = twitchChatBot;
			_commandsContainer = commandsContainer;
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
			Console.Title = StandardMessages.BotInformation.AboutBot;
			Console.TreatControlCAsInput = true;
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
		}
	}
}
