using System;
using System.Collections.Generic;
using FirstLight.Game.TestCases.Helpers;

namespace FirstLight.Game.TestCases
{
	/// <summary>Typed test-helper registry. Helpers are concrete classes, unlike service interfaces.</summary>
	public sealed class TestInstaller
	{
		private readonly Dictionary<Type, TestHelper> _helpers = new Dictionary<Type, TestHelper>();

		public void Bind<T>(T instance) where T : TestHelper
		{
			if (instance == null) throw new ArgumentNullException(nameof(instance));
			_helpers.Add(typeof(T), instance);
		}

		public T Resolve<T>() where T : TestHelper => (T)_helpers[typeof(T)];

		public void OnGameAwaken()
		{
			foreach (var helper in _helpers.Values) helper.OnGameAwaken();
		}
	}
}
