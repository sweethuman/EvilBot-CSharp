using Serilog;
using System;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Models;

namespace EvilBot
{
    internal class TwitchConnections : ITwitchConnections
    {
        private static TwitchAPI api;
        private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        private static TwitchClient client;
        private ILoggerManager _loggerManager;

        public TwitchClient Client
        {
            get
            {
                return client;
            }
        }

        public TwitchAPI Api
        {
            get
            {
                return api;
            }
        }

        public TwitchConnections(ILoggerManager loggerManager)
        {
            _loggerManager = loggerManager;
        }

        public void Connect()
        {
            Log.Debug("Connecting");
            ClientInitialize();
            ApiInitialize();
        }

        public void Disconnect()
        {
            Log.Debug("Disconnecting");
        }

        private void ClientInitialize()
        {
            var clientOptions = new ClientOptions
            {
                ClientType = ClientType.Chat,
                ReconnectionPolicy = new ReconnectionPolicy(5, 5),
                UseSsl = true,
                MessagesAllowedInPeriod = 90,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
            var customClient = new WebSocketClient(clientOptions);
            client = new TwitchClient(client: customClient, logger: _loggerManager.ClientLogger);
            client.Initialize(credentials, TwitchInfo.ChannelName);
            client.Connect();
        }

        private void ApiInitialize()
        {
            api = new TwitchAPI(loggerFactory: _loggerManager.APILoggerFactory);
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;
        }
    }
}