using System;
using System.Collections.Generic;
using System.IO;
using FirstLight.Game.Services;
using FirstLight.Game.Utils;
using Quantum;
using UnityEditor;
using UnityEngine;
using LogType = UnityEngine.LogType;

// Observation only: use the normal Editor UI and physical/simulated OS controls to play.
// This never creates a runner, changes input, sends a game command, or advances a frame.
public static class InteractiveGameplayObserver
{
    [Serializable] public class MatchProof
    {
        public int FirstFrame, LastFrame, Bots, MovementPolls, ShootingPolls, LocalAttacks;
        public string Entity, FirstPosition, LastPosition;
        public double GreatestDistance;
    }
    [Serializable] public class Proof
    {
        public string UnityVersion;
        public string ServicesVersion;
        public List<MatchProof> Matches = new List<MatchProof>();
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }
    private static readonly object Owner = new object();
    private static readonly Proof Evidence = new Proof();
    private static QuantumGame _game;
    private static IPlayerInputService _input;
    private static MatchProof _match;
    private static EntityRef _entity;
    private static Photon.Deterministic.FPVector2 _origin;
    private static bool _hasOrigin;
    private static string _path;
    private static double _nextSave;

    public static string Run(string outputPath)
    {
        _path = outputPath;
        Evidence.UnityVersion = Application.unityVersion;
        Evidence.ServicesVersion = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.gamelovers.services/package.json").version;
        QuantumEvent.SubscribeManual<EventOnPlayerAttack>(Owner, OnAttack);
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
        return "Observing normal native games and sent input until Editor Play stops.";
    }
    private static void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
            Evidence.Errors.Add(message);
        if (type == LogType.Warning) Evidence.Warnings.Add(message);
    }
    private static void OnAttack(EventOnPlayerAttack ev)
    {
        if (_match != null && ReferenceEquals(ev.Game, _game) && ev.PlayerEntity == _entity)
            _match.LocalAttacks++;
    }
    private static void OnInput(Quantum.Input input)
    {
        if (_match == null) return;
        if (input.Direction != Photon.Deterministic.FPVector2.Zero) _match.MovementPolls++;
        if (input.IsShooting) _match.ShootingPolls++;
    }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            if (_input != null) _input.OnQuantumInputSent -= OnInput;
            QuantumEvent.UnsubscribeListener(Owner);
            EditorApplication.update -= Tick;
            Application.logMessageReceived -= OnLog;
            Save();
            return;
        }
        var game = QuantumRunner.Default?.Game;
        if (!ReferenceEquals(game, _game))
        {
            if (_input != null) _input.OnQuantumInputSent -= OnInput;
            _input = null;
            _game = game;
            _hasOrigin = false;
            _entity = EntityRef.None;
            _match = null;
            if (game != null)
            {
                _match = new MatchProof();
                Evidence.Matches.Add(_match);
            }
        }
        if (_game?.Frames.Verified != null && _match != null)
        {
            var frame = _game.Frames.Verified;
            _match.LastFrame = frame.Number;
            _match.Bots = frame.ComponentCount<BotCharacter>();
            if (_input == null && MainInstaller.TryResolve<IMatchServices>(out var services))
            {
                _input = services.PlayerInputService;
                _input.OnQuantumInputSent += OnInput;
            }
            if (_game.GetLocalPlayers().Count > 0)
            {
                var entity = _game.GetLocalPlayerEntityRef();
                if (frame.Exists(entity) && frame.TryGet<Transform2D>(entity, out var transform))
                {
                    _entity = entity;
                    if (!_hasOrigin)
                    {
                        _hasOrigin = true;
                        _origin = transform.Position;
                        _match.FirstFrame = frame.Number;
                        _match.FirstPosition = _origin.ToString();
                        _match.Entity = entity.ToString();
                    }
                    var delta = transform.Position - _origin;
                    var distance = Math.Sqrt(delta.X.AsDouble * delta.X.AsDouble + delta.Y.AsDouble * delta.Y.AsDouble);
                    _match.GreatestDistance = Math.Max(distance, _match.GreatestDistance);
                    _match.LastPosition = transform.Position.ToString();
                }
            }
        }
        if (EditorApplication.timeSinceStartup >= _nextSave)
        {
            _nextSave = EditorApplication.timeSinceStartup + 1;
            Save();
        }
    }
    private static void Save() => File.WriteAllText(_path, Newtonsoft.Json.JsonConvert.SerializeObject(Evidence, Newtonsoft.Json.Formatting.Indented));
}
