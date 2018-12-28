namespace EvilBot.Resources
{
	public static class Enums
	{
		public enum DatabaseTables
		{
			UserPoints,
			FilteredUsers
		}

		public enum DatabaseUserPointsOrderRow
		{
			Points,
			Minutes,
			Rank,
			None
		}

		public enum FilteredUsersDatabaseAction
		{
			Remove,
			Insert
		}

		public enum PollAddVoteFinishState
		{
			PollNotActive,
			VoteAdded,
			OptionOutOfRange,
			VoteFailed,
			AlreadyVoted
		}
	}
}