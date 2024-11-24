// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using System;
using TS3AudioBot;
using TS3AudioBot.CommandSystem;
using TS3AudioBot.Plugins;
using TSLib.Full.Book;
using TSLib.Full;
using LiteDB;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using RankingSystem.Services;
using RankingSystem.Interfaces;
using System.Linq;
//using NLog.Fluent;
//using NLog;

namespace RankingSystem
{
	public class Ranking : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();
		private Constants constants = new Constants();
		//private ConfigManagerService configManagerService = new ConfigManagerService();
		public static Ranking Instance { get; private set; }
		//LocalizationManager localizationManager = new LocalizationManager();
		private bool looping = true;

		//private RankingModule ranking;
		private OnlineCounterModule onlineCounter;
		private AfkModule afk;
		private AdminModule admin;
		private StatisticsModule statistics;
		//private OnboardingModule onlineboarding;
		private MockUserRepository mockRepo = new MockUserRepository();
		private OnboardingModule _onboardingModule;
		private Services.CommandManager commandManager = new Services.CommandManager();
		private ChannelManager channelManager;
		private IServerGroupManager serverGroupManager;
		private UserStatusUpdater userStatusUpdater;
		private Services.LocalizationManager localizationManager = new Services.LocalizationManager();

		public Ranking(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;
			//this.Log = _log;

			Instance = this;
		}

		public async void Initialize()
		{
			// Build Modules
			//ranking = new RankingModule(ts3Client, tsFullClient, serverView);
			//configManagerService.LoadConfig();
			onlineCounter = new OnlineCounterModule(ts3Client, tsFullClient, serverView);
			afk = new AfkModule(ts3Client, tsFullClient, serverView, mockRepo);
			admin = new AdminModule(ts3Client, tsFullClient, serverView);

			channelManager = new ChannelManager(tsFullClient);
			serverGroupManager = new ServerGroupManager(tsFullClient);
			userStatusUpdater = new UserStatusUpdater(mockRepo, serverGroupManager, channelManager, localizationManager, tsFullClient);
			_onboardingModule = new OnboardingModule(mockRepo, commandManager, channelManager, serverGroupManager, localizationManager, userStatusUpdater, onlineCounter, tsFullClient);
			statistics = new StatisticsModule(onlineCounter, _onboardingModule);

			//NLog.LogManager.Configuration = NLog.LogManager.Configuration ?? new NLog.Config.XmlLoggingConfiguration("NLog.config");


			//onlineboarding = new OnboardingModule(TSUser);

			// Start Modules
			//await ranking.StartRankingModule();
			onlineCounter.StartOnlineCounterModule();
			afk.StartAfkModule();
			admin.StartAdminModule();
			_onboardingModule.StartOnboardingModule();
			//await _onboardingModule.CheckUser();
			await userStatusUpdater.CheckUser();
			//await userStatusUpdater.UpdateUsers();

			statistics.StartStatisticsModule();
			await onlineCounter.CheckOnlineUsers(true);
			//Console.WriteLine("All Modules Initialized!");
			//Console.WriteLine($"NLog conf loded: {NLog.LogManager.Configuration}!");
			//Console.WriteLine($"LogManager.Configuration: {NLog.LogManager.Configuration != null}");
			//Console.WriteLine($"NLog config path: {NLog.LogManager.Configuration?}");

			//Log.Info($"Logger name: {Log.Name}");
			//Console.WriteLine($"Logger name: {Log.Name}");
			//Console.WriteLine("-- Ranking System Fully up and running! --");
			Log.Info("-- Ranking System Fully up and running! --");

			//Start main loop BLOCKS (await to infinity)!!!!!dfd
			await StartLoop();
		}

