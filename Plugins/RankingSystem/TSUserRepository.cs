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
