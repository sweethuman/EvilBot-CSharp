using System;
using System.Threading.Tasks;
using System.Timers;

namespace EvilBot
{
    internal interface IDataProcessor
    {
        Task<string[]> GetPointsMinutesAsync(string userID);

        Task<string> GetUserIdAsync(string username);

        Task<TimeSpan?> GetUptimeAsync();

        void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

        void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);
    }
}