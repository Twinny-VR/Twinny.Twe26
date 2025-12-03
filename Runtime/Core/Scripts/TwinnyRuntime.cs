using Concept.SmartTools;
using Concept.UI;
using System;
using System.Dynamic;
using UnityEditor;
using UnityEngine;

namespace Twinny.Core
{

    [Serializable]
    public class TwinnyRuntime : ScriptableObject
    {
        private static TwinnyRuntime m_instance;
        public BuildType ambientType;
        public float fadeTime = 1f;
        public bool forceFrameRate;
        [ShowIf("forceFrameRate")]
        [Range(60,120)]
        public int targetFrameRate = 90;


        [Header("WEB HOOK")]
        [Tooltip("This system is only for realeses build version.")]
        public string webHookUrl;


        private static TwinnyRuntime LoadRuntimeProfile<T>() where T : TwinnyRuntime => LoadRuntimeProfile<T>(typeof(T).Name + "Preset");
        private static TwinnyRuntime LoadRuntimeProfile<T>(string fileName) where T : TwinnyRuntime
        {
            var config = Resources.Load<T>(fileName);
           
            if (config == null)
            {
                Debug.LogWarning($"[TwinnyManager] Impossible to load '{fileName}'.");
                return null;
            }
            Debug.Log($"[TwinnyManager] RuntimeProfile '{fileName}' loaded successfully.");
            return config;
        }

        public static TwinnyRuntime GetInstance() => GetInstance<TwinnyRuntime>();
        public static TwinnyRuntime GetInstance(bool forceCreate) => GetInstance<TwinnyRuntime>(forceCreate);
        public static T GetInstance<T>(bool forceCreate = false) where T : TwinnyRuntime
        {
            if( m_instance == null ) m_instance = LoadRuntimeProfile<T>();
            if (m_instance == null && forceCreate) m_instance = CreateAsset<T>();
            return m_instance as T;
        }
        public static T CreateAsset<T>() where T : TwinnyRuntime => CreateAsset<T>(typeof(T).Name + "Preset");
        public static T CreateAsset<T>(string assetName) where T : TwinnyRuntime

        {
#if UNITY_EDITOR
            string folderPath = "Assets/Resources";
            string assetPath = $"{folderPath}/{assetName}.asset";

            // Garante que a pasta existe
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            // Cria instância do ScriptableObject
            T asset = CreateInstance<T>();

            // Salva como arquivo .asset
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TwinnyRuntime] Asset created: {assetPath}");

            return asset;
#else
        Debug.LogError("[TwinnyRuntime] CreateInstance only works in Editor ambient!");
        return null;
#endif
        }

    }
}
