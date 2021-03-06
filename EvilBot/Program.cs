﻿using System;
using System.Runtime.InteropServices;
using Autofac;
using EvilBot.Resources;
using Sentry;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local

namespace EvilBot
{
	internal static class Program
	{
		private const int MF_BYCOMMAND = 0x00000000;
		private const int SC_CLOSE = 0xF060;

		[DllImport("user32.dll")]
		private static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

		[DllImport("user32.dll")]
		private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

		[DllImport("kernel32.dll", ExactSpelling = true)]
		private static extern IntPtr GetConsoleWindow();

		private static void Main(string[] args)
		{
			DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);
			SetConsoleMode();
			using (SentrySdk.Init("https://e115c16b1ca7429aabb761ecacaaea74@sentry.io/1401263"))
			{
				using (var scope = ContainerConfig.Container.BeginLifetimeScope())
				{
					var app = scope.Resolve<IApplication>();
					app.Run();
					var stop = false;
					do
					{
						var keyPress = Console.ReadKey();
						if (keyPress.Modifiers == (ConsoleModifiers.Control | ConsoleModifiers.Shift) &&
						    keyPress.Key == ConsoleKey.Oem3) stop = true;
					} while (!stop);
					app.Stop();
				}
			}
		}

		private static void SetConsoleMode()
		{
			Console.Title = StandardMessages.BotInformation.AboutBot;
			Console.TreatControlCAsInput = true;
		}
	}
}
