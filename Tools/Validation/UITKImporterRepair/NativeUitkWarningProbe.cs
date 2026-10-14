using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class NativeUitkWarningProbe
{
 public static void Run()
 {
  if(!Application.dataPath.Contains("/.local/maze-native-probe/"))throw new InvalidOperationException("Owned clone required");
  var stage=Environment.GetEnvironmentVariable("BLAST_UITK_WARNING_STAGE")??"Unknown";Debug.Log("[UITK Warning Probe] Begin "+stage);
  var paths=File.ReadAllLines("uitk-warning-paths.txt");
  foreach(var path in paths){AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);if(AssetDatabase.LoadMainAssetAtPath(path)==null)throw new InvalidOperationException("Actual native imported asset missing: "+path);}
  Debug.Log("[UITK Warning Probe] PASS "+stage+" actual imported assets="+paths.Length);
 }
}
