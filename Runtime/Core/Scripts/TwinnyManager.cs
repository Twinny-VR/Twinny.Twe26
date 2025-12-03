using System.Globalization;
using System.IO;
using Concept.Core;
using Concept.Helpers;

#if UNITY_LOCALIZATION
using Concept.Localization;
#endif
using Twinny.Helpers;
using Twinny.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
#if TWINNY_OPENXR
using UnityEngine.XR.Management;
#endif
namespace Twinny.Core
{
    public enum Platform //TODO Mudar pra BuildTarget
    {
        UNKNOW,
        WINDOWS,
        XR,
        MOBILE,
        WEBGL
    }
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class TwinnyManager
    {
        public const string PACKAGE_NAME = "com.twinny.twe26";
        const string DEFAULT_KEYSTORE = "TwinnyKey.keystore";
        public const string SAMPLE_ROOT = "Assets/Samples/Twinny Engine";
        public const string DEFAULT_WEBHOOK = "https://discordapp.com/api/webhooks/1430265789373612063/CMk7R0RtVqtDKxJYbeszAEMFmu1bZkfjWVjk2rkpDYDbfYdyybXo_-NTRgTtakLFCra5";

        public static Platform currentPlatform = Platform.UNKNOW;

        private static TwinnyRuntime m_config => TwinnyRuntime.GetInstance<TwinnyRuntime>();

        //public delegate void onPlatformInitilize(Platform platform);
        //public static onPlatformInitilize OnPlatformInitialize;
       

        static TwinnyManager()
        {

#if UNITY_EDITOR && UNITY_ANDROID
            string defaultKeyStore = IOExtensions.GetPackageAbsolutePath(PACKAGE_NAME);
            string currentKeyStore = PlayerSettings.Android.keystoreName;
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, currentKeyStore);

            bool overwrite = (string.IsNullOrEmpty(currentKeyStore) || !File.Exists(path));

            if(overwrite)
            {

                string newKey = Path.Combine(defaultKeyStore, "Samples~", DEFAULT_KEYSTORE); ;
                Debug.LogWarning($"[TwinnyManager] No valid keystore defined. Using default: '{DEFAULT_KEYSTORE}'.");
                PlayerSettings.Android.keystoreName = newKey;
                EditorUtility.SetDirty(UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
                AssetDatabase.SaveAssets();
            }
#endif
            GetCurrentPlatform();
            var locale = CultureInfo.CurrentCulture.ToString();
            //LocalizationProvider.SetLocale(locale);
#if UNITY_LOCALIZATION
            LocalizationProvider.Initialize();
#endif
        }

        public static void GetCurrentPlatform()
        {

#if UNITY_EDITOR

            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android)
            {

#if TWINNY_OPENXR
                currentPlatform = Platform.XR;
                Debug.LogWarning("[TwinnyManager] XR Platform initialized.");
#else
                currentPlatform = Platform.MOBILE;
                Debug.LogWarning("[TwinnyManager] Android Platform initialized.");
#endif
            }
            else
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.iOS)
            {
                currentPlatform = Platform.MOBILE;
                Debug.LogWarning("[TwinnyManager] iOS Platform initialized.");
            }
            else
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows || EditorUserBuildSettings.activeBuildTarget == BuildTarget.StandaloneWindows64)
            {
                currentPlatform = Platform.WINDOWS;
                Debug.LogWarning("[TwinnyManager] Windows Platform initialized.");
            }
            else
                if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.WebGL)
            {
                currentPlatform = Platform.WEBGL;
                Debug.LogWarning("[TwinnyManager] WebGL Platform initialized.");
            }
            else
            {
                currentPlatform = Platform.UNKNOW;
                Debug.LogError($"[TwinnyManager] Unknow Platform initialized ({Application.platform}).");
            }


#else
                if (Application.platform == RuntimePlatform.Android)
            {
#if TWINNY_OPENXR
                currentPlatform = Platform.XR;
                Debug.LogWarning("[TwinnyManager] XR Platform initialized.");
#else
                    currentPlatform = Platform.MOBILE;
                    Debug.LogWarning("[TwinnyManager] Android Platform initialized.");
#endif



            }
            else
            if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                currentPlatform = Platform.MOBILE;
                Debug.LogWarning("[TwinnyManager] iOS Platform initialized.");
            }
            else
            if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
            {
                Debug.LogWarning("[TwinnyManager] nacOS Platform initialized.");
            }
            else
            if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
            {
                currentPlatform = Platform.WINDOWS;
                Debug.LogWarning("[TwinnyManager] Windows Platform initialized.");
            }
            else
            if (Application.platform == RuntimePlatform.WebGLPlayer)
            {
                currentPlatform = Platform.WEBGL;
                Debug.LogWarning("[TwinnyManager] WebGL Platform initialized.");
            }
            else
            {
                currentPlatform = Platform.UNKNOW;
                Debug.LogError($"[TwinnyManager] Unknow Platform initialized ({Application.platform}).");
            }
#endif
           
        }


        //[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            //TODO Aprimorar sistema de webhook com Dispatcher
            /* 
            if(m_config == null)
            _ = DiscordUtils.SendEmbedAsync(DEFAULT_WEBHOOK, "Runtime Preset", "The project doesn't have any preset defined."); // Green color
            else
            if (!m_config.isTestBuild && m_config.webHookUrl != string.Empty)
                    {
                        GameObject go = new GameObject();
                        go.name = "[WebHook]";
                        DiscordExceptionHandler discordHandler = go.AddComponent<DiscordExceptionHandler>();
                        discordHandler.webhookUrl = m_config.webHookUrl;
                discordHandler.sendWarnings = m_config.sendWarnings;       
            }
            */
            CallbackHub.CallAction<IUICallBacks>(callback => callback.OnPlatformInitialize());

            bool isTestBuild = m_config.ambientType != Concept.SmartTools.BuildType.RELEASE;
            if (isTestBuild)
            {
                Debug.LogWarning("*** TEST VERSION BUILD! Unset in Config file before Release!");
                if (DebugPanel.Instance != null)
                {
                    DebugPanel.Instance.visible = isTestBuild;
                    DebugPanel.Debug("===============================\n" +
                                     "=          <color=#3cbcd6>TEST VERSION BUILD!</color>          =\n" +
                                     "= <color=#3cbcd6>Unset in Config file before Release.</color> =\n" +
                                     "===============================", " ", LogType.Warning);
                }
            }

            if (m_config != null && m_config.forceFrameRate)
            {
                Application.targetFrameRate = m_config.targetFrameRate;
                Debug.LogWarning($"[TwinnyManager] Application frame rate locked at {m_config.targetFrameRate}FPS.");
            }

        }
    }
}
