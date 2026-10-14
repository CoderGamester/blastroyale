using System;
using System.Threading;
using Unity.Services.Authentication;

namespace FirstLight.Game.Utils
{
	/// <summary>Captures the real signed-in UGS account and invalidates work across account sessions.</summary>
	public static class UgsAccountSession
	{
		private static readonly object Gate = new object();
		private static IAuthenticationService _authentication;
		private static long _generation;

		private static IAuthenticationService Subscribe()
		{
			var current = AuthenticationService.Instance;
			lock (Gate)
			{
				if (ReferenceEquals(current, _authentication)) return current;
				if (_authentication != null)
				{
					_authentication.SignedIn -= Invalidate;
					_authentication.SignedOut -= Invalidate;
					_authentication.Expired -= Invalidate;
					_authentication.PlayerIdChanged -= OnPlayerIdChanged;
				}
				_authentication = current;
				current.SignedIn += Invalidate;
				current.SignedOut += Invalidate;
				current.Expired += Invalidate;
				current.PlayerIdChanged += OnPlayerIdChanged;
				Invalidate();
			}
			return current;
		}

		private static void OnPlayerIdChanged(string playerId) => Invalidate();
		public static void Invalidate() => Interlocked.Increment(ref _generation);

		public static Snapshot Capture()
		{
			var authentication = Subscribe();
			if (!authentication.IsAuthorized || string.IsNullOrEmpty(authentication.PlayerId))
				throw new InvalidOperationException("An authorized Unity account is required for cloud player data.");
			var snapshot = new Snapshot(authentication.PlayerId, Interlocked.Read(ref _generation));
			snapshot.ThrowIfChanged();
			return snapshot;
		}

		public readonly struct Snapshot
		{
			public string PlayerId { get; }
			public long Generation { get; }
			internal Snapshot(string playerId, long generation)
			{
				PlayerId = playerId;
				Generation = generation;
			}

			public bool IsCurrent
			{
				get
				{
					var authentication = Subscribe();
					return authentication.IsAuthorized && authentication.PlayerId == PlayerId &&
						Interlocked.Read(ref _generation) == Generation;
				}
			}

			public void ThrowIfChanged()
			{
				if (!IsCurrent) throw new OperationCanceledException("The Unity account session changed during cloud player data work.");
			}
		}
	}
}
