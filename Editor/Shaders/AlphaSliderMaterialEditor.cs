#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twinny.Editor.Shaders
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Material))]
    public class AlphaSliderMaterialEditor : MaterialEditor
    {
        private const string StepPropertyName = "_Step";
        private const string CutoffHeightPropertyName = "_CutoffHeight";
        private const string ScopePropertyName = "_SCOPE";
        private const float GlobalScopeValue = 1f;
        private const float EditorResetCutoffHeight = 1000f;

        [InitializeOnLoadMethod]
        private static void RegisterPlayModeCallbacks()
        {
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        }

        public override void OnInspectorGUI()
        {
            Material[] selectedMaterials = targets.OfType<Material>().ToArray();
            if (selectedMaterials.Length == 0)
            {
                base.OnInspectorGUI();
                return;
            }

            bool allSupportStep = selectedMaterials.All(SupportsStepProperty);
            bool hasMixedShaders = selectedMaterials
                .Select(material => material.shader)
                .Distinct()
                .Count() > 1;

            if (allSupportStep && hasMixedShaders)
            {
                DrawMixedStepInspector(selectedMaterials);
                return;
            }

            base.OnInspectorGUI();

            if (allSupportStep)
                SyncGlobalCutoffHeight(selectedMaterials);
        }

        private static void DrawMixedStepInspector(Material[] materials)
        {
            float firstValue = materials[0].GetFloat(StepPropertyName);
            bool hasMixedValues = materials.Any(material => !Mathf.Approximately(material.GetFloat(StepPropertyName), firstValue));
            bool allGlobalScope = materials.All(IsGlobalScope);

            EditorGUILayout.HelpBox(
                allGlobalScope
                    ? "Materiais com shaders diferentes, mas com a propriedade compartilhada _Step. O inspector abaixo permite editar esse valor em conjunto e sincronizar o global _CutoffHeight."
                    : "Materiais com shaders diferentes, mas pelo menos um deles nao esta em Scope Global. O inspector permite editar _Step, mas nao sincroniza o global _CutoffHeight.",
                MessageType.Info);

            EditorGUI.showMixedValue = hasMixedValues;
            EditorGUI.BeginChangeCheck();
            float newValue = EditorGUILayout.FloatField(StepPropertyName, firstValue);
            EditorGUI.showMixedValue = false;

            if (EditorGUI.EndChangeCheck())
            {
                foreach (Material material in materials)
                {
                    Undo.RecordObject(material, "Update _Step");
                    material.SetFloat(StepPropertyName, newValue);
                    EditorUtility.SetDirty(material);
                }

                if (allGlobalScope)
                    Shader.SetGlobalFloat(CutoffHeightPropertyName, newValue);
            }
            else if (!hasMixedValues && allGlobalScope)
            {
                Shader.SetGlobalFloat(CutoffHeightPropertyName, firstValue);
            }
        }

        private static bool SupportsStepProperty(Material material)
        {
            return material != null && material.shader != null && material.HasProperty(StepPropertyName);
        }

        private static void SyncGlobalCutoffHeight(Material[] materials)
        {
            if (materials.Length == 0)
                return;

            if (!materials.All(IsGlobalScope))
                return;

            Shader.SetGlobalFloat(CutoffHeightPropertyName, materials[0].GetFloat(StepPropertyName));
        }

        private static bool IsGlobalScope(Material material)
        {
            return material != null &&
                   material.HasProperty(ScopePropertyName) &&
                   Mathf.Approximately(material.GetFloat(ScopePropertyName), GlobalScopeValue);
        }

        private static void HandlePlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
                return;

            Shader.SetGlobalFloat(CutoffHeightPropertyName, EditorResetCutoffHeight);
        }
    }
}
#endif