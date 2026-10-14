using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using GameLovers.MobileServices.Editor.Settings;
using GameLovers.MobileServices.Device;
using UnityEditor;
using UnityEngine;
public static class NativeMobileScannerProbe
{
 public static void Run()
 {
  if(!Application.dataPath.Contains("/.local/maze-native-probe/"))throw new InvalidOperationException("Owned clone required");
  var stage=Environment.GetEnvironmentVariable("BLAST_MOBILE_SCANNER_STAGE")??"Unknown";
  var config=AssetDatabase.LoadAssetAtPath<MobileServicesConfig>("Assets/Editor/MobileServicesConfig.asset");if(config==null)throw new InvalidOperationException("Actual native config missing");
  var scanner=typeof(MobileServicesConfig).Assembly.GetType("GameLovers.MobileServices.Editor.Settings.MobileServicesScanner",true);
  var result=(ProjectScanResult)scanner.GetMethod("Scan",BindingFlags.Public|BindingFlags.Static).Invoke(null,null);
  var before=result.GetConfigurationWarnings(config).ToArray();
  if(stage=="Patched")
  {
   config.DeepLinks.AddIosUrlScheme("blastroyale");config.DeepLinks.AndroidIntentFilters.RemoveAll(entry=>entry.Scheme=="blastroyale"&&string.IsNullOrEmpty(entry.Host)&&string.IsNullOrEmpty(entry.PathPrefix));config.DeepLinks.AddAndroidIntentFilter("blastroyale","friend");EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
  }
  if(!config.TryValidate(out var validationError))throw new InvalidOperationException("Actual config validation failed: "+validationError);
  var after=result.GetConfigurationWarnings(config).ToArray();
  var targets=new[]{typeof(PermissionsService),typeof(IPermissionsService),typeof(AttService),typeof(IAttService)};
  var culprits=AppDomain.CurrentDomain.GetAssemblies().Where(a=>a.GetReferencedAssemblies().Any(r=>r.Name=="GameLovers.MobileServices")).SelectMany(a=>a.GetTypes()).SelectMany(type=>type.GetFields(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance).Where(field=>targets.Contains(field.FieldType)).Select(field=>new{assembly=type.Assembly.GetName().Name,type=type.FullName,member=field.Name,target=field.FieldType.FullName})).ToArray();
  var players=UnityEditor.Compilation.CompilationPipeline.GetAssemblies(UnityEditor.Compilation.AssembliesType.Player).Select(a=>a.name).ToArray();
  File.WriteAllText("mobile-scanner-"+stage.ToLowerInvariant()+"-native.json",JsonConvert.SerializeObject(new{stage,before,after,result.UsesAtt,result.UsesDeepLinks,permissions=result.ReferencedPermissions.Select(p=>p.ToString()).ToArray(),config.Capabilities.AppTracking,config.DeepLinks.IosUrlSchemes,android=config.DeepLinks.AndroidIntentFilters,culprits,players},Formatting.Indented));
  if(stage=="Original"&&(!result.UsesAtt||result.ReferencedPermissions.Count!=7))throw new InvalidOperationException("Original scanner did not reproduce its own Editor permission references");
  if(stage=="Patched"&&(after.Length!=0||result.UsesAtt||result.ReferencedPermissions.Count!=0))throw new InvalidOperationException("Correct player-only scan still infers unused capabilities");
  if(stage=="Positive"&&(result.UsesAtt||result.ReferencedPermissions.Count!=7||after.Length!=6))throw new InvalidOperationException("Genuine player permission reference was excluded");
  Debug.Log("[Mobile Scanner Probe] PASS "+stage+" warnings="+after.Length);
 }
}
