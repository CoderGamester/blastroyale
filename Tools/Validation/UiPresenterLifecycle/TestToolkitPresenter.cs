using Cysharp.Threading.Tasks;
using FirstLight.Game.UI;

namespace FirstLight.Tests.PlayMode.UI
{
    public class TestToolkitPresenter : UIPresenter
    {
        public int Queries { get; private set; }
        public int Opens { get; private set; }
        protected override void QueryElements() => Queries++;
        protected override UniTask OnScreenOpen(bool reload)
        {
            Opens++;
            return UniTask.CompletedTask;
        }
    }
}
