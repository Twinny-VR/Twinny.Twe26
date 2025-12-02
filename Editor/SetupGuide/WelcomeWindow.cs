#if UNITY_EDITOR
using Concept.SmartTools;
using Twinny.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;


namespace Twinny.Editor
{


[InitializeOnLoad]
public static class WelcomeWindowRegister
{
    private const string PACKAGE_NAME = "welcome";
    static  WelcomeWindowRegister()
    {
        SetupGuideWindow.RegisterModule(PACKAGE_NAME, typeof(WelcomeWindow));
    }
}


[UxmlElement]
public partial class WelcomeWindow : VisualElement, IModuleSetup
{
    private TextField m_productField;
    private TextField m_companyField;
    private TextField m_versionField;
    private Label m_packageNameLabel;
    private Label m_versionLabel;
    private ScrollView m_smartBuildSettings;

    public WelcomeWindow()
    {
        var visualTree = Resources.Load<VisualTreeAsset>("SetupWelcome");
        if (visualTree == null)
        {
            Debug.LogError("SetupWelcome não encontrado em Resources!");
            return;
        }

        visualTree.CloneTree(this);

        m_productField = this.Q<TextField>("ProductNameField");
        m_companyField = this.Q<TextField>("CompanyNameField");
        m_versionField = this.Q<TextField>("VersionField");
        m_packageNameLabel = this.Q<Label>("PackageNameLabel");
        m_versionLabel = this.Q<Label>("VersionLabel");
        //SmartBuild
        m_smartBuildSettings = this.Q<ScrollView>("SmartBuildSettings");
    }

    public void OnShowSection(SetupGuideWindow guideWindow, int tabIndex) {

            var pkgInfo = SmartTools.GetPackageInfo(typeof(TwinnyManager));
        m_productField.value = PlayerSettings.productName;
            m_packageNameLabel.text = pkgInfo.displayName.ToUpper();
        m_versionLabel.text = "Versão " + guideWindow.packageVersion;
        m_versionField.value = PlayerSettings.bundleVersion;
        m_companyField.RegisterValueChangedCallback(evt => { PlayerSettings.companyName = evt.newValue; });
        m_productField.RegisterValueChangedCallback(evt => { PlayerSettings.productName = evt.newValue; });
        m_versionField.RegisterValueChangedCallback(evt => { PlayerSettings.bundleVersion = evt.newValue; });
            if (PlayerSettings.companyName == "DefaultCompany")
                PlayerSettings.companyName = pkgInfo?.author.name;

        var company = Application.companyName;
        m_companyField.value = company;




    }

    public void OnApply() { }
}
#endif

}