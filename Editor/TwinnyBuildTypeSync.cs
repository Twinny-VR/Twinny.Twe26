#if UNITY_EDITOR
using Concept.SmartTools;
using Concept.SmartTools.Editor;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Twinny.Core.Editor
{
    [InitializeOnLoad]
    public static class TwinnyBuildTypeSync
    {
        private const string SmartBuilderBuildTypePath = "m_uploadSettings.buildType";
        private const string RuntimeBuildTypePath = "buildType";
        private static bool s_isSynchronizing;

        static TwinnyBuildTypeSync()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (s_isSynchronizing || modifications == null || modifications.Length == 0)
            {
                return modifications;
            }

            foreach (UndoPropertyModification modification in modifications)
            {
                PropertyModification currentModification = modification.currentValue;
                Object target = currentModification.target;
                if (target == null)
                {
                    continue;
                }

                if (target is SmartBuilderConfig && currentModification.propertyPath == SmartBuilderBuildTypePath)
                {
                    SyncFromSmartTools();
                    break;
                }

                if (target is TwinnyRuntime runtime && currentModification.propertyPath == RuntimeBuildTypePath)
                {
                    SyncFromRuntime(runtime);
                    break;
                }
            }

            return modifications;
        }

        public static void SyncFromRuntime(TwinnyRuntime runtime)
        {
            SmartUploaderSettings settings = SmartUploader.Settings;
            if (runtime == null || settings == null || settings.buildType == runtime.buildType)
            {
                return;
            }

            s_isSynchronizing = true;
            try
            {
                settings.buildType = runtime.buildType;
                EditorUtility.SetDirty(SmartBuilderConfig.instance);
                AssetDatabase.SaveAssets();
            }
            finally
            {
                s_isSynchronizing = false;
            }
        }

        public static void SyncFromSmartTools()
        {
            SmartUploaderSettings settings = SmartUploader.Settings;
            if (settings == null)
            {
                return;
            }

            s_isSynchronizing = true;
            try
            {
                bool changedAnyAsset = false;
                foreach (TwinnyRuntime runtime in LoadRuntimeAssets())
                {
                    changedAnyAsset |= SyncRuntimeAsset(runtime, settings.buildType);
                }

                if (changedAnyAsset)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                s_isSynchronizing = false;
            }
        }

        private static bool SyncRuntimeAsset(TwinnyRuntime runtime, BuildType buildType)
        {
            if (runtime == null || runtime.buildType == buildType)
            {
                return false;
            }

            runtime.buildType = buildType;
            EditorUtility.SetDirty(runtime);
            return true;
        }

        private static IEnumerable<TwinnyRuntime> LoadRuntimeAssets()
        {
            string[] assetGuids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets/Resources" });
            foreach (string guid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                TwinnyRuntime runtime = AssetDatabase.LoadAssetAtPath<TwinnyRuntime>(assetPath);
                if (runtime != null)
                {
                    yield return runtime;
                }
            }
        }
    }
}
#endif
