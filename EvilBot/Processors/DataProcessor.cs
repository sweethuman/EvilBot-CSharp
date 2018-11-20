using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;
using EvilBot.Utilities.Resources;
using EvilBot.Utilities.Resources.Interfaces;
using Serilog;

namespace EvilBot.Processors
{
	public class DataProcessor : IDataProcessor
	{
		private readonly IApiRetriever _apiRetriever;
		private readonly IConfiguration _configuration;
		private readonly IDataAccess _dataAccess;
		private readonly IFilterManager _filterManager;
		private readonly List<Tuple<string, int>> _ranks = new List<Tuple<string, int>>();

		public DataProcessor
			(IDataAccess dataAccess, IConfiguration configuration, IFilterManager filterManager, IApiRetriever apiRetriever)
		{
			_dataAccess = dataAccess;
			_configuration = configuration;
			_apiRetriever = apiRetriever;
			_filterManager = filterManager;
			InitializeRanks();
		}

		public event EventHandler<RankUpdateEventArgs> RankUpdated;

		protected virtual void OnRankUpdated(string name, string rank)
		{
			RankUpdated?.Invoke(this, new RankUpdateEventArgs {Name = name, Rank = rank});
		}

		private void InitializeRanks()
		{
			_ranks.Add(new Tuple<string, int>("Fara Rank", 0));
			_ranks.Add(new Tuple<string, int>("Rookie", 50));
			_ranks.Add(new Tuple<string, int>("Alpha", 500));
			_ranks.Add(new Tuple<string, int>("Thug", 2500));
			_ranks.Add(new Tuple<string, int>("Sage", 6000));
			_ranks.Add(new Tuple<string, int>("Lord", 10000));
			_ranks.Add(new Tuple<string, int>("Initiate", 15000));
			_ranks.Add(new Tuple<string, int>("Veteran", 22000));
			_ranks.Add(new Tuple<string, int>("Emperor", 30000));
		}

		public List<IRankItem> GetRankList()
		{
			return _ranks.Select((t, i) => new RankItem(i, t.Item1, t.Item2)).ToList<IRankItem>();
		}
		#region TimedPointManagers

		public async void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
		{
			Log.Debug("Updating Lurkers!");
			//in case twitch says something went wrong, it throws exception, catch that exception
			try
			{
				var userIdList = await _apiRetriever.GetChattersUsers(TwitchInfo.ChannelName);
				var userList = userIdList.Select(t => new UserBase(t.DisplayName, t.Id)).ToList<IUserBase>();
				await AddToUserAsync(userList, minutes: 10).ConfigureAwait(false);
				Log.Debug("Database updated! Lurkers present: {Lurkers}", userList.Count);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "AddLurkerTimer failed");
			}
		}

