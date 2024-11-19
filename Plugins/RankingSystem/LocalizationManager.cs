using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace RankingSystem
{
	public class LocalizationManager
	{
		private readonly Dictionary<string, Dictionary<string, string>> _translations;

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
			_translations = new Dictionary<string, Dictionary<string, string>>();
			LoadAllLanguages();
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
			// Ensure languageCode is not null and normalize it to lowercase
			countryCode = (countryCode ?? "en").ToLowerInvariant();

			//Console.WriteLine($"Recieved Translation request. Code: {countryCode} key: {key}");

			// Normalize the country code to uppercase for consistency
			//countryCode = countryCode?.ToUpperInvariant();

			// Find the language code for the given country, or fall back to English
			if (!_countryToLanguageMap.TryGetValue(countryCode, out string languageCode))
			{
				languageCode = "en"; // Default to English if country code is not mapped
				//Console.WriteLine($"Trying maps result: {languageCode}");
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

			// Final fallback if key is missing everywhere
			return $"[Missing translation: {key}]";

			// Fallback to English if language or key is not found
			//if (_translations.TryGetValue(languageCode, out var translations) && translations.TryGetValue(key, out var translation))
			//{
			//	return translation;
			//}

			//// Try to get the English version as a fallback
			//if (_translations.TryGetValue("en", out var defaultTranslations) && defaultTranslations.TryGetValue(key, out var defaultTranslation))
			//{
			//	return defaultTranslation;
			//}

			//// Final fallback if key is missing everywhere
			//return $"[Missing translation: {key}]";
		}

	}
}
