using System.Collections.Generic;
using Autofac.Extras.Moq;
using EvilBot.Utilities;
using EvilBot.Utilities.Resources.Interfaces;
using Xunit;

namespace EvilBot.Tests
{
	public class PollManagerTests
	{
		[Fact]
		public void PollCreate_ReturnCorrectStringAndStoreDataCorrectly()
		{
			using (var mock = AutoMock.GetLoose())
			{
				mock.Mock<IDataAccess>();
				mock.Create<PollManager>();
				var itemList = new List<string>
				{
					"cat",
					"dog",
					"pig",
					"hamster"
				};
			}
		}
	}
}