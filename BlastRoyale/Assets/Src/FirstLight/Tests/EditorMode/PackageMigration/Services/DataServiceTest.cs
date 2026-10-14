using FirstLight.Game.Services;
using System.Collections.Generic;
using GameLovers.Services;
using NSubstitute;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace FirstLightEditor.Services.Tests
{
	[TestFixture]
	public class DataServiceTest
	{
		private ClientDataService _dataService;

		// ReSharper disable once MemberCanBePrivate.Global
		public interface IDataMockup {}

		[SetUp]
		public void Init()
		{
			_dataService = new ClientDataService();
		}

		[Test]
		public void AddData_Successfully()
		{
			var data = Substitute.For<IDataMockup>();
			
			_dataService.AddData(data, false);

			Assert.AreSame(data, _dataService.GetData<IDataMockup>());
		}
	}
}