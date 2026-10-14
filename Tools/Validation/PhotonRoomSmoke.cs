using System;
using System.IO;
using Photon.Realtime;
using UnityEditor;
using UnityEngine;

// Real SDK/cloud smoke. Install temporarily under Assets/Editor; App ID stays in ignored local JSON.
public static class PhotonRoomSmoke
{
    [Serializable] private class LocalConfig { public string PhotonQuantumAppId = ""; }
    private static RealtimeClient _client;
    private static DateTime _deadline;
    private static bool _requestedRoom;
    private static bool _connected;

    public static void Run()
    {
        var config = JsonUtility.FromJson<LocalConfig>(File.ReadAllText(Path.Combine(Application.dataPath, "Resources/LocalClientConfig.json")));
        if (!Guid.TryParse(config.PhotonQuantumAppId, out _)) throw new InvalidOperationException("Configure a real Photon App ID first");
        _client = new RealtimeClient { ClientType = ClientAppType.Quantum };
        _client.AuthValues = new AuthenticationValues("blast-smoke-" + Guid.NewGuid().ToString("N"));
        _deadline = DateTime.UtcNow.AddSeconds(50);
        EditorApplication.update += Update;
        if (!_client.ConnectUsingSettings(new AppSettings { AppIdQuantum = config.PhotonQuantumAppId, AppVersion = "BlastModernizationQuantum3Smoke", FixedRegion = "eu" }))
            Finish(false, "connect_request_rejected");
    }

    private static void Update()
    {
        _client.Service();
        if (_client.State == ClientState.ConnectedToMasterServer && !_requestedRoom)
        {
            _connected = true;
            _requestedRoom = true;
            if (!_client.OpCreateRoom(new EnterRoomArgs {
                RoomName = "blast-smoke-" + Guid.NewGuid().ToString("N"),
                RoomOptions = new RoomOptions { MaxPlayers = 1, IsVisible = false, PlayerTtl = 0, EmptyRoomTtl = 0 }
            })) Finish(false, "room_request_rejected");
        }
        if (_client.InRoom) Finish(true, "joined_private_ephemeral_room");
        else if (DateTime.UtcNow > _deadline) Finish(false, _client.State.ToString());
    }

    private static void Finish(bool joined, string outcome)
    {
        EditorApplication.update -= Update;
        _client.Disconnect();
        var path = Environment.GetEnvironmentVariable("BLAST_PHOTON_SMOKE_RESULT");
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Set BLAST_PHOTON_SMOKE_RESULT to an ignored output path");
        File.WriteAllText(path, "{\"quantum_client\":true,\"connected_to_master\":" + _connected.ToString().ToLowerInvariant() +
            ",\"joined_private_room\":" + joined.ToString().ToLowerInvariant() + ",\"outcome\":\"" + outcome + "\"}\n");
        Debug.Log("[Photon Room Smoke] " + outcome);
        EditorApplication.Exit(joined ? 0 : 2);
    }
}
