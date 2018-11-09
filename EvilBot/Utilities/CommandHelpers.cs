using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities
{
	public static class CommandHelpers
	{
		//returns, first string is minutes, second is
		public static List<string> ManageCommandSorter(string stringOne, string stringTwo)
		{
			if (stringOne.EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
			{
				var temporary = stringTwo;
				stringTwo = stringOne;
				stringOne = temporary;
			}

			var stringOrder = new List<string>();
			stringOrder.Add(stringOne);
			stringOrder.Add(stringTwo);
			return stringOrder;
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

		public static string PollOptionsStringBuilder(List<IPollItem> pollItems)
		{
			var builder = new StringBuilder();
			builder.Append("<1");
			for (int i = 2; i <= pollItems.Count; i++)
			{
				builder.AppendFormat(",{0}", i);
			}

			builder.Append(">");
			return builder.ToString();
		}
	}
}