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
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;

namespace EvilBot
{
	public static class ContainerConfig
	{
		public static IContainer Config()
		{
			var builder = new ContainerBuilder();
			builder.RegisterType<App>().As<IApplication>().SingleInstance();
			builder.RegisterType<DataProcessor>().As<IDataProcessor>().SingleInstance();
			builder.RegisterType<CommandProcessor>().As<ICommandProcessor>().SingleInstance();
			builder.RegisterType<SqliteDataAccess>().As<IDataAccess>().SingleInstance();
			builder.RegisterType<LoggerUtility>().As<ILoggerUtility>().SingleInstance();
			builder.RegisterType<TwitchChatBot>().As<ITwitchChatBot>().SingleInstance();
			builder.RegisterType<TwitchConnections>().As<ITwitchConnections>().SingleInstance();
			builder.RegisterType<PollManager>().As<IPollManager>().SingleInstance();
			builder.RegisterType<SetsFilterManager>().As<IFilterManager>().SingleInstance();
			builder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
			builder.RegisterType<ApiRetriever>().As<IApiRetriever>().SingleInstance();
			builder.RegisterType<SetsTalkerCounter>().As<ITalkerCounter>().SingleInstance();
			builder.RegisterType<SetsPresenceCounter>().As<IPresenceCounter>().SingleInstance();
			builder.RegisterType<RankManager>().As<IRankManager>().SingleInstance();

			return builder.Build();
		}
	}
}
