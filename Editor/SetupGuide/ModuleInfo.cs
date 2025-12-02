using System;
using Twinny.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    /*
    [Serializable, UxmlElement("ModuleInfo")]
    public partial class ModuleInfo : VisualElement
    {
        [UxmlAttribute("section-module")] public GameModule sectionModule { get; set; }
        [UxmlAttribute("section-name")] public string sectionName { get; set; }
        [UxmlAttribute("section-title")] public string sectionTitle { get; set; }

        [UxmlAttribute("section-icon")] public Sprite sectionIcon { get; set; }

        public VisualElement sectionButton;
        public VisualTreeAsset sectionLayout;

        //  public Section() { }

    }
    */
    [Serializable]
    public class ModuleInfo
    {
        public ProjectModule moduleType;
        public string moduleName;
        public string moduleDisplayName;
        public Sprite moduleIcon;
        public string moduleInstallPath;
        public PackageInfoData[] dependencies;
        [HideInInspector]
        public VisualTreeAsset layout; //This property is setted by each module

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
