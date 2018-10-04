using Serilog;
using System;

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

        private static void SetConsoleMode()
        {
            Console.Title = @"EvilBot for Twitch by M0rtuary";
            Console.TreatControlCAsInput = true;
        }

        public void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            SetConsoleMode();
            _twitchConnection.Connect();
            _twitchChatBot.Connect();
            var stop = false;
            do
            {
                var keyPress = Console.ReadKey();
                if (keyPress.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift | ConsoleModifiers.Alt) && keyPress.Key == ConsoleKey.F6)
                {
                    stop = true;
                }
            } while (!stop);

            _twitchConnection.Disconnect();
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
        }
    }
}