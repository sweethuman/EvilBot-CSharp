using System;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities.Interfaces;
using Serilog;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Enums;
using TwitchLib.Communication.Models;

namespace EvilBot.TwitchBot
{
	internal class TwitchConnections : ITwitchConnections
	{
		private readonly ConnectionCredentials _credentials =
			new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);

		private readonly ILoggerManager _loggerManager;

		public TwitchConnections(ILoggerManager loggerManager)
		{
			_loggerManager = loggerManager;
			Connect();
		}

		public TwitchClient Client { get; private set; }

		public TwitchAPI Api { get; private set; }

		public void Connect()
		{
			Log.Debug("Connecting");
			ClientInitialize();
			ApiInitialize();
		}

		//NOTE probably this is temporary, and maybe there is a better solution with wich I can use this without needing to have a ref to this class everywhere
		public void SendErrorMessage(string message)
		{
			Client.SendMessage(TwitchInfo.ChannelName.ToLower(), $"/me {message}");
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
			Client = new TwitchClient(customClient, logger: _loggerManager.ClientLogger);
			Client.Initialize(_credentials, TwitchInfo.ChannelName);
			Client.Connect();
		}

		private void ApiInitialize()
		{
			Api = new TwitchAPI(_loggerManager.ApiLoggerFactory);
			Api.Settings.ClientId = TwitchInfo.ClientID;
			Api.Settings.AccessToken = TwitchInfo.BotToken;
		}
	}
}