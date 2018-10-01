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
            _twitchChatBot.Connect();

            Console.ReadLine();

            _twitchChatBot.Disconnect();
        }
    }
}