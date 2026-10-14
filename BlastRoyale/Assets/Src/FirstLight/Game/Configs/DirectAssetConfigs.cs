using System;
using GameLovers.Services.AssetsImporter;

namespace FirstLight.Game.Configs
{
    /// <summary>Game configuration entries that embed values rather than weak asset links.</summary>
    public abstract class DirectAssetConfigs<TId, TValue> : AssetConfigsScriptableObjectBase<TId, TValue>
    {
        public override Type AssetType => typeof(TValue);
    }
}
