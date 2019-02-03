using Autofac;
using EvilBot.Managers;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot;
using EvilBot.TwitchBot.Commands;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;

namespace EvilBot
{
	public static class ContainerConfig
	{

		public static readonly IContainer Container = Config();

		private static IContainer Config()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<App>().As<IApplication>().SingleInstance();
			builder.RegisterType<DataProcessor>().As<IDataProcessor>().SingleInstance();
			builder.RegisterType<SqliteDataAccess>().As<IDataAccess>().SingleInstance();
			builder.RegisterType<LoggerUtility>().As<ILoggerUtility>().SingleInstance();
			builder.RegisterType<TwitchChatBot>().As<ITwitchChatBot>().SingleInstance();
			builder.RegisterType<TwitchConnections>().As<ITwitchConnections>().SingleInstance();
			builder.RegisterType<ApiRetriever>().As<IApiRetriever>().SingleInstance();
			builder.RegisterType<PollManager>().As<IPollManager>().SingleInstance();
			builder.RegisterType<RankManager>().As<IRankManager>().SingleInstance();
			builder.RegisterType<BetManager>().As<IBetManager>().SingleInstance();
			builder.RegisterType<SetsFilterManager>().As<IFilterManager>().SingleInstance();
			builder.RegisterType<SetsTalkerCounter>().As<ITalkerCounter>().SingleInstance();
			builder.RegisterType<SetsPresenceCounter>().As<IPresenceCounter>().SingleInstance();
			builder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
			builder.RegisterType<CommandsContainer>().AsSelf().SingleInstance();

			builder.RegisterType<FilterCommand>().AsSelf().SingleInstance();
			builder.RegisterType<GiveawayCommand>().AsSelf().SingleInstance();
			builder.RegisterType<ManageCommand>().AsSelf().SingleInstance();
			builder.RegisterType<PollCommand>().AsSelf().SingleInstance();
			builder.RegisterType<RankCommand>().AsSelf().SingleInstance();
			builder.RegisterType<TopCommand>().AsSelf().SingleInstance();
			builder.RegisterType<AboutCommand>().AsSelf().SingleInstance();
			builder.RegisterType<ChangelogCommand>().AsSelf().SingleInstance();
			builder.RegisterType<PointRateCommand>().AsSelf().SingleInstance();
			builder.RegisterType<RankListCommand>().AsSelf().SingleInstance();
			builder.RegisterType<HelpCommand>().AsSelf().SingleInstance();
			builder.RegisterType<BetCommand>().AsSelf().SingleInstance();

			return builder.Build();
		}
	}
}
