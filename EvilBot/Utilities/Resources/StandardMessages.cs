namespace EvilBot.Utilities.Resources
{
	public static class StandardMessages
	{
		public static string ManageCommandText { get; } =
			"/me Command format !manage <username> <(-)pointnumber> <(-)minutenumber>m";

		public static string PollCreateText { get; } =
			"/me Command format !pollcreate option1 | option2 | [option3] | [option4]";

		public static string PollVoteText { get; } = "/me Command format !pollvote <1,2,3,4>";
		public static string PollNotActiveText { get; } = "/me Nu exista poll activ!";

		public static string ComenziText { get; } =
			"/me !rank !manage(mod) !pollcreate(mod) !pollvote !pollstats !pollend(mod) !filter(mod)";

		public static string FilterText { get; } = "/me !filter get/add/remove <username>";
		public static string UserMissingText { get; } = "/me Acest User nu exista!";
		public static string BigError { get; } = "/me Ok. UMM, SOMETHING HUGE FAILED, PLEASE REPORT ERORR";
	}
}