using System.Reflection;

namespace EvilBot.Resources
{
	public static class StandardMessages
	{

		//TODO move command formats inside commands, and try to make it auto generated for every command
		public static class BotInformation
		{
			public static string AboutBot { get; } = $"EvilBot v{Assembly.GetEntryAssembly().GetName().Version}beta by M0rtuary";

			public static string ChangelogBot { get; } = "Changelog: https://bit.ly/evil-changelog";
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

		public static class BetMessages
		{

			public static string EndBetFormat { get; } = "/me !bet end <YES(1), NO(2)>";

			public static string CreateFromat { get; } = "/me !bet create <betName>";

			public static string MakeVoteFormat { get; } = "/me !bet vote <XP> <YES(1), NO(2)>";

			public static string OptionInvalid { get; } = "/me Optiunea este invalida. Te rog alege YES(1) or NO(2)";

			public static string MinPoints { get; } = "/me Trebuie sa pariezi minim 1XP";

			public static string NotActive { get; } = "/me Nu exista pariu activ!";

			public static string Locked { get; } = "/me Pariatul a fost INCHIS. Nu poti sa faci sau sa retragi pariuri.";
		}

		public static class ErrorMessages
		{
			public static string BigError { get; } = "/me Ok. UMM, SOMETHING HUGE FAILED, PLEASE REPORT ERORR";

			/// <summary>
			/// <c>{0}</c> is the Name
			/// </summary>
			public static string UserMissing { get; } = "/me Utilizatorul \"{0}\" nu exista!";

			/// <summary>
			/// <c>{0}</c> is the Name
			/// </summary>
			public static string InvalidName { get; } =  "/me Numele \"{0}\" este invalid.";

			public static string NotNumber { get; } = "/me Optiunea data nu este un numar!";

			/// <summary>
			/// <c>{0}</c> is the username of the user not present.
			/// </summary>
			public static string NotInDatabase { get; } = "/me \"{0}\" nu este inca in baza de date.";

			public static string NotEnoughPoints { get; } = "/me Nu ai destul XP.";

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

		/// <summary>
		/// <c>{0}</c> is LurkerPoints
		/// <c>{1}</c> is LurkerMinutes
		/// <c>{2}</c> is TalkerPoints
		/// <c>{3}</c> is TalkerMinutes
		/// </summary>
		public static string PointRate { get; } = "/me Lurker: {0}XP pe {1} minute; Talker: {2}XP pe {3} minute";


	}
}
