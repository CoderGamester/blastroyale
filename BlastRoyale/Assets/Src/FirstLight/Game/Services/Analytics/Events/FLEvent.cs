using Unity.Services.Analytics;

namespace FirstLight.Game.Services.Analytics.Events
{
	public abstract class FLEvent : Event
	{
		protected FLEvent(string name) : base(name)
		{
		}
	}
}
