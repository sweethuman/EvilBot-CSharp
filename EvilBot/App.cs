using Serilog;
using System;

namespace EvilBot
{
    internal class App : IApplication
    {
        private ITwitchChatBot _twitchChatBot;

        public App(ITwitchChatBot twitchChatBot)
        {
            _twitchChatBot = twitchChatBot;
        }

        public void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _twitchChatBot.Connect();

            Console.ReadLine();

            _twitchChatBot.Disconnect();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
        }
    }
}