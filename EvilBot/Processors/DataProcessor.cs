using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Api.Core.Exceptions;

namespace EvilBot.Processors
{
	public class DataProcessor : IDataProcessor
	{
		private readonly IApiRetriever _apiRetriever;
		private readonly IConfiguration _configuration;
		private readonly IDataAccess _dataAccess;
		private readonly IFilterManager _filterManager;
		private readonly ITalkerCounter _talkerCounter;
		private readonly ITwitchConnections _twitchConnections;
		private readonly IRankManager _rankManager;

		public DataProcessor
		(IDataAccess dataAccess, IConfiguration configuration, IFilterManager filterManager,
			IApiRetriever apiRetriever, ITwitchConnections twitchConnections, ITalkerCounter talkerCounter,
			IRankManager rankManager)
		{
			_dataAccess = dataAccess;
			_configuration = configuration;
			_apiRetriever = apiRetriever;
			_filterManager = filterManager;
			_twitchConnections = twitchConnections;
			_talkerCounter = talkerCounter;
			_rankManager = rankManager;

		}


		#region GeneralProcessors

		public static IEnumerable<List<T>> SplitList<T>(List<T> list, int sizeOfSplit = 100)
		{
			for (var i = 0; i < list.Count; i += sizeOfSplit)
				yield return list.GetRange(i, Math.Min(sizeOfSplit, list.Count - i));
		}

		public List<T> RemoveFilteredUsers<T>(List<T> userList) where T : IUserBase
		{
			for (var i = 0; i < userList.Count; i++)
			{
				if (!_filterManager.CheckIfUserFiltered(userList[i])) continue;
				userList.RemoveAll(x => x.UserId == userList[i].UserId);
				i--;
			}

			return userList;
		}

		#endregion GeneralProcessors

		#region TimedPointManagers

		public async void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
		{
			Log.Debug("Updating Lurkers!");
			//in case twitch says something went wrong, it throws exception, catch that exception
			try
			{
				var userIdList =
					await _apiRetriever.GetChattersUsersAsync(_configuration.ChannelName).ConfigureAwait(false);
				var userList = userIdList.Select(t => new UserBase(t.DisplayName, t.Id)).ToList<IUserBase>();
				await AddToUserAsync(userList,_configuration.LurkerPoints ,10).ConfigureAwait(false);
				Log.Debug("Database updated! Lurkers present: {Lurkers}", userList.Count);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "AddLurkerTimer failed");
				_twitchConnections.SendErrorMessage("A esuat sa updateze Lurkerii. SEND LOGS.");
			}
		}

		public async void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
		{
			Log.Debug("Updating Talkers!");
			var temporaryTalkers = _talkerCounter.ClearTalkers();
			try
			{
				await AddToUserAsync(temporaryTalkers, _configuration.TalkerPoints).ConfigureAwait(false);
				Log.Debug("Database updated! Talkers present: {Talkers}", temporaryTalkers.Count);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "AddPointsTimer failed");
				_twitchConnections.SendErrorMessage("A esuat sa updateze Talkerii. SEND LOGS.");
			}
		}

		#endregion TimedPointManagers

		#region Points


		/// <inheritdoc />
		public async Task AddToUserAsync
			(List<IUserBase> userList, int points = 1, int minutes = 0, bool subCheck = true)
		{
			if (userList.Count != 0)
			{
				userList = RemoveFilteredUsers(userList);

				var pointsMultiplier = _configuration.PointsMultiplier;
				var channelSubscribers = new List<IUserBase>();
				if (subCheck)
				{
					for (var attempt = 0; attempt < 5; attempt++)
					{
						try
						{
							channelSubscribers = await _apiRetriever
								.GetChannelSubscribersAsync(_apiRetriever.TwitchChannelId)
								.ConfigureAwait(false);
							break;
						}
						catch (GatewayTimeoutException ex)
						{
							if (attempt == 4) throw;
							Log.Warning(ex, "Failed. Gateway Timed Out. Retrying in 20 Seconds");
						}
						catch (Exception ex)
						{
							Log.Error(ex, "Failed to GetSubscribers or ChannelId");
							throw;
						}
						await Task.Delay(20000).ConfigureAwait(false);
					}
				}

				var addPointsTasks = new List<Task>();
				foreach (var user in userList)
				{
					var pointAdderValue = points;
					if (channelSubscribers.Any(x => x.UserId == user.UserId))
						pointAdderValue = (int) (pointAdderValue * pointsMultiplier);
					addPointsTasks.Add(_dataAccess.ModifierUserIdAsync(user.UserId, pointAdderValue, minutes));
				}

				await Task.WhenAll(addPointsTasks).ConfigureAwait(false);
				await _rankManager.UpdateRankAsync(userList).ConfigureAwait(false);
			}
		}

		#endregion Points
	}
}
