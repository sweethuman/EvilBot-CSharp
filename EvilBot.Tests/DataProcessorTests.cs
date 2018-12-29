using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Autofac.Extras.Moq;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Moq;
using Xunit;

namespace EvilBot.Tests
{
	public class DataProcessorTests
	{
		[Theory]
		[InlineData("6", "70", "Initiate (Lvl.6) XP: 70/22000")]
		[InlineData("0", "5", "Fara Rank XP: 5/50")]
		[InlineData("adad", "dasdas", null)]
		[InlineData("8", "60000", "Emperor (Lvl.8) XP: 60000")]
		public void GetRankFormatted_ShouldReturnCorrectString(string rankString, string pointsString, string expected)
		{
			var dataProcessor = new DataProcessor(null, null, null, null, null, null);
			var result = dataProcessor.GetRankFormatted(rankString, pointsString);
			Assert.Equal(expected, result);
		}

		[Theory]
		[InlineData(true)]
		[InlineData(false)]
		//NOTE wanted to add to subcheck true and false and going to use it.is to see how to make
		public async void AddToUserAsync_ShouldMakeCorrectChanges(bool subCheck)
		{
			using (var mock = AutoMock.GetLoose())
			{
				var userList = GetSampleUsers();
				mock.Mock<IDataAccess>()
					.Setup(x => x.ModifierUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>()))
					.Returns(Task.FromResult(default(object)))
					.Verifiable();
				mock.Mock<IDataAccess>()
					.Setup(x => x.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, It.IsAny<string>()))
					.ReturnsAsync(new DatabaseUser())
					.Verifiable();
				mock.Mock<IDataAccess>()
					.Setup(x => x.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, "00000000"))
					.ReturnsAsync(new DatabaseUser
					{
						Id = 1,
						Minutes = "10",
						Points = "3000",
						Rank = "0",
						UserId = "00000000"
					})
					.Verifiable();
				mock.Mock<IDataAccess>()
					.Setup(x => x.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, "00000003"))
					.ReturnsAsync(new DatabaseUser
					{
						Id = 1,
						Minutes = "10",
						Points = "5",
						Rank = "0",
						UserId = "00000003"
					})
					.Verifiable();
				mock.Mock<IDataAccess>()
					.Setup(x => x.ModifyUserIdRankAsync(It.IsAny<string>(), It.IsAny<int>()))
					.Returns(Task.FromResult(default(object)));
				mock.Mock<ITwitchConnections>();
				mock.Mock<IApiRetriever>()
					.SetupGet(x => x.TwitchChannelId)
					.Returns("11122233");
				mock.Mock<IApiRetriever>()
					.Setup(x => x.GetChannelSubscribersAsync("11122233"))
					.ReturnsAsync(GetSampleUserSubscribers());
				mock.Mock<IConfiguration>()
					.Setup(x => x.PointsMultiplier)
					.Returns(2);
				mock.Mock<IFilterManager>()
					.Setup(x => x.CheckIfUserIdFiltered("00000001"))
					.Returns(true);
				var cls = mock.Create<DataProcessor>();
				await cls.AddToUserAsync(userList, subCheck: subCheck).ConfigureAwait(false);
				mock.Mock<IDataAccess>()
					.Verify(x => x.ModifierUserIdAsync("00000000", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
						Times.Exactly(1));
				mock.Mock<IDataAccess>()
					.Verify(x => x.ModifierUserIdAsync("00000001", It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()),
						Times.Exactly(0));
				mock.Mock<IDataAccess>()
					.Verify(
						x => x.ModifierUserIdAsync("00000002",
							It.Is<int>(i => i == 2 && subCheck || i == 1 && subCheck == false), 0, 0),
						Times.Exactly(1));
				mock.Mock<IDataAccess>()
					.Verify(
						x => x.ModifierUserIdAsync("00000003",
							It.Is<int>(i => i == 1 && subCheck || i == 1 && subCheck == false), 0, 0),
						Times.Exactly(1));
				mock.Mock<IDataAccess>()
					.Verify(x => x.ModifyUserIdRankAsync("00000000", It.IsAny<int>()), Times.Exactly(1));
				mock.Mock<IDataAccess>()
					.Verify(x => x.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints,
						It.Is<string>(z => z == "00000000" || z == "00000002" || z == "00000003")));
				mock.Mock<IDataAccess>()
					.VerifyNoOtherCalls();
			}
		}


		private List<IUserBase> GetSampleUserSubscribers()
		{
			var output = new List<IUserBase>
			{
				new UserBase("Pantalonus", "00000002")
			};
			return output;
		}

		private List<IUserBase> GetSampleUsers()
		{
			var output = new List<IUserBase>
			{
				new UserBase("dingo", "00000000"),
				new UserBase("Mantoly", "00000001"),
				new UserBase("Pantalony", "00000002"),
				new UserBase("Gouliver","00000003")
			};
			return output;
		}

		[Fact]
		public async void AddToUserAsync_ShouldThrowAndHandleException()
		{
			using (var mock = AutoMock.GetLoose())
			{
				var userList = GetSampleUsers();
				var user = userList[1];
				mock.Mock<IDataAccess>()
					.Setup(x => x.ModifierUserIdAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
						It.IsAny<int>()))
					.Returns(Task.FromResult(default(object)));
				mock.Mock<IApiRetriever>()
					//NOTE this exception is not thrown for both
					.Setup(x => x.GetUserIdAsync(It.IsAny<string>()))
					.Throws(new Exception("Test Exception Handling"));
				mock.Mock<IApiRetriever>()
					.Setup(x => x.GetChannelSubscribersAsync(It.IsAny<string>()))
					.Throws(new Exception("Test Exception Handling"));
				mock.Mock<IConfiguration>()
					.Setup(x => x.PointsMultiplier)
					.Returns(2);
				mock.Mock<IFilterManager>()
					.Setup(x => x.CheckIfUserIdFiltered(user.UserId))
					.Returns(true);
				var cls = mock.Create<DataProcessor>();
				mock.Mock<IApiRetriever>();
				await Assert.ThrowsAnyAsync<Exception>(() => cls.AddToUserAsync(userList)).ConfigureAwait(false);
			}
		}
	}
}
