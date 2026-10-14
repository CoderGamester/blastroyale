using FirstLight.Game.Services;
using FirstLight.Game.Localization;
using Quantum;
using UnityEngine.Playables;

namespace FirstLight.Game.Timeline
{
	/// <inheritdoc />
	/// <remarks>
	/// The <see cref="PlayableBehaviour"/> for showing videos on the generic dialog UI
	/// </remarks>
	[System.Serializable]
	public class GenericVideoBehaviour : PlayableBehaviourBase
	{
		public LocalizedTerm Title;
		public LocalizedTerm Description;
		public GameId VideoId;
		public LocalizedTerm ButtonText;

		/// <inheritdoc />
		protected override void OnEnter(Playable playable)
		{
			var confirmButton = new GenericDialogButton
			{
				ButtonText = ButtonText.ToString(),
				ButtonOnClick = Resume
			};

			// Legacy video dialogs have been removed, but preserved all the FTUE related functionality just in case
			// we need to reference it. This is required for FtueTrack.cs

			// Services.GenericDialogService.OpenVideoDialog(Title.ToString(), Description.ToString(), VideoId, false, confirmButton);
		}
	}
}
