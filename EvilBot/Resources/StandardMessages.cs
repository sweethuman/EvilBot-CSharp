namespace EvilBot.Resources
{
	public static class StandardMessages
	{
		public static string ManageCommandText { get; } =
			"/me Command format !manage <username> <(-)pointnumber> <(-)minutenumber>m";

		public static string PollCreateText { get; } =
			"/me Command format !pollcreate option1 | option2 | [option3] | [option4]";

		public static string PollVoteText { get; } = "/me Foloseste !pollvote <1,2,3,4>";

		public static string PollVoteNotNumber { get; } = "/me Optiunea data nu este un numar!";

		public static string PollNotActiveText { get; } = "/me Nu exista poll activ!";

		public static string ComenziText { get; } =
			"/me !rank !ranklist !top !pollvote !pollstats !pollcreate(mod) !pollend(mod) !manage(mod) !filter(mod) !giveaway(mod)";

		public static string FilterText { get; } = "/me !filter get/add/remove <username>";

		public static string BigError { get; } = "/me Ok. UMM, SOMETHING HUGE FAILED, PLEASE REPORT ERORR";

		//TODO change this to String.format format
		public static string UserMissingText(string name)
		{
			return $"/me \"{name}\" nu exista!";
		}

		public static string InvalidName(string name)
		{
			return $"/me Numele \"{name}\" este invalid.";
		}
	}
}
