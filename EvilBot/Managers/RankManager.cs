using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using Serilog;

namespace EvilBot.Managers
{
	public class RankManager : IRankManager
	{
		private readonly List<IRankItem> _ranks = new List<IRankItem>();
		private readonly IDataAccess _dataAccess;
		private readonly IApiRetriever _apiRetriever;
		public string RankListString { get; private set; }


		public RankManager(IDataAccess dataAccess, IApiRetriever apiRetriever)
		{
			_dataAccess = dataAccess;
			_apiRetriever = apiRetriever;
			InitializeRanks();
			BuildRankListString();
		}

		private void BuildRankListString()
		{
			var rankList = GetRankList();
			var builder = new StringBuilder();
			for (var i = 1; i < rankList.Count; i++)
				builder.AppendFormat("{0}.{1}:{2} ", rankList[i].Id, rankList[i].Name, rankList[i].RequiredPoints);
			RankListString = builder.ToString();
		}

		public event EventHandler<RankUpdateEventArgs> RankUpdated;

		protected virtual void OnRankUpdated(string name, string rank)
		{
			RankUpdated?.Invoke(this, new RankUpdateEventArgs {Name = name, Rank = rank});
		}

		private void InitializeRanks()
		{
			_ranks.Add(new RankItem(0, "Newbie", 0));
			_ranks.Add(new RankItem(1, "Rookie", 50));
			_ranks.Add(new RankItem(2, "Alpha", 500));
			_ranks.Add(new RankItem(3, "Thug", 2500));
			_ranks.Add(new RankItem(4, "Sage", 6000));
			_ranks.Add(new RankItem(5, "Lord", 10000));
			_ranks.Add(new RankItem(6, "Initiate", 15000));
			_ranks.Add(new RankItem(7, "Veteran", 22000));
			_ranks.Add(new RankItem(8, "Emperor", 30000));
		}


		public string GetRankFormatted(string rankString, string pointsString)
		{
			if (int.TryParse(rankString, out var rank) && int.TryParse(pointsString, out var points))
			{
				if (rank == 0) return $"{_ranks[rank].Name} XP: {points}/{_ranks[rank + 1].RequiredPoints}";
				if (rank == _ranks.Count - 1) return $"{_ranks[rank].Name} (Lvl.{rank}) XP: {points}";
				return $"{_ranks[rank].Name} (Lvl.{rank}) XP: {points}/{_ranks[rank + 1].RequiredPoints}";
			}

			Log.Error("{rankString} {pointsString} is not a parseable value to int {method}", rankString, pointsString,
				$"{ToString()} GetRankFormatted");
			return null;
		}

		public int CalculateRank(int points)
		{
			var place = 0;
			for (var i = 0; i < _ranks.Count - 1; i++)
			{
				if (points < _ranks[i + 1].RequiredPoints) break;
				place = i + 1;
			}

			return place;
		}

		public IRankItem GetRank(int rank)
		{
			if (rank < _ranks.Count && rank >= 0) return _ranks[rank];
			Log.Error("ASKED FOR INEXISTENT RANK. OUT OF BOUNDS.");
			return new RankItem(rank, "INEXISTENT", 0);

		}

		public List<IRankItem> GetRankList()
		{
			return _ranks;
		}


		public async Task UpdateRankAsync(IEnumerable<string> userIds)
		{
			var userList = userIds.ToList();
			var countOfRemovedItems = userList.RemoveAll(x => x == null);
			if (countOfRemovedItems != 0 ) Log.Warning("THERE ARE NULLS inside the UserIds that need to be Rank Updated. REMOVED.");
			var usersApi = await _apiRetriever.GetUsersHelixAsync(userList).ConfigureAwait(false);
			var userBaseUsers = usersApi.Select(x => new UserBase(x.DisplayName, x.Id)).ToList<IUserBase>();
			await UpdateRankAsync(userBaseUsers).ConfigureAwait(false);
		}

		public Task UpdateRankAsync(IUserBase user)
		{
			return UpdateRankAsync(new[] {user});
		}

		public async Task UpdateRankAsync(IReadOnlyList<IUserBase> userList)
		{
			Log.Debug("Checking Ranks for {userCount}", userList.Count);
			var usersUpdated = new List<IUserStructure>();
			var databaseRankUpdateTasks = new List<Task>();
			var getUserAttributesTasks = userList
				.Select(t => _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, t.UserId))
				.ToList();
			var userAttributes = (await Task.WhenAll(getUserAttributesTasks).ConfigureAwait(false)).ToList();
			if (userAttributes.Contains(null))
				Log.Error(
					"There shouldn't have been sent a null user or every user should have been in the database. INVESTIGATE!");
			userAttributes.RemoveAll(x => x == null);
			var query =
				from userAttribute in userAttributes
				join user in userList on userAttribute.UserId equals user.UserId
				select new UserStructureData(user.DisplayName, userAttribute.Id, userAttribute.UserId,
					userAttribute.Points, userAttribute.Minutes, userAttribute.Rank);
			var users = query.ToList<IUserStructure>();
			for (var i = 0; i < users.Count; i++)
				try
				{
					if (!int.TryParse(users[i].Points, out var points))
					{
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", users[i].Points,
							$"{ToString()}UpdateRankAsync");
						continue;
					}

					if (!int.TryParse(users[i].Rank, out var rank))
					{
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", users[i].Rank,
							$"{ToString()}UpdateRankAsync");
						continue;
					}

					var currentRank = CalculateRank(points);
					if (currentRank == rank) continue;
					databaseRankUpdateTasks.Add(_dataAccess.ModifyUserIdRankAsync(userList[i].UserId, currentRank));
					users[i].Rank = currentRank.ToString();
					usersUpdated.Add(users[i]);
				}
				catch (NullReferenceException e)
				{
					Log.Error(e,
						"Some null happened, probably userAttributes, prevent from sending the null onward, or check why it was sent {problemSource}",
						e.Source);
					await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
					throw;
				}
				catch (Exception e)
				{
					Log.Error(e,
						"Ok, this is pretty bad, it's not null so somebody didn't know how to handle stuff {source}",
						e.Source);
					await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
					throw;
				}

			await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
			for (var i = 0; i < usersUpdated.Count; i++)
				OnRankUpdated(usersUpdated[i].DisplayName,
					$"{_ranks[int.Parse(usersUpdated[i].Rank)].Name} (Lvl. {usersUpdated[i].Rank})");
		}
	}
}
