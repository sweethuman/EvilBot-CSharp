namespace EvilBot
{
    public static class Factory
    {
        public static ILoggerManager GetLoggerManager()
        {
            return new LoggerManager();
        }

        public static IDataAccess GetDataAccess()
        {
            return new SqliteDataAccess();
        }
    }
}