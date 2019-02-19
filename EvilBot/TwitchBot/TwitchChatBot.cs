using System.Timers;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	internal class TwitchChatBot : ITwitchChatBot
	{
		private readonly IConfiguration _configuration;
		private readonly IDataAccess _dataAccess;
		private readonly IDataProcessor _dataProcessor;
		private readonly ITwitchConnections _twitchConnection;
		// ReSharper disable once NotAccessedField.Local
		private readonly CommandsContainer _commandsContainer;
		// ReSharper disable once NotAccessedField.Local
		private readonly MessageHandler _messageHandler;

		private Timer _addLurkerPointsTimer;
		private Timer _addPointsTimer;

		public TwitchChatBot(ITwitchConnections twitchConnection, IDataAccess dataAccess, IDataProcessor dataProcessor,
			IConfiguration configuration, IApiRetriever apiRetriever, IPresenceCounter presenceCounter,
			CommandsContainer commandsContainer, MessageHandler messageHandler)
		{
			_twitchConnection = twitchConnection;
			_dataProcessor = dataProcessor;
			_dataAccess = dataAccess;
			_configuration = configuration;
			_commandsContainer = commandsContainer;
			_messageHandler = messageHandler;
			presenceCounter.MakePresent(apiRetriever.TwitchChannelId);
			Connect();
		}

		~TwitchChatBot()
		{
			Log.Debug("Disposing of TwitchChatBot");
			_addLurkerPointsTimer.Dispose();
			_addPointsTimer.Dispose();
		}

		#region TwitchChatBot Initializers

		public void Connect()
		{
			Log.Debug("Starting EvilBot");

			EventInitializer();
			TimerInitializer();
		}

		public void Disconnect()
		{
			_dataAccess.Close();
			Log.Debug("Disconnecting");
		}


		private void TimerInitializer()
		{
			_addPointsTimer = new Timer(1000 * 60 * _configuration.TalkerMinutes);
			_addPointsTimer.Elapsed += _dataProcessor.AddPointsTimer_ElapsedAsync;
			_addPointsTimer.Start();

			_addLurkerPointsTimer = new Timer(1000 * 60 * _configuration.LurkerMinutes);
			_addLurkerPointsTimer.Elapsed += _dataProcessor.AddLurkerPointsTimer_ElapsedAsync;
			_addLurkerPointsTimer.Start();

		}

		private void EventInitializer()
		{
			_twitchConnection.Client.OnConnectionError += Client_OnConnectionError;
			_twitchConnection.Client.OnMessageSent += Client_OnMessageSent;
		}

		#endregion TwitchChatBot Initializers

		#region TwitchChatBot EventTriggers

		private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			Log.Error("Error!!! {ErrorMessage}", e.Error.Message);
		}

		private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
		{
			Log.Information("Message sent: {message}", e.SentMessage.Message);
		}

		#endregion TwitchChatBot EventTriggers
	}
}
