using Autofac;

namespace EvilBot
{
    public static class ContainerConfig
    {
        public static IContainer Config()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Application>().As<IApplication>().SingleInstance();
            builder.RegisterType<DataProcessor>().As<IDataProcessor>().SingleInstance();
            builder.RegisterType<SqliteDataAccess>().As<IDataAccess>().SingleInstance();
            builder.RegisterType<LoggerManager>().As<ILoggerManager>().SingleInstance();
            builder.RegisterType<TwitchChatBot>().As<ITwitchChatBot>().As<ITwitchConnections>().SingleInstance();
            builder.RegisterType<PollManager>().As<IPollManager>().SingleInstance();

            return builder.Build();
        }
    }
}