		private async Task StartLoop()
		{
			while (looping)
			{
				try
				{
					await afk.UserIdleCheck();
					//await ranking.CheckOnlineUsers();
					await onlineCounter.CheckOnlineUsers(true);
					await onlineCounter.CheckForDailyReset();
					statistics.LogUserCount();
					//await _onboardingModule.CheckUser();
					await userStatusUpdater.CheckUser();
					//await userStatusUpdater.UpdateUsers();
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
				await Task.Delay(TimeSpan.FromMinutes(constants.UpdateInterval));
			}
		}

		[Command("talk2me")]
		public static async Task<string> CommandEcho(ClientCall invoker)
		{
			var user = await Instance.tsFullClient.ClientInfo(invoker.ClientId.Value);

			//string greeting = Instance.localizationManager.GetTranslation("ir", "greeting");
			string greeting = Instance.localizationManager.GetTranslation(user.Value.CountryCode, "greeting");

			string message = $"{greeting}: {user.Value.CountryCode}";
			return message;
		}

		[Command("rank")]
		public static string CommandRank(ClientCall invoker, string querystring)
		{
			// Retrieve the user's data from the database
			var db = new LiteDatabase("rank_users.db;Upgrade=true;");
			var usersCollection = db.GetCollection<RankingModule.User>("users");

			var user = usersCollection.FindOne(x => x.UserID == invoker.ClientUid.ToString());
			Console.WriteLine("Invoker: " + invoker.NickName + " UserInDB: " + user);

			if (user != null)
			{
				// Update the necessary fields
				TimeSpan timeSpan = TimeSpan.Parse(querystring);
				user.OnlineTime = timeSpan;
				user.LastUpdate = DateTime.Now;
				user.UpdateTime = true;

				// Update the user's data in the database
				usersCollection.Update(user);
				return $"User {user.Name} changed. New time: {user.OnlineTime.TotalDays} Days, {user.OnlineTime.Hours} hours and {user.OnlineTime.Minutes} minutes";
			}
			else
			{
				return "User not found.";
			}
		}

		[Command("resettimer")]
		public static async void ResetTimer(ClientCall invoker)
		{
			await Instance.onlineCounter.PerformDailyReset();
			//return "User not found.";
		}

		[Command("rank")]
		public static string CommandRank(ClientCall invoker, string DBuser, string query)
		{
			// Retrieve the user's data from the database
			var db = new LiteDatabase("rank_users.db;Upgrade=true;");
			var usersCollection = db.GetCollection<RankingModule.User>("users");

			var user = usersCollection.FindOne(x => x.Name == DBuser);
			Console.WriteLine("Invoker: " + invoker.NickName + " UserInDB: " + user);

			if (user != null)
			{
				// Update the necessary fields
				TimeSpan timeSpan = TimeSpan.Parse(query);
				user.OnlineTime = timeSpan;
				user.LastUpdate = DateTime.Now;
				user.UpdateTime = true;

				// Update the user's data in the database
				usersCollection.Update(user);

				return $"User {user.Name} changed. New time: {user.OnlineTime.TotalDays} Days, {user.OnlineTime.Hours} hours and {user.OnlineTime.Minutes} minutes";
			}
			else
			{
				return "User not found.";
			}

		}

		[Command("rankdelete")]
		public static string CommandRankDelete(ClientCall invoker)
		{
			// Retrieve the user's data from the database
			var db = new LiteDatabase("rank_users.db;Upgrade=true;");
			var usersCollection = db.GetCollection<RankingModule.User>("users");
			//usersCollection.Delete(Query.All());

			return "Database Cleaned";
			//}
		}

		[Command("importdb")]
		public static string ImportDataBase(ClientCall invoker)
		{
			try
			{
				var jsonData = System.IO.File.ReadAllText("exportedData.json");
				var jsonObj = JsonConvert.DeserializeObject<JObject>(jsonData);

				using (var newDb = new LiteDatabase(@"newDatabase.db"))
				{
					foreach (var collection in jsonObj.Properties())
					{
						var collectionName = collection.Name;
						var collectionData = collection.Value.ToObject<List<BsonDocument>>();

						var dbCollection = newDb.GetCollection<BsonDocument>(collectionName);
						dbCollection.InsertBulk(collectionData);
					}
				}

				Console.WriteLine("Data import completed.");
			}
			catch (Exception ex)
			{
				Console.WriteLine($"An error occurred: {ex.Message}");
			}

			return "Data import completed";
			//}
		}

		public void Dispose()
		{
			admin.Dispose();
			_onboardingModule.StopOnboardingModule();
			looping = false;
		}
	}
}
