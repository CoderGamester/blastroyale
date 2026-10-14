using FirstLight.Inspector;
using System;
using System.Globalization;
using FirstLight.Game.Services;
using FirstLight.Game.Utils.Attributes;
using FirstLight.Game.Localization;
using UnityEngine;

namespace FirstLight.Game.Configs.Utils
{
	[Serializable]
	public struct TransformParams
	{
		public Vector3 Position;
		public Vector3 Rotation;
		public Vector3 Scale;
	}

	[Serializable]
	public struct AnimatorParams
	{
		 public bool ApplyRootMotion;
		 public AnimatorUpdateMode UpdateMode;
		 public AnimatorCullingMode CullingMode;
		[InspectorField(Required = true)] public RuntimeAnimatorController Controller;
	}

	[Serializable]
	public class LocalizableString
	{
		[SerializeField] private bool _useLocalization;

		[LocalizationTerm, SerializeField, InspectorField(VisibleWhen = "_useLocalization")]
		private string _localizationTerm;

		[SerializeField, InspectorField(VisibleWhen = nameof(_useLocalization), InvertVisibility = true)]
		private string _text;

		public string LocalizationTerm => _localizationTerm;
		public string Text => _text;
		public bool UseLocalization => _useLocalization;

		public string GetText()
		{
			return _useLocalization ? GameLocalization.GetTranslation(_localizationTerm) : _text;
		}

		public static LocalizableString FromText(string text)
		{
			return new LocalizableString
			{
				_useLocalization = false,
				_text = text
			};
		}

		public static LocalizableString FromTerm(string term)
		{
			return new LocalizableString
			{
				_useLocalization = true,
				_localizationTerm = term
			};
		}
	}

	[Serializable]
	public class DurationConfig
	{
		public const string DATE_FORMAT = "dd/MM/yyyy-HH:mm";

		public static DateTime Parse(string datetime)
		{
			return DateTime.ParseExact(datetime, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
		}

		[InspectorField(Validate = "IsValidDate", ValidationMessage = "Invalid date format! Use: '" + DATE_FORMAT + "'")]
		public string StartsAt;

		[InspectorField(Validate = "IsValidDate", ValidationMessage = "Invalid date format! Use: '" + DATE_FORMAT + "'")]
		public string EndsAt;

		public static bool IsValidDate(string input)
		{
			return DateTime.TryParseExact(input, DATE_FORMAT, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _);
		}

		public DateTime GetStartsAtDateTime() => Parse(StartsAt);
		public DateTime GetEndsAtDateTime() => Parse(EndsAt);

		public bool Contains(DateTime dateTime)
		{
			return GetStartsAtDateTime() <= dateTime && GetEndsAtDateTime() > dateTime;
		}
	}
}
