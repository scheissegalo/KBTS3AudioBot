using LiteDB;
using RankingSystem.Interfaces;
using RankingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TSLib;

namespace RankingSystem.Services
{
	public class MockUserRepository : IUserRepository
	{
		private readonly LiteDatabase _db;
		private readonly ILiteCollection<TSUser> _dbUsers;

		public MockUserRepository(string databasePath = "ts_users.db")
		{
			// Initialize the LiteDB database
			_db = new LiteDatabase($"Filename={databasePath};Upgrade=true;");
			_dbUsers = _db.GetCollection<TSUser>("tsusers");
		}

		public TSUser? FindOne(Uid uid) =>
			_dbUsers.FindOne(u => u.UserID == (Uid)uid);

		public TSUser? FindOneByName(string name) =>
			_dbUsers.FindOne(u => u.Name == name);

		public TSUser? FindOneByPredicate(Expression<Func<TSUser, bool>> predicate) =>
			_dbUsers.FindOne(predicate);

		public void Insert(TSUser user) =>
			_dbUsers.Insert(user);

		public void Upsert(TSUser user) =>
			_dbUsers.Upsert(user);

		public void Update(TSUser user) =>
			_dbUsers.Update(user);

		public TSUser FindById(TSUser id) =>
			_dbUsers.FindById(id.Id);

		public IEnumerable<TSUser> GetAll() =>
			_dbUsers.FindAll();
	}
}

