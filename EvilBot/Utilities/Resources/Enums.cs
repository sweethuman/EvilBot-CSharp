namespace EvilBot.Utilities.Resources
{
	public static class Enums
	{
		public enum DatabaseUserPointsOrderRow
		{
			Points,
			Minutes,
			Rank,
			None
		}

		public enum DatabaseTables
		{
			UserPoints,
			FilteredUsers
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