using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Authentication.Generated;
using Unity.Services.Authentication.Shared;
using Unity.Services.Core.Environments.Internal;
using Unity.Services.Core.Scheduler.Internal;
using Player = Unity.Services.Authentication.Generated.Player;

namespace FirstLight.Validation.Collaborators
{
    // Test-only collaborators implement the imported SDK contracts; no SDK type or namespace is redefined.
    sealed class MemoryCache : IAuthenticationCache
    {
        readonly Dictionary<string, string> _values = new()
        {
            ["session_token"] = "synthetic-local-session", ["player_id"] = "synthetic-lifecycle-player"
        };
        public string Profile => "default";
        public string CloudProjectId => "synthetic-project";
        public bool HasKey(string key) => _values.ContainsKey(key);
        public void DeleteKey(string key) => _values.Remove(key);
        public void SetString(string key, string value) => _values[key] = value;
        public string GetString(string key) => _values.GetValueOrDefault(key);
        public void Migrate(string key) { }
    }

    sealed class ControlledScheduler : IActionScheduler
    {
        readonly Dictionary<long, Action> _actions = new();
        long _next;
        public long ScheduleAction(Action action, double delaySeconds = 0) { var id = ++_next; _actions[id] = action; return id; }
        public void CancelAction(long actionId) => _actions.Remove(actionId);
        public void Execute(long actionId) { var action = _actions[actionId]; _actions.Remove(actionId); action(); }
    }

    sealed class LocalEnvironment : IEnvironments { public string Current => "development"; }
    sealed class LocalMetrics : IAuthenticationMetrics
    {
        public void SendNetworkErrorMetric() { }
        public void SendExpiredSessionMetric() { }
        public void SendClientInvalidStateExceptionMetric() { }
        public void SendUnlinkExternalIdNotFoundExceptionMetric() { }
        public void SendClientSessionTokenNotExistsExceptionMetric() { }
    }

    sealed class LocalNames : IPlayerNamesApi
    {
        public IApiConfiguration Configuration { get; } = new ApiConfiguration();
        public string GetBasePath() => "synthetic-local";
        public Task<ApiResponse<Player>> GetNameAsync(string playerId, bool? autoGenerate = null, bool? showMetadata = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ApiResponse<Player> { Data = new Player(playerId, "NativePlayer#0001") });
        public Task<ApiResponse<Player>> UpdateNameAsync(string playerId, UpdateNameRequest updateNameRequest, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Name updates are not part of lifecycle regression.");
    }

    sealed class LocalNetwork : IAuthenticationNetworkClient
    {
        readonly SignInResponse _response;
        readonly Action _called;
        public LocalNetwork(SignInResponse response, Action called) { _response = response; _called = called; }
        public Task<SignInResponse> SignInAnonymouslyAsync(SignInAnonymouslyRequest request) { _called(); return Task.FromResult(_response); }
        public Task<SignInResponse> SignInWithSessionTokenAsync(SessionTokenRequest request) { _called(); return Task.FromResult(_response); }
        public Task<SignInResponse> GenerateRestrictedTokenAsync(SessionTokenRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> SignInWithExternalTokenAsync(string idProvider, SignInWithExternalTokenRequest externalToken) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<LinkResponse> LinkWithExternalTokenAsync(string idProvider, LinkWithExternalTokenRequest externalToken) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<UnlinkResponse> UnlinkExternalTokenAsync(string idProvider, UnlinkRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<PlayerInfoResponse> GetPlayerInfoAsync(string playerId) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task DeleteAccountAsync(string playerId) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> SignInWithUsernamePasswordAsync(UsernamePasswordRequest credentials) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> SignUpWithUsernamePasswordAsync(UsernamePasswordRequest credentials) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> AddUsernamePasswordAsync(UsernamePasswordRequest credentials) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> UpdatePasswordAsync(UpdatePasswordRequest credentials) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<GenerateCodeResponse> GenerateSignInCodeAsync(GenerateSignInCodeRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<CodeLinkConfirmResponse> ConfirmCodeAsync(ConfirmSignInCodeRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<SignInResponse> SignInWithCodeAsync(SignInWithCodeRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<CodeLinkInfoResponse> GetCodeIdentifierAsync(CodeLinkInfoRequest request) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
        public Task<GetNotificationsResponse> GetNotificationsAsync(string playerId) => throw new NotSupportedException("Unexpected network operation in lifecycle regression.");
    }
}
