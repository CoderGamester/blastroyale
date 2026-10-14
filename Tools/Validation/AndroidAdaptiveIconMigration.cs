using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class AndroidAdaptiveIconMigration
{
    private static PlatformIconKind Kind(Type type, string name) =>
        (PlatformIconKind)(type.GetProperty(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ??
        type.GetField(name, BindingFlags.Public | BindingFlags.Static)?.GetValue(null) ??
        throw new MissingMemberException(type.FullName, name));

    public static void Run()
    {
        var kinds = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Android.AndroidPlatformIconKind", false)).First(t => t != null);
        var adaptive = Kind(kinds, "Adaptive");
        var round = Kind(kinds, "Round");
        var legacy = Kind(kinds, "Legacy");
        var target = NamedBuildTarget.Android;
        var icons = PlayerSettings.GetPlatformIcons(target, adaptive);
        var populated = icons.First(i => i.GetTextures().Any(t => t != null));
        var textures = populated.GetTextures();
        foreach (var icon in icons) icon.SetTextures(textures);
        PlayerSettings.SetPlatformIcons(target, adaptive, icons);
        foreach (var kind in new[] { round, legacy })
        {
            var obsolete = PlayerSettings.GetPlatformIcons(target, kind);
            foreach (var icon in obsolete) icon.SetTextures(new Texture2D[icon.maxLayerCount]);
            PlayerSettings.SetPlatformIcons(target, kind, obsolete);
        }
        AssetDatabase.SaveAssets();
        if (PlayerSettings.GetPlatformIcons(target, adaptive).Any(i => i.GetTextures().Any(t => t == null)))
            throw new InvalidOperationException("Every native adaptive layer must retain authored icon textures.");
        if (new[] { round, legacy }.SelectMany(kind => PlayerSettings.GetPlatformIcons(target, kind)).Any(i => i.GetTextures().Any(t => t != null)))
            throw new InvalidOperationException("Deprecated Android icon textures still configured.");
        Debug.Log("[Adaptive Icons] Native adaptive slots populated; legacy and round texture overrides cleared.");
    }
}
