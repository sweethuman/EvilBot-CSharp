using Autofac;
using EvilBot.Processors;
using EvilBot.Processors.Interfaces;
using EvilBot.TwitchBot;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;
using EvilBot.Utilities.Resources;
using EvilBot.Utilities.Resources.Interfaces;

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
            builder.RegisterType<LoggerManager>().As<ILoggerManager>().SingleInstance();
            builder.RegisterType<TwitchChatBot>().As<ITwitchChatBot>().SingleInstance();
            builder.RegisterType<TwitchConnections>().As<ITwitchConnections>().SingleInstance();
            builder.RegisterType<PollManager>().As<IPollManager>().SingleInstance();
            builder.RegisterType<FilterManager>().As<IFilterManager>().SingleInstance();
            builder.RegisterType<Configuration>().As<IConfiguration>().SingleInstance();
            builder.RegisterType<ApiRetriever>().As<IApiRetriever>().SingleInstance();

            return builder.Build();
        }
    }
}