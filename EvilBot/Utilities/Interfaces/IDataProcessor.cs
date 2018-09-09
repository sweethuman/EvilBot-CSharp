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

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.

        void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

        void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.

        string GetRank(string pointsString);
    }
}