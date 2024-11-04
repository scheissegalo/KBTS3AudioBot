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

namespace RankingSystem
{
	public class Ranking : IBotPlugin
	{
		private TsFullClient tsFullClient;
		private Ts3Client ts3Client;
		private Connection serverView;
		private Constants constants = new Constants();
		public static Ranking Instance { get; private set; }

		//private readonly int UpdateInterval; //min

		private RankingModule ranking;
		private OnlineCounterModule onlineCounter;
		private AfkModule afk;
		private AdminModule admin;
		private StatisticsModule statistics;

		public Ranking(Ts3Client ts3Client, Connection serverView, TsFullClient tsFull)
		{
			this.ts3Client = ts3Client;
			this.tsFullClient = tsFull;
			this.serverView = serverView;

			Instance = this;
		}

		public async void Initialize()
		{
			// Build Modules
			ranking = new RankingModule(ts3Client, tsFullClient, serverView);
			onlineCounter = new OnlineCounterModule(ts3Client, tsFullClient, serverView);
			afk = new AfkModule(ts3Client, tsFullClient, serverView);
			admin = new AdminModule(ts3Client, tsFullClient, serverView);
			statistics = new StatisticsModule(onlineCounter);

			// Start Modules
			await ranking.StartRankingModule();
			onlineCounter.StartOnlineCounterModule();
			afk.StartAfkModule();
			admin.StartAdminModule();
			statistics.StartStatisticsModule();

			Console.WriteLine("All Modules Initialized!");
			Console.WriteLine("-- Ranking System Fully up and running! --");

			//Start main loop BLOCKS (await to infinity)!!!!!
			await StartLoop();

		}

		private async Task StartLoop()
		{
			while (true)
			{
				try
				{
					await afk.UserIdleCheck();
					await ranking.CheckOnlineUsers();
					await onlineCounter.CheckForDailyReset();
					statistics.LogUserCount();						
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.ToString());
				}
				await Task.Delay(TimeSpan.FromMinutes(constants.UpdateInterval));
			}
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
		}
	}
}
