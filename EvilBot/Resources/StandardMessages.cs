namespace EvilBot.Resources
{
	public static class StandardMessages
	{
		public static class BotInformation
		{
			public static string AboutBot { get; } = "EvilBot v0.6.0.0beta by M0rtuary";

			public static string ChangelogBot { get; } = "Changelog: https://goo.gl/mLvcct";
		}

		public static class PollMessages
		{

			public static string PollDefaultFormat { get; } = "/me !poll vote/stats";
			public static string PollModFormat { get; } = "/me !poll vote/stats/create/end";

			public static string PollCreateFormat { get; } =
				"/me Command format !poll create option1 | option2 | [option3] | [option4]";

			public static string PollVoteFormat { get; } = "/me Foloseste !poll vote <1,2,3,4>";

			public static string PollVoteNotNumber { get; } = "/me Optiunea data nu este un numar!";

			public static string PollNotActive { get; } = "/me Nu exista poll activ!";
		}

		public static class UserErrorMessages
		{
			/// <summary>
			/// <c>{0}</c> is the Name
			/// </summary>
			public static string UserMissing { get; } = "/me Utilizatorul \"{0}\" nu exista!";

			/// <summary>
			/// <c>{0}</c> is the Name
			/// </summary>
			public static string InvalidName { get; } =  "/me Numele \"{0}\" este invalid.";

			public static string NotNumber { get; } = "/me Optiunea data nu este un numar!";

			//TODO make a not nuber with the format
//			/// <summary>
//			/// <c>{0}</c> is the option reported as not a number
//			/// </summary>
//			public static string NotNumberFormatted { get; } = "/me \"{0}\" nu este un numar!";
		}

		public static string Comenzi { get; } =
			"/me !rank !ranklist !top !pointrate !pollvote !pollstats !pollcreate(mod) !pollend(mod) !manage(mod) !filter(mod) !giveaway(mod) !about !changelog";

		public static string ManageCommandFormat { get; } =
			"/me Command format !manage <username> <(-)pointnumber> <(-)minutenumber>m";

		public static string FilterFormat { get; } = "/me !filter get/add/remove <username>";

		public static string BigError { get; } = "/me Ok. UMM, SOMETHING HUGE FAILED, PLEASE REPORT ERORR";


		/// <summary>
		/// <c>{0}</c> is LurkerPoints
		/// <c>{1}</c> is LurkerMinutes
		/// <c>{2}</c> is TalkerPoints
		/// <c>{3}</c> is TalkerMinutes
		/// </summary>
		public static string PointRate { get; } = "/me Lurker: {0}XP pe {1} minute; Talker: {2}XP pe {3} minute";


	}
}
