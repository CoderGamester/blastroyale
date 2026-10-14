using System;
using System.IO;
using UnityEngine;

namespace FirstLight.Editor.Build
{
    public static class PackageRestore
    {
        // Unity resolves/imports UPM dependencies before executeMethod. CI then closes
        // this process and applies the exact reviewed Editor scanner patch on the host.
        public static void Resolve()
        {
            var cache = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PackageCache"));
            if (Directory.GetDirectories(cache, "com.gamelovers.mobileservices@*").Length != 1)
                throw new InvalidOperationException("The pinned MobileServices package was not resolved.");
            Debug.Log("[Package Restore] Pinned UPM dependencies resolved; ready for closed-Editor patch.");
        }
    }
}
