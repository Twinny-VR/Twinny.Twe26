#if UNITY_EDITOR
using Concept.SmartTools;
using Concept.SmartTools.Editor;
using Concept.UI;
using Twinny.Core;
using Twinny.Core.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    [InitializeOnLoad]
    public static class WelcomeWindowRegister
    {
        private const string PACKAGE_NAME = "welcome";

        static WelcomeWindowRegister()
        {
            SetupGuideWindow.RegisterModule(PACKAGE_NAME, typeof(WelcomeWindow));
            SmartBuilderWindow.OpenSmartWindow = tabIndex => SetupGuideWindow.OpenSection(PACKAGE_NAME, tabIndex);
        }
    }

    [UxmlElement]
    public partial class WelcomeWindow : VisualElement, IModuleSetup
    {
        private const string PackageRootPath = "Packages/com.twinny.twe26";

        private TextField m_productField;
        private TextField m_companyField;
        private TextField m_versionField;
        private Label m_packageNameLabel;
        private Label m_versionLabel;
        private Button m_updatePackageButton;
        private VisualElement m_smartBuildSettings;
        private SmartBuilderView m_smartBuilderView;

        public WelcomeWindow()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("SetupWelcome");
            if (visualTree == null)
            {
                Debug.LogError("SetupWelcome nao encontrado em Resources!");
                return;
            }

            visualTree.CloneTree(this);

            m_productField = this.Q<TextField>("ProductNameField");
            m_companyField = this.Q<TextField>("CompanyNameField");
            m_versionField = this.Q<TextField>("VersionField");
            m_packageNameLabel = this.Q<Label>("PackageNameLabel");
            m_versionLabel = this.Q<Label>("VersionLabel");
            m_updatePackageButton = this.Q<Button>("UpdatePackageButton");
            m_smartBuildSettings = this.Q<VisualElement>("SmartBuilderHost");
            m_smartBuilderView = this.Q<SmartBuilderView>("EmbeddedSmartBuilderView");

            if (m_updatePackageButton != null)
            {
                m_updatePackageButton.clicked += () => PackageUpdateUtility.RequestUpdate(PackageRootPath);
            }
        }

        public void OnShowSection(SetupGuideWindow guideWindow, int tabIndex)
        {
            var pkgInfo = SmartTools.GetPackageInfo(typeof(TwinnyManager));
            m_productField.value = PlayerSettings.productName;
            m_packageNameLabel.text = pkgInfo.displayName.ToUpper();
            m_versionLabel.text = "Versao " + guideWindow.packageVersion;
            if (m_updatePackageButton != null)
            {
                m_updatePackageButton.style.display = PackageUpdateUtility.CanShowUpdateButton(PackageRootPath)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }

            m_versionField.value = PlayerSettings.bundleVersion;
            m_companyField.RegisterValueChangedCallback(evt => { PlayerSettings.companyName = evt.newValue; });
            m_productField.RegisterValueChangedCallback(evt => { PlayerSettings.productName = evt.newValue; });
            m_versionField.RegisterValueChangedCallback(evt => { PlayerSettings.bundleVersion = evt.newValue; });
            if (PlayerSettings.companyName == "DefaultCompany")
            {
                PlayerSettings.companyName = pkgInfo?.author.name;
            }

            var company = Application.companyName;
            m_companyField.value = company;
            m_smartBuilderView?.SelectTab(tabIndex);
        }

        public void OnApply()
        {
        }
    }
}
#endif


