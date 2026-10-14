using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace FirstLight.Game.Localization
{
	/// <summary>Bundled game translations with English fallback and no network dependency.</summary>
	public static class GameLocalization
	{
		[Serializable]
		public sealed class Language
		{
			public string Name = string.Empty;
			public string Code = string.Empty;
		}

		[Serializable]
		private sealed class Term
		{
			public string Key = string.Empty;
			public string[] Translations = Array.Empty<string>();
		}

		[Serializable]
		private sealed class Catalog
		{
			public Language[] Languages = Array.Empty<Language>();
			public Term[] Terms = Array.Empty<Term>();
		}

		private static Catalog _catalog;
		private static Dictionary<string, string[]> _translations;
		private static int _language;
		public static event Action LanguageChanged;

		public static IReadOnlyList<Language> Languages
		{
			get { EnsureLoaded(); return _catalog.Languages; }
		}

		public static string LanguageCode
		{
			get { EnsureLoaded(); return _catalog.Languages[_language].Code; }
			set
			{
				EnsureLoaded();
				var index = FindLanguage(value);
				if (index < 0) throw new ArgumentException("The requested language is not bundled.", nameof(value));
				if (index == _language) return;
				_language = index;
				PlayerPrefs.SetString("language_code", value);
				PlayerPrefs.Save();
				LanguageChanged?.Invoke();
			}
		}

		public static IReadOnlyList<string> GetTermsList()
		{
			EnsureLoaded();
			return _translations.Keys.ToArray();
		}

		public static string GetTranslation(string term, string language = null)
		{
			return TryGetTranslation(term, out var result, language) ? result : term ?? string.Empty;
		}

		public static bool TryGetTranslation(string term, out string result, string language = null)
		{
			EnsureLoaded();
			result = null;
			if (string.IsNullOrWhiteSpace(term) || !_translations.TryGetValue(term, out var translations)) return false;
			var index = language == null ? _language : FindLanguage(language);
			if (index < 0) throw new ArgumentException("The requested language is not bundled.", nameof(language));
			result = index < translations.Length ? translations[index] : null;
			if (string.IsNullOrWhiteSpace(result)) result = translations[0];
			return !string.IsNullOrWhiteSpace(result);
		}

		private static int FindLanguage(string language)
		{
			return Array.FindIndex(_catalog.Languages, candidate =>
				string.Equals(candidate.Code, language, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(candidate.Name, language, StringComparison.OrdinalIgnoreCase));
		}

		private static void EnsureLoaded()
		{
			if (_catalog != null) return;
			var asset = Resources.Load<TextAsset>("LocalizationCatalog");
			if (asset == null) throw new InvalidOperationException("The bundled LocalizationCatalog resource is missing.");
			var catalog = JsonConvert.DeserializeObject<Catalog>(asset.text);
			if (catalog?.Languages == null || catalog.Languages.Length == 0 || catalog.Terms == null)
				throw new InvalidOperationException("The bundled localization catalog is invalid.");
			var translations = new Dictionary<string, string[]>(StringComparer.Ordinal);
			foreach (var term in catalog.Terms)
			{
				if (string.IsNullOrWhiteSpace(term.Key) || term.Translations == null || term.Translations.Length != catalog.Languages.Length)
					throw new InvalidOperationException("A bundled localization term has invalid translations.");
				translations.Add(term.Key, term.Translations);
			}
			_catalog = catalog;
			_translations = translations;
			// Preserve the original project default (English) unless the player chose a language.
			_language = Math.Max(0, FindLanguage(PlayerPrefs.GetString("language_code", "en")));
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			_catalog = null;
			_translations = null;
			_language = 0;
			LanguageChanged = null;
		}
	}
}
