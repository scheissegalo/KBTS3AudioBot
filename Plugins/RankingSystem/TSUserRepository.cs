// KBTS3AudioBot - An advanced Musicbot for Teamspeak 3
// Copyright (C) 2024 KBTS3AudioBot contributors
// https://github.com/scheissegalo/KBTS3AudioBot
// This program is free software: you can redistribute it and/or modify
// it under the terms of the Open Software License v. 3.0
//
// You should have received a copy of the Open Software License along with this
// program. If not, see <https://opensource.org/licenses/OSL-3.0>.

using LiteDB;
using System.Collections.Generic;
using System;
using TSLib;
using RankingSystem.Models;

namespace RankingSystem
{
	public class TSUserRepository
	{
		private readonly LiteDatabase _db;
		private readonly ILiteCollection<TSUser> _dbUsers;

		public TSUserRepository(string databasePath = "ts_users.db")
		{
			// Initialize the LiteDB database
			_db = new LiteDatabase($"Filename={databasePath};Upgrade=true;");
			_dbUsers = _db.GetCollection<TSUser>("tsusers");
		}

		// Example: Find a single user by a predicate
		public TSUser FindOne(Uid? userId)
		{
			return _dbUsers.FindOne(u => u.UserID == userId);
		}
		public TSUser FindOneByName(string? userName)
		{
			return _dbUsers.FindOne(u => u.Name == userName);
		}
		public TSUser? FindOneByPredicate(System.Linq.Expressions.Expression<Func<TSUser, bool>> predicate)
		{
			return _dbUsers.FindOne(predicate);
		}

		// Example: Add or update a user
		public void Upsert(TSUser user)
		{
			_dbUsers.Upsert(user);
		}

		public void Update(TSUser user)
		{
			//Console.WriteLine($"Updating user {user.Name} ID:{user.UserID} chanID: {user.ChannelID}");
			_dbUsers.Update(user);
		}
		public TSUser FindById(TSUser id)
		{
			return _dbUsers.FindById(id.Id);
		}
		public void Insert(TSUser user)
		{
			_dbUsers.Insert(user);
		}

		// Example: Get all users
		public IEnumerable<TSUser> GetAll()
		{
			return _dbUsers.FindAll();
		}
	}
}
