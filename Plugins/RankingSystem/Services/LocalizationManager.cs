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
using System.IO;
using Newtonsoft.Json;
using RankingSystem.Interfaces;

namespace RankingSystem.Services
{
	public class LocalizationManager : ILocalizationManager
	{
		private Dictionary<string, Dictionary<string, string>> _translations;

		// Map specific country codes to language codes
		private readonly Dictionary<string, string> _countryToLanguageMap = new Dictionary<string, string>
		{
			{ "ua", "ru" }, // Ukraine falls back to Russian
			{ "by", "ru" }, // Belarus
			{ "lt", "ru" },
			{ "ru", "ru" },
			{ "de", "de" }, // Germany, Austria, and Switzerland fall back to German
			{ "at", "de" },
			{ "ch", "de" },
			{ "us", "en" }, // USA falls back to English
			{ "ca", "en" }, // Canada falls back to English
			{ "ae", "ae" },
			{ "cz", "cz" },
			{ "ir", "ir" },
			{ "pl", "pl" },
			{ "tr", "tr" },
			{ "hu", "hu" },
			{ "fi", "fi" }, 
			// Add more mappings as needed
		};

		public LocalizationManager()
		{
			_translations = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
			LoadAllLanguages();
		}

		public void LoadTranslations(string languageFilePath)
		{
			try
			{
				string json = File.ReadAllText(languageFilePath);
				_translations = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error loading translations: {ex.Message}");
			}
		}

		private void LoadAllLanguages()
		{
			string languagesPath = Path.Combine(Directory.GetCurrentDirectory(), "languages");
			foreach (var file in Directory.GetFiles(languagesPath, "*.json"))
			{
				var languageCode = Path.GetFileNameWithoutExtension(file);
				var jsonContent = File.ReadAllText(file);
				var translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent);

				if (translations != null)
				{
					_translations[languageCode] = translations;
					//Console.WriteLine($"Language added {file}");
				}
			}
		}

		public string GetTranslation(string countryCode, string key)
		{
			countryCode = (countryCode ?? "en").ToLowerInvariant();

			// Find the language code for the given country, or fall back to English
			if (!_countryToLanguageMap.TryGetValue(countryCode, out string languageCode))
			{
				languageCode = "en"; // Default to English if country code is not mapped
			}

			// Attempt to get the translation for the resolved language code
			if (_translations.TryGetValue(languageCode, out var translations) && translations.TryGetValue(key, out var translation))
			{
				//Console.WriteLine($"Language successfully resolved! Key: {key} translation: {translation}");
				return translation;
			}

			// Fallback to English if key is missing in resolved language
			if (_translations.TryGetValue("en", out var defaultTranslations) && defaultTranslations.TryGetValue(key, out var defaultTranslation))
			{
				//Console.WriteLine($"Key missing in language!{countryCode}");
				return defaultTranslation;
			}

			// Return the key as a last resort
			return key;
		}
	}
}
