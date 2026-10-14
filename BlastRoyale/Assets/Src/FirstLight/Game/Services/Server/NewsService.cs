using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using FirstLight.Game.Data;
using Newtonsoft.Json;
using PlayFab.ClientModels;
using UnityEngine;

namespace FirstLight.Game.Services
{
	public interface INewsService
	{
		UniTask<List<TitleNewsItem>> GetNews();
		UniTask<bool> HasNotSeenNews();
	}

	/// <summary>Reads authored news from Resources/BundledNews.json and persists the last viewed article ID.</summary>
	public sealed class BundledNewsService : INewsService
	{
		private readonly ClientDataService _localData = new ClientDataService();
		private readonly NewsData _data;
		private readonly List<TitleNewsItem> _news;

		public BundledNewsService()
		{
			_data = _localData.LoadData<NewsData>();
			var catalog = Resources.Load<TextAsset>("BundledNews");
			if (catalog == null) throw new InvalidOperationException("Resources/BundledNews.json is required for the bundled news service.");
			_news = (JsonConvert.DeserializeObject<List<TitleNewsItem>>(catalog.text) ?? new List<TitleNewsItem>())
				.OrderByDescending(article => article.Timestamp).Take(30).ToList();
			if (_news.Any(article => string.IsNullOrWhiteSpace(article.NewsId) || string.IsNullOrWhiteSpace(article.Body)))
				throw new InvalidOperationException("Bundled news articles require stable NewsId and authored Body JSON.");
		}

		public UniTask<List<TitleNewsItem>> GetNews()
		{
			var latest = _news.FirstOrDefault();
			if (latest != null)
			{
				_data.LastNewsId = latest.NewsId;
				_localData.SaveData<NewsData>();
			}
			return UniTask.FromResult(new List<TitleNewsItem>(_news));
		}

		public UniTask<bool> HasNotSeenNews() => UniTask.FromResult(_news.Count > 0 && _data.LastNewsId != _news[0].NewsId);
	}
}
