using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using System;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using FirstLight.FLogger;
using FirstLight.Game.Data;
using FirstLight.Game.Services;
using FirstLight.Game.UIElements;
using FirstLight.Game.Utils;
using FirstLight.Game.UI;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace FirstLight.Game.Presenters
{
	[UILayer(UILayer.Popup)]
	public class PrivacyDialogPresenter : UIPresenterData<PrivacyDialogPresenter.StateData>
	{
		public class StateData
		{
			public Action OnAccept;
		}

		private Button _privacy;
		private Button _terms;
		private LocalizedButton _confirm;
		private IGameServices _services;

		protected override void QueryElements()
		{
			_services = MainInstaller.ResolveServices();
			_terms = Root.Q<Button>("TermsOfServiceButton");
			_privacy = Root.Q<Button>("PrivacyButton");

			_confirm = Root.Q<LocalizedButton>("ConfirmButton").Required();
			_confirm.clicked += Data.OnAccept;

			_terms.clicked += () =>
			{
				DownloadAndShow("Terms of Service", null, HardCodedTexts.TERMS_OF_SERVICE).Forget();
			};

			_privacy.clicked += () =>
			{
				DownloadAndShow("Privacy Policy", null, HardCodedTexts.PRIVACY_POLICY).Forget();
			};
		}

		private async UniTaskVoid DownloadAndShow(string title, string url, string defaultValue)
		{
			var data = new GenericScrollingTextDialogPresenter.StateData()
			{
				Title = title,
				OnConfirm = () => _services.UIService.OpenScreen<PrivacyDialogPresenter>(Data).Forget()
			};
			if (url == null)
			{
				data.Text = defaultValue;
			}
			else
			{
				var www = UnityWebRequest.Get(url);
				await www.SendWebRequest();
				if (www.result == UnityWebRequest.Result.Success)
				{
					FLog.Verbose("Downloaded privacy policy");
					data.Text = RemoveUnicodeAndColorCodes(www.downloadHandler.text);
				}
				else
				{
					FLog.Verbose("Using default privacy policy");
					data.Text = defaultValue;
				}
			}

			await _services.UIService.OpenScreen<GenericScrollingTextDialogPresenter>(data);
		}

		private string RemoveUnicodeAndColorCodes(string text)
		{
			string cleanedText = Regex.Replace(text, @"\p{Cs}", ""); // Remove Unicode characters
			cleanedText = Regex.Replace(cleanedText, @"\u001B\[\d{1,2}m", ""); // Remove color codes (e.g., \u001B[32m)
			return cleanedText;
		}
	}

	public static class HardCodedTexts
	{
		public const string PRIVACY_POLICY = @"<b>Development privacy notice — placeholder</b>
This restored project is a development build. Its configured account and multiplayer services process the data needed to provide those features. Device test results are saved locally. Optional analytics follows the tracking preference selected on this device.
Before distributing this build, the game operator must provide a privacy policy covering the configured services and their actual data handling.";

		public const string TERMS_OF_SERVICE = @"<b>Development terms — placeholder</b>
This restored project is provided for development and evaluation. Virtual items and currencies are gameplay data.
Before distributing this build, the game operator must provide appropriate terms of service. The repository license and individual dependency licenses apply to development use.";
	}
}
