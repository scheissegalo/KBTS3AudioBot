namespace RankingSystem.Interfaces
{
	public interface ILocalizationManager
	{
		string GetTranslation(string languageCode, string key);
		void LoadTranslations(string languageFilePath);
	}
}
