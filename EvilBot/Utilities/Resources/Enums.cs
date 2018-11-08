namespace EvilBot.Utilities.Resources
{
	public static class Enums
	{
		public enum DatabaseRow
		{
			Points,
			Minutes,
			Rank
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
			VoteFailed
		}
	}
}