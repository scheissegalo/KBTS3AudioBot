using RankingSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using TSLib;

namespace RankingSystem.Interfaces
{
	public interface IUserRepository
	{
		TSUser? FindOne(Uid uid);
		TSUser? FindOneByName(string name);
		TSUser? FindOneByPredicate(Expression<Func<TSUser, bool>> predicate);
		void Insert(TSUser user);
		void Upsert(TSUser user);
		void Update(TSUser user);
		TSUser FindById(TSUser id);
		IEnumerable<TSUser> GetAll();
	}
}
