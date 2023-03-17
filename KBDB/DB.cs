using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace KBDB
{
	public class DB
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public int Age { get; set; }

		public static List<DB> LoadData(string filePath)
		{
			if (!File.Exists(filePath))
			{
				return new List<DB>();
			}

			string jsonString = File.ReadAllText(filePath);
			return JsonSerializer.Deserialize<List<DB>>(jsonString);
		}

		public static void SaveData(string filePath, List<DB> data)
		{
			string jsonString = JsonSerializer.Serialize(data);
			File.WriteAllText(filePath, jsonString);
		}

		public static void AddData(string filePath, DB newData)
		{
			List<DB> dataList = LoadData(filePath);
			newData.Id = dataList.Count == 0 ? 1 : dataList.Max(d => d.Id) + 1;
			dataList.Add(newData);
			SaveData(filePath, dataList);
		}

		public static void DeleteData(string filePath, int id)
		{
			List<DB> dataList = LoadData(filePath);
			DB dataToDelete = dataList.FirstOrDefault(d => d.Id == id);
			if (dataToDelete != null)
			{
				dataList.Remove(dataToDelete);
				SaveData(filePath, dataList);
			}
		}

		public static void UpdateData(string filePath, int id, DB updatedData)
		{
			List<DB> dataList = LoadData(filePath);
			DB dataToUpdate = dataList.FirstOrDefault(d => d.Id == id);
			if (dataToUpdate != null)
			{
				dataToUpdate.Name = updatedData.Name;
				dataToUpdate.Age = updatedData.Age;
				SaveData(filePath, dataList);
			}
		}

	}
}
