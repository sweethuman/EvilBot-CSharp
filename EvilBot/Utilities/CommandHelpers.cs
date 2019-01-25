using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client.Models;
using UserType = TwitchLib.Client.Enums.UserType;

namespace EvilBot.Utilities
{
	public static class CommandHelpers
	{
		/// <summary>
		///     This takes the minutes and points strings and arranges them;
		/// </summary>
		/// <param name="stringOne"></param>
		/// <param name="stringTwo"></param>
		/// <returns>First string is minutes. Second string is points</returns>
		public static (string minutesString, string pointsString) ManageCommandSorter(string stringOne,
			string stringTwo)
		{
			if ((stringTwo ?? "0").EndsWith("m", StringComparison.InvariantCultureIgnoreCase) ||
			    !(stringOne ?? "0").EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
			{
				var temporary = stringOne;
				stringOne = stringTwo;
				stringTwo = temporary;
			}

			stringOne = stringOne?.Trim('m', 'M');

			return (stringOne, stringTwo);
		}

		public static List<string> FilterAndPreparePollOptions(string arguments)
		{
			arguments = arguments.Trim();
			arguments = arguments.Trim('|');
			arguments = arguments.Trim();
			var options = arguments.Split('|').ToList();
			for (var i = 0; i < options.Count; i++) options[i] = options[i].Trim();
			options.RemoveAll(string.IsNullOrEmpty);
			return options;
		}

		public static string OptionsStringBuilder(int countOfOptions)
		{
			var builder = new StringBuilder();
			builder.Append("<1");
			for (var i = 2; i <= countOfOptions; i++) builder.AppendFormat(",{0}", i);

			builder.Append(">");
			return builder.ToString();
		}

		public static T ChangeOutputIfMod<T>(UserType type, T userOutput, T modOutput)
		{
			return type < UserType.Moderator ? userOutput : modOutput;
		}
	}
}
