using PlayFab.Internal;

namespace PlayFab.SharedModels
{

    public class PlayFabBaseModel
    {
        public string ToJson()
        {
            var json = PluginManager.GetPlugin<ISerializerPlugin>(PluginContract.PlayFab_Serializer);
            return json.SerializeObject(this);
        }
    }

    public class PlayFabRequestCommon : PlayFabBaseModel
    {
        public PlayFabAuthenticationContext AuthenticationContext;
    }

    public class PlayFabResultCommon : PlayFabBaseModel
    {
        public PlayFabRequestCommon Request;
        public object CustomData;
    }

    public class PlayFabLoginResultCommon : PlayFabResultCommon
    {
        public PlayFabAuthenticationContext AuthenticationContext;
    }

    public class PlayFabResult<TResult> where TResult : PlayFabResultCommon
    {
        public TResult Result;
        public object CustomData;
    }
}