		public async void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
		{
			Log.Debug("Updating Talkers!");
			var temporaryTalkers = PointCounter.ClearTalkerPoints();
			try
			{
				await AddToUserAsync(temporaryTalkers).ConfigureAwait(false);
				Log.Debug("Database updated! Talkers present: {Talkers}", temporaryTalkers.Count);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "AddPointsTimer failed");
			}
		}

		#endregion TimedPointManagers

		#region Ranking and Points

		public string GetRankFormatted(string rankString, string pointsString)
		{
			if (int.TryParse(rankString, out var rank) && int.TryParse(pointsString, out var points))
			{
				if (rank == 0) return $"{_ranks[rank].Item1} XP: {points}/{_ranks[rank + 1].Item2}";
				if (rank == _ranks.Count - 1) return $"{_ranks[rank].Item1} (Lvl.{rank}) XP: {points}";
				return $"{_ranks[rank].Item1} (Lvl.{rank}) XP: {points}/{_ranks[rank + 1].Item2}";
			}

			Log.Error("{rankString} {pointsString} is not a parseable value to int {method}", rankString, pointsString,
				$"{ToString()} GetRankFormatted");
			return null;
		}
		
		private int GetRank(int points)
		{
			var place = 0;
			for (var i = 0; i < _ranks.Count - 1; i++)
			{
				if (points < _ranks[i + 1].Item2) break;
				place = i + 1;
			}

			return place;
		}
		
	    /// <inheritdoc />
		public async Task AddToUserAsync
			(List<IUserBase> userList, int points = 1, int minutes = 0, bool subCheck = true)
		{
			if (userList.Count != 0)
			{
				//NOTE maybe this should be put in a function and moved upwards because it doesn't really fit the scope of this method
				for (var i = 0; i < userList.Count; i++)
				{
					if (!_filterManager.CheckIfUserFiltered(userList[i])) continue;
					userList.RemoveAll(x => x.UserId == userList[i].UserId);
					i--;
				}

				var pointsMultiplier = _configuration.PointsMultiplier;
				//t: make sub checking more efficient
				List<IUserBase> channelSubscribers;
				try
				{
					if (subCheck)
					{
						channelSubscribers = await _apiRetriever.GetChannelSubscribers(_apiRetriever.TwitchChannelId).ConfigureAwait(false);
					}
					else
					{
						channelSubscribers = new List<IUserBase>();
					}
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Failed to GetSubscribers or ChannelId");
					throw;
				}

				int pointAdderValue;
				var addPointsTasks = new List<Task>();
				for (var i = 0; i < userList.Count; i++)
				{
					pointAdderValue = points;
					if (channelSubscribers.Any(x => x.UserId == userList[i].UserId))
						pointAdderValue = (int) (pointAdderValue * pointsMultiplier);
					addPointsTasks.Add(_dataAccess.ModifierUserIdAsync(userList[i].UserId, pointAdderValue, minutes));
				}

				await Task.WhenAll(addPointsTasks).ConfigureAwait(false);
				await UpdateRankAsync(userList).ConfigureAwait(false);
			}
		}

		private async Task UpdateRankAsync(IReadOnlyList<IUserBase> userList)
		{
			Log.Debug("Checking Ranks for {userCount}", userList.Count);
			//!WARNING GetUserAttributesAsync() also gets minutes, wich I don't currently need and it might cause performance issues if volume is large
			var userAttributesTasks = new List<Task<List<string>>>();
			var userNameRanks = new List<int>();
			var usersUpdated = new List<IUserBase>();
			var databaseRankUpdateTasks = new List<Task>();
			for (var i = 0; i < userList.Count; i++)
				userAttributesTasks.Add(GetUserAttributesAsync(userList[i].UserId));
			var userAttributes = (await Task.WhenAll(userAttributesTasks).ConfigureAwait(false)).ToList();
			for (var i = 0; i < userAttributes.Count; i++)
				try
				{
					if (!int.TryParse(userAttributes[i][0], out var points))
					{
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", userAttributes[i][1],
							$"{ToString()}UpdateRankAsync");
						continue;
					}

					if (!int.TryParse(userAttributes[i][2], out var rank))
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", userAttributes[i][1],
							$"{ToString()}UpdateRankAsync");

					var currentRank = GetRank(points);
					if (currentRank != rank)
					{
						databaseRankUpdateTasks.Add(_dataAccess.ModifyUserIdRankAsync(userList[i].UserId, currentRank));
						//TODO make it so that it goes all into a single class
						userNameRanks.Add(currentRank);
						usersUpdated.Add(userList[i]);
					}
				}
				catch (NullReferenceException e)
				{
					Log.Error(e,
						"Some null happened, probably userAttributes, prevent from sending the null onward, or check why it was sent {problemSource}",
						e.Source);
				}
				catch (Exception e)
				{
					Log.Error(e,
						"Ok, this is pretty bad, it's not null so somebody didn't know how to handle stuff {source}",
						e.Source);
				}

			await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
			for (var i = 0; i < usersUpdated.Count; i++)
				OnRankUpdated(usersUpdated[i].DisplayName,
					$"{_ranks[userNameRanks[i]].Item1} (Lvl. {userNameRanks[i]})");
		}

		#endregion Ranking and Points
		
		#region GeneralProcessors

		//TODO: advance to a better system, maybe with tuples and maybe make it get all the attributes, or return them all for easy identification and read
		public async Task<List<string>> GetUserAttributesAsync(string userId)
		{
			Log.Debug("Asking for attributes of {userId}", userId);
			if (userId == null) return null;

			var properties = await _dataAccess.RetrieveUserFromTable(Enums.DatabaseTables.UserPoints, userId);
			if (properties == null) return null;
			var results = new List<string> {properties.Points, properties.Minutes, properties.Rank};
			return results[0] == null ? null : results;
		}

		public static IEnumerable<List<T>> SplitList<T>(List<T> list, int sizeOfSplit = 100)
		{
			for (var i = 0; i < list.Count; i+=sizeOfSplit)
			{
				yield return list.GetRange(i, Math.Min(sizeOfSplit, list.Count - i));	
			}
		}
		
		#endregion GeneralProcessors
	}
}