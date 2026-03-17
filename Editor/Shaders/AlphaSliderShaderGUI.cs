#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Twinny.Editor.Shaders
{
    public class AlphaSliderShaderGUI : ShaderGUI
    {
        private const string CutoffHeightPropertyName = "_CutoffHeight";
        private const string StepPropertyName = "_Step";
        private const string ScopePropertyName = "_SCOPE";
        private const float GlobalScopeValue = 1f;

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            base.OnGUI(materialEditor, properties);

            Material targetMaterial = materialEditor.target as Material;
            if (targetMaterial == null)
                return;

            MaterialProperty stepProperty = FindProperty(StepPropertyName, properties, false);
            if (stepProperty == null)
                return;

            float stepValue = stepProperty.floatValue;
            bool isGlobalScope = IsGlobalScope(targetMaterial);

            if (isGlobalScope)
                Shader.SetGlobalFloat(CutoffHeightPropertyName, stepValue);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                isGlobalScope
                    ? "_CutoffHeight e global neste shader. Enquanto este material estiver em Scope Global, o inspector sincroniza o global usando o valor exposto de _Step."
                    : "Este material nao esta em Scope Global. O inspector mostra _Step, mas nao sincroniza o global _CutoffHeight.",
                isGlobalScope ? MessageType.Info : MessageType.None);
            EditorGUILayout.LabelField("Global Sync", isGlobalScope ? "Ativo" : "Inativo");

            if (isGlobalScope)
                EditorGUILayout.LabelField("Global _CutoffHeight", stepValue.ToString("F3"));
        }

        private static bool IsGlobalScope(Material material)
        {
            return material != null &&
                   material.HasProperty(ScopePropertyName) &&
                   Mathf.Approximately(material.GetFloat(ScopePropertyName), GlobalScopeValue);
        }
    }
}
#endif