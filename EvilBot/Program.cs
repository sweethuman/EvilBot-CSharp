using System;

namespace EvilBot
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            TwitchChatBot bot = new TwitchChatBot();
            bot.Connect();

            Console.ReadLine();

            bot.Disconnect();
        }
    }
}