using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Temporary native settings migration. Both version writers must use the same Resources key.
public static class VersionResourceConfiguration
{
    public static void Run()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType("GameLovers.Services.Versioning.Editor.VersioningEditorSettings", false))
            .FirstOrDefault(t => t != null) ?? throw new InvalidOperationException("Pinned Services version settings unavailable.");
        var singleton = type.BaseType.GetProperty("instance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        type.GetMethod("SetResourcesFolderPath").Invoke(singleton, new object[] { "Assets/Resources" });
        if ((string)type.GetProperty("ResourcesFolderPath").GetValue(singleton) != "Assets/Resources")
            throw new InvalidOperationException("Native version resource setting did not persist.");
        Debug.Log("[Version Resources] Both writers use Assets/Resources.");
    }
}
