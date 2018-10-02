using Serilog;
using System;

namespace EvilBot
{
    internal class App : IApplication
    {
        private ITwitchChatBot _twitchChatBot;
        private ITwitchConnections _twitchConnection;

        public App(ITwitchConnections twitchConnections, ITwitchChatBot twitchChatBot)
        {
            _twitchChatBot = twitchChatBot;
            _twitchConnection = twitchConnections;
        }

        public void Run()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _twitchConnection.Connect();
            _twitchChatBot.Connect();

            Console.ReadLine();

            _twitchConnection.Disconnect();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Log.Fatal(e.ExceptionObject as Exception, "Unhandled exception blew UP");
        }
    }
}