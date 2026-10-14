using System;
using System.IO;
using System.Reflection;
using FirstLight.Editor.Build;
using FirstLight.Game.Utils;
using UnityEditor;
using UnityEngine;

// Temporary native Editor probe: verifies CI configures a previously unlinked player from ignored JSON.
public static class CloudBuildConfigurationSmoke
{
    public static void Run()
    {
        var setter = typeof(PlayerSettings).GetMethod("SetCloudProjectId", BindingFlags.Static | BindingFlags.NonPublic);
        var configure = typeof(Builder).GetMethod("ConfigureCloudProjectFromLocalConfiguration", BindingFlags.Static | BindingFlags.NonPublic);
        if (setter == null || configure == null) throw new MissingMethodException("Native cloud build configuration");
        var original = Application.cloudProjectId;
        var empty = false;
        var matched = false;
        try
        {
            setter.Invoke(null, new object[] { string.Empty });
            empty = string.IsNullOrEmpty(Application.cloudProjectId);
            configure.Invoke(null, null);
            matched = string.Equals(Application.cloudProjectId, FLEnvironment.Configuration.UnityProjectId, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            setter.Invoke(null, new object[] { original });
            AssetDatabase.SaveAssets();
        }
        var path = Environment.GetEnvironmentVariable("BLAST_CLOUD_BUILD_RESULT");
        if (string.IsNullOrEmpty(path)) throw new InvalidOperationException("Set BLAST_CLOUD_BUILD_RESULT");
        File.WriteAllText(path, "{\"native_player_initially_empty\":" + empty.ToString().ToLowerInvariant() +
            ",\"build_config_matches_ignored_json\":" + matched.ToString().ToLowerInvariant() +
            ",\"original_native_player_setting_restored\":" + (Application.cloudProjectId == original).ToString().ToLowerInvariant() + "}\n");
        Debug.Log("[Cloud Build Configuration] Native empty-player to ignored configuration: " + (empty && matched));
        EditorApplication.Exit(empty && matched ? 0 : 1);
    }
}
