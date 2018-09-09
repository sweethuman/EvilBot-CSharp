using System;

namespace EvilBot
{
    internal class Application : IApplication
    {
        private ITwitchChatBot _twitchChatBot;

        public Application(ITwitchChatBot twitchChatBot)
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