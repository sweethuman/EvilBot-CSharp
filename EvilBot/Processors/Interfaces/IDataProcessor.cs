using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace EvilBot.Processors
{
    internal interface IDataProcessor
    {
        event EventHandler<RankUpdateEventArgs> RankUpdated;

        Task<List<string>> GetUserAttributesAsync(string userID);

        Task<string> GetUserIdAsync(string username);

        Task<TimeSpan?> GetUptimeAsync();

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.

        void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

        void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.

        string GetRankFormatted(string rankString, string pointsString);
    }
}