using Autofac;
using EvilBot.Processors;

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

            return builder.Build();
        }
    }
}