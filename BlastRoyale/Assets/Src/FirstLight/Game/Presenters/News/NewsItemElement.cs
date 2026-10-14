using MainInstaller = FirstLight.Game.Utils.MainInstaller;
using FirstLight.Game.Services.Infrastructure;
using System;
using System.Globalization;
using FirstLight.FLogger;
using FirstLight.Game.Utils;
using FirstLight.Game.UI;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace FirstLight.Game.Views.UITK
{
	[Serializable]
	public class NewsItemData
	{
		public string Image;
		public string Text;
		public string Category;
		public NewsActionButtonData[] Buttons;
	}

	public class TitleNews
	{
		public DateTime Date;
		public string DateFormatted;
		public string Title;
		public NewsItemData Data;

		public TitleNews() { }

		public TitleNews(TitleNewsItem playfabItem)
		{
			Date = playfabItem.Timestamp;
			DateFormatted = Date.ToShortDateString();
			Title = playfabItem.Title;
			Data = JsonConvert.DeserializeObject<NewsItemData>(playfabItem.Body);
			Data.Category ??= "Game";
		}
	}

	/// <summary>
	/// Handles a news (journal) item
	/// </summary>
	[UxmlElement]
	public partial class NewsItemElement : VisualElement
	{
		private static VisualTreeAsset _itemResource;

		private Label _title;
		private Label _date;
		private Label _text;
		private VisualElement _image;
		private VisualElement _actions;
		private VisualElement _hot;
		private VisualElement _hotShine;

		private string _uxmlTitle = "Test Title";
		[UxmlAttribute("title")]
		public string Title
		{
			get => _uxmlTitle;
			set { _uxmlTitle = value; ScheduleUxmlPreview(); }
		}
		private string _uxmlText = "test text";
		[UxmlAttribute("text")]
		public string Text
		{
			get => _uxmlText;
			set { _uxmlText = value; ScheduleUxmlPreview(); }
		}
		private string _uxmlImageUrl = "";
		[UxmlAttribute("image-url")]
		public string ImageUrl
		{
			get => _uxmlImageUrl;
			set { _uxmlImageUrl = value; ScheduleUxmlPreview(); }
		}
		private string _uxmlCategory = "Game";
		[UxmlAttribute("category")]
		public string Category
		{
			get => _uxmlCategory;
			set { _uxmlCategory = value; ScheduleUxmlPreview(); }
		}
		private string _uxmlDate = "01/01/1981";
		[UxmlAttribute("date")]
		public string Date
		{
			get => _uxmlDate;
			set { _uxmlDate = value; ScheduleUxmlPreview(); }
		}

		private bool _hasRuntimeData;
		private IVisualElementScheduledItem _previewRefresh;

		private void ScheduleUxmlPreview()
		{
			if (_hasRuntimeData) return;
			_previewRefresh?.Pause();
			_previewRefresh = schedule.Execute(() =>
			{
				if (_hasRuntimeData) return;
				ApplyData(new TitleNews
				{
					Title = Title, DateFormatted = Date,
					Data = new NewsItemData { Text = Text, Image = ImageUrl, Category = Category }
				});
			});
		}

		public NewsItemElement()
		{
			if (_itemResource == null)
			{
				_itemResource = Resources.Load<VisualTreeAsset>("NewsItem");
			}
			_itemResource.CloneTree(this);

			_title = this.Q<Label>("Title").Required();
			_date = this.Q<Label>("Date").Required();
			_text = this.Q<Label>("Text").Required();
			_image = this.Q<VisualElement>("ContentImage").Required();
			_actions = this.Q("Controls").Required();
			_hot = this.Q("Hot").Required();
			_hotShine = this.Q("HotShine").Required();
			RegisterCallback<AttachToPanelEvent>(_ => ScheduleUxmlPreview());
		}

		public void SetHot(bool hot)
		{
			_hot.SetDisplay(hot);
			_hotShine.SetDisplay(hot);
			if (hot)
			{
				_hotShine.AddRotatingEffect(5, 1);
				_hotShine.AnimatePing();
				_hot.AnimatePing();
			}
		}

		public void SetData(TitleNews newsItem)
		{
			_hasRuntimeData = true;
			_previewRefresh?.Pause();
			ApplyData(newsItem);
		}

		private void ApplyData(TitleNews newsItem)
		{
			_date.text = newsItem.DateFormatted;
			_title.text = newsItem.Title;
			var data = newsItem.Data;
			_text.text = data.Text;
			if (!string.IsNullOrWhiteSpace(data.Image) && Application.isPlaying)
			{
				_image.SetDisplay(true);
				MainInstaller.ResolveServices().RemoteTextureService.SetTexture(_image, data.Image);
			}
			else
			{
				_image.SetDisplay(false);
			}

			_actions.Clear();
			if (newsItem.Data.Buttons == null || newsItem.Data.Buttons.Length == 0)
			{
				_actions.SetDisplay(false);
				return;
			}

			_actions.SetDisplay(true);
			foreach (var btnData in newsItem.Data.Buttons)
			{
				_actions.Add(new NewsActionButton().SetData(btnData));
			}
		}
	}
}
