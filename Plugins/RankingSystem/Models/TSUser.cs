// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using LiteDB;
using System;
using TSLib;

namespace RankingSystem.Models
{
	public class TSUser
	{
		public ObjectId Id { get; set; }
		public Uid UserID { get; set; }
		public ClientId ClientID { get; set; }
		public string Name { get; set; }
		public float Score { get; set; } = 0f;
		public string Nickname { get; set; }
		public bool SetupDone { get; set; } = false;
		public bool SkipSetup { get; set; } = false;
		public bool AcceptedRules { get; set; } = false;
		public int SetupStep { get; set; } = 0;
		public bool WantsOwnChannel { get; set; } = false;
		public bool WantsOwnChannelNotificationSend { get; set; } = false;
		//public bool wantsRanking { get; set; } = true;
		//public TimeSpan OnlineTime { get; set; }
		public DateTime SetupDate { get; set; }
		public DateTime LastUpdate { get; set; }
		public DateTime LastNotification { get; set; }
		public string SteamID { get; set; }
		public bool NotificationSend { get; set; } = false;
		public bool DailyStatusEnabled { get; set; } = true;
		public string CountryCode { get; set; } = "en";
		public bool RankingEnabled { get; set; } = true;
		public ChannelId ChannelID { get; set; }
		public ulong ChannelIDInt { get; set; } = 0;
		public bool isOnline { get; set; } = false;

		[BsonIgnore]
		public ulong ChannelIDValue
		{
			get => ChannelID.Value;
			set => ChannelID = ChannelId.To(value);
		}
		public TimeSpan OnlineTime { get; set; }

		public TSUser()
		{
			//InitializeDB();
		}

		//private LiteDatabase _db;
		//private ILiteCollection<TSUser> DBusers;
		//private bool isInitialized = false;

		//private void InitializeDB()
		//{
		//	try
		//	{
		//		// Initialize the LiteDB database and assign it to the class-level variable
		//		_db = new LiteDatabase(@"Filename=ts_users.db;Upgrade=true;");

		//		// Get or create the collection and assign it to the class-level variable
		//		DBusers = _db.GetCollection<TSUser>("tsusers");

		//		if (DBusers == null)
		//		{
		//			Console.WriteLine("Database collection 'tsusers' is null!");
		//			return;
		//		}

		//		Console.WriteLine("Database initialized successfully.");
		//	}
		//	catch (Exception ex)
		//	{
		//		Console.WriteLine(ex.ToString());
		//	}
		//}
	}
}
