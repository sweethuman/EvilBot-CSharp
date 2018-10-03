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
        private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        private readonly ILoggerManager _loggerManager;

        public TwitchClient Client { get; private set; }

        public TwitchAPI Api { get; private set; }

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
            Client = new TwitchClient(client: customClient, logger: _loggerManager.ClientLogger);
            Client.Initialize(credentials, TwitchInfo.ChannelName);
            Client.Connect();
        }

        private void ApiInitialize()
        {
            Api = new TwitchAPI(loggerFactory: _loggerManager.APILoggerFactory);
            Api.Settings.ClientId = TwitchInfo.ClientID;
            Api.Settings.AccessToken = TwitchInfo.BotToken;
        }
    }
}