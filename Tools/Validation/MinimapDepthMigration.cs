using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

// Temporary native migration: URP RenderGraph camera targets require depth.
public static class MinimapDepthMigration
{
    public static void Run()
    {
        const string path = "Assets/Art/RenderTexture/MiniMapRenderTexture.renderTexture";
        var texture = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (texture == null) throw new InvalidOperationException("Authored minimap texture is missing.");
        texture.depthStencilFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
        if (texture.depthStencilFormat == GraphicsFormat.None)
            throw new InvalidOperationException("Native graphics device did not supply a depth format.");
        EditorUtility.SetDirty(texture);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var restored = AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if (restored.depthStencilFormat == GraphicsFormat.None)
            throw new InvalidOperationException("Native minimap depth did not survive import.");
        Debug.Log("[Minimap Depth] Native saved/imported depth format: " + restored.depthStencilFormat);
    }
}
