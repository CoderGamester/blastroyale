using System;
using FirstLight.Game.Localization;
using NUnit.Framework;

namespace FirstLight.Tests.EditorMode.Models
{
	public sealed class LocalizationCatalogTests
	{
		[TestCase("en", "Adventure Completed!")]
		[TestCase("ru", "Приключение завершено!")]
		[TestCase("pt", "Aventura concluída!")]
		public void ReadsPreservedTranslations(string language, string expected)
		{
			Assert.That(GameLocalization.GetTranslation("AdventureMenu/AdventureCompleted", language), Is.EqualTo(expected));
		}

		[Test]
		public void FallsBackToEnglishForUntranslatedText()
		{
			Assert.That(GameLocalization.GetTranslation("UITStandings/item_bonuses", "ru"), Is.EqualTo("ITEM BONUSES"));
		}

		[Test]
		public void MissingTermsRemainDetectable()
		{
			Assert.That(GameLocalization.TryGetTranslation("__missing_test_term__", out _), Is.False);
			Assert.That(GameLocalization.GetTranslation("__missing_test_term__"), Is.EqualTo("__missing_test_term__"));
		}

		[Test]
		public void RejectsUnsupportedLanguage()
		{
			Assert.Throws<ArgumentException>(() => GameLocalization.GetTranslation("AdventureMenu/AdventureCompleted", "unknown"));
		}
	}
}
