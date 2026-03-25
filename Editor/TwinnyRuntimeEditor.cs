#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Twinny.Core.Editor
{
    [CustomEditor(typeof(TwinnyRuntime), true)]
    public class TwinnyRuntimeEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            bool changed = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (!changed)
            {
                return;
            }

            if (target is TwinnyRuntime runtime)
            {
                TwinnyBuildTypeSync.SyncFromRuntime(runtime);
                EditorUtility.SetDirty(runtime);
            }
        }
    }
}
#endif
