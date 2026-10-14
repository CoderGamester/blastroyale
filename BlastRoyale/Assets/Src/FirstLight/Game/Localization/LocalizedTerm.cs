using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace FirstLight.Game.Localization
{
	/// <summary>A serialized localization key for timeline dialog text.</summary>
	[Serializable]
	public sealed class LocalizedTerm
	{
		[FormerlySerializedAs("mTerm"), SerializeField] private string _term;
		public override string ToString() => GameLocalization.GetTranslation(_term);
	}
}
