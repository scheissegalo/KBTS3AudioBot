// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RankingSystem
{
	internal class StatisticsModule
	{
		private OnlineCounterModule onlineCounterModule;
		private OnboardingModule boardingModule; // maybe need to Grab Users

		private List<UserStatistic> userStatistics = new List<UserStatistic>();
		private bool initialCheck = true;

		public StatisticsModule(OnlineCounterModule ocm, OnboardingModule boardingModule)
		{
			this.onlineCounterModule = ocm;
			this.boardingModule = boardingModule;
		}

		public void StartStatisticsModule()
		{
			//await Task.Delay(200);
			Console.WriteLine("Statistics Module initialized!");
			//LogUserCount();
		}

		public void LogUserCount()
		{
			if (initialCheck)
			{
				initialCheck = false;
			}
			else
			{
				if (!onlineCounterModule.isChecking)
				{
					userStatistics.Add(new UserStatistic
					{
						Timestamp = DateTime.UtcNow,
						UserCount = onlineCounterModule.count
					});

					// Save to file for persistence
					SaveUserStatisticsToFile();
				}

			}
		}

		private void SaveUserStatisticsToFile()
		{
			List<UserStatistic> currentStatistics;

			// Read existing file data if the file exists
			if (System.IO.File.Exists("user_statistics.json"))
			{
				var existingData = System.IO.File.ReadAllText("user_statistics.json");
				currentStatistics = JsonConvert.DeserializeObject<List<UserStatistic>>(existingData) ?? new List<UserStatistic>();
			}
			else
			{
				currentStatistics = new List<UserStatistic>();
			}

			// Add the new entry
			currentStatistics.Add(new UserStatistic
			{
				Timestamp = DateTime.UtcNow,
				UserCount = onlineCounterModule.count // or however you’re capturing the count
			});

			// Write back the entire updated list
			System.IO.File.WriteAllText("user_statistics.json", JsonConvert.SerializeObject(currentStatistics));
			//Console.WriteLine("Statistics Recorded!");
		}

		public async Task TestTask()
		{
			await Task.Delay(1000);
			Console.WriteLine("Tested AFK done!");
		}

		public class UserStatistic
		{
			public DateTime Timestamp { get; set; }
			public uint UserCount { get; set; }
		}
	}
}
