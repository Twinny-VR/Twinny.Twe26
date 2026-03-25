using System;
using UnityEngine;

namespace Twinny.Editor
{
    [Serializable]
    public class ModuleInfo
    {
        public int sortOrder;
        public string moduleName;
        public string moduleDisplayName;
        public Sprite moduleIcon;
        public string moduleInstallPath;
        public PackageInfoData[] dependencies;
        
        public ModuleInfo()
        {
            dependencies = Array.Empty<PackageInfoData>();
        }
    }

    [Serializable]
    public struct PackageInfoData
    {
        public string name;
        public string displayName;
        public string installPath;

        public PackageInfoData(string name, string displayName, string installPath)
        {
            this.name = name;
            this.displayName = displayName;
            this.installPath = installPath;
        }
    }
}
