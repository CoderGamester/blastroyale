using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Install temporarily under Assets/Editor to exercise the real Boot scene in the target Editor.
[InitializeOnLoad]
public static class MainBootSmoke
{
    private const string Active = "BlastModernization.BootSmoke.Active";
    private const string Deadline = "BlastModernization.BootSmoke.Deadline";
    private const string Errors = "BlastModernization.BootSmoke.Errors";
    private const string Entered = "BlastModernization.BootSmoke.Entered";

    static MainBootSmoke()
    {
        if (!SessionState.GetBool(Active, false)) return;
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Editor is already playing");
        SessionState.SetBool(Active, true);
        SessionState.SetBool(Entered, false);
        SessionState.SetInt(Errors, 0);
        SessionState.SetString(Deadline, DateTime.UtcNow.AddSeconds(45).Ticks.ToString());
        EditorApplication.update += Update;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        Application.logMessageReceived += OnLog;
        EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        SessionState.SetBool(Entered, true);
        SessionState.SetString(Deadline, DateTime.UtcNow.AddSeconds(25).Ticks.ToString());
    }

    private static void OnLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            SessionState.SetInt(Errors, SessionState.GetInt(Errors, 0) + 1);
    }

    private static void Update()
    {
        if (!long.TryParse(SessionState.GetString(Deadline, ""), out var ticks) || DateTime.UtcNow.Ticks < ticks) return;
        var entered = SessionState.GetBool(Entered, false);
        var errors = SessionState.GetInt(Errors, 0);
        SessionState.SetBool(Active, false);
        EditorApplication.update -= Update;
        Application.logMessageReceived -= OnLog;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        var path = Environment.GetEnvironmentVariable("BLAST_BOOT_SMOKE_RESULT");
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Set BLAST_BOOT_SMOKE_RESULT to an ignored output path");
        File.WriteAllText(path, "{\"entered_play_mode\":" + entered.ToString().ToLowerInvariant() + ",\"error_logs\":" + errors + "}\n");
        Debug.Log("[Blast Boot Smoke] EnteredPlayMode=" + entered + "; runtime error logs=" + errors);
        EditorApplication.Exit(entered && errors == 0 ? 0 : 2);
    }
}
