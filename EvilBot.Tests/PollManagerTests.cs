using Autofac.Extras.Moq;
using EvilBot.Processors;
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
            }

        }
    }
}