using Twinny.Shaders;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor.Shaders
{
    [CustomEditor(typeof(AlphaClipper))]
    [CanEditMultipleObjects]
    public class AlphaClipperEditor : UnityEditor.Editor
    {
        private const string UxmlPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uxml";
        private const string UssPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uss";
        private const string IconsPath = "Packages/com.twinny.twe26/Editor/Icons/icons.png";
        private const string AlphaClipperIconName = "icons_14";
        private const string TitleFontPath = "Packages/com.twinny.twe26/Editor/SetupGuide/Resources/Fonts/DINNextLTPro-Condensed.otf";

        private VisualTreeAsset _visualTree;
        private StyleSheet _styleSheet;

        public override VisualElement CreateInspectorGUI()
        {
            LoadAssets();
            var root = _visualTree.CloneTree();
            root.styleSheets.Add(_styleSheet);

            ApplyTitle(root);
            ApplyHeroIcon(root);
            ApplyTitleFont(root);

            VisualElement contentRoot = GetContentRoot(root);
            AddSettingsFields(CreateSection(contentRoot, "Settings"));
            AddRuntimeInfo(CreateSection(contentRoot, "Runtime Preview"));
            AddCutoffGroupsCard(CreateSection(contentRoot, "Cutoff Groups"));

            return root;
        }

        private void LoadAssets()
        {
            if (_visualTree == null)
                _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);

            if (_styleSheet == null)
                _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
        }

        private static VisualElement GetContentRoot(VisualElement root)
        {
            return root.Q<VisualElement>(className: "root") ?? root;
        }

        private static VisualElement CreateSection(VisualElement root, string title)
        {
            var section = new VisualElement();
            section.AddToClassList("section");

            var label = new Label(title);
            label.AddToClassList("section-title");
            section.Add(label);

            var fields = new VisualElement();
            fields.AddToClassList("fields");
            section.Add(fields);

            root.Add(section);
            return fields;
        }

        private void ApplyTitle(VisualElement root)
        {
            if (root == null) return;

            var title = root.Q<Label>(className: "hero-title");
            if (title != null) title.text = "Alpha Clipper";

            var subtitle = root.Q<Label>(className: "hero-subtitle");
            if (subtitle != null) subtitle.text = "Global cutoff range and transition control";
        }

        private void AddSettingsFields(VisualElement container)
        {
            if (container == null) return;

            AddProperty(container, serializedObject.FindProperty("_minMaxWallHeight"), serializedObject);
            AddProperty(container, serializedObject.FindProperty("_transitionDuration"), serializedObject);
        }

        private void AddRuntimeInfo(VisualElement container)
        {
            if (container == null) return;
            if (targets != null && targets.Length != 1) return;

            var alphaClipper = target as AlphaClipper;
            if (alphaClipper == null) return;

            Vector2 minMaxWallHeight = AlphaClipper.MinMaxWallHeight;
            float cutoffHeight = Shader.GetGlobalFloat("_CutoffHeight");
            int cutoffGroupCount = Object.FindObjectsByType<CutoffGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            AddHelpLabel(container, $"Scene Cutoff Groups: {cutoffGroupCount}");
            AddHelpLabel(container, $"Global Cutoff Height: {cutoffHeight:0.##}");
            AddHelpLabel(container, $"Min Height: {minMaxWallHeight.x:0.##}");
            AddHelpLabel(container, $"Max Height: {minMaxWallHeight.y:0.##}");
            AddHelpLabel(container, $"Target Object: {alphaClipper.gameObject.name}");
        }

        private void AddCutoffGroupsCard(VisualElement container)
        {
            if (container == null) return;

            CutoffGroup[] cutoffGroups = Object.FindObjectsByType<CutoffGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (cutoffGroups == null || cutoffGroups.Length == 0)
            {
                AddHelpLabel(container, "No CutoffGroup found in the loaded scenes.");
                return;
            }

            for (int i = 0; i < cutoffGroups.Length; i++)
            {
                CutoffGroup cutoffGroup = cutoffGroups[i];
                if (!cutoffGroup)
                    continue;

                container.Add(CreateCutoffGroupCard(cutoffGroup));
            }
        }

        private VisualElement CreateCutoffGroupCard(CutoffGroup cutoffGroup)
        {
            var card = new Button(() => HighlightCutoffGroup(cutoffGroup))
            {
                text = string.Empty
            };

            card.style.unityTextAlign = TextAnchor.MiddleLeft;
            card.style.flexDirection = FlexDirection.Column;
            card.style.alignItems = Align.FlexStart;
            card.style.marginBottom = 6f;
            card.style.paddingLeft = 8f;
            card.style.paddingRight = 8f;
            card.style.paddingTop = 6f;
            card.style.paddingBottom = 6f;

            var title = new Label(cutoffGroup.name);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.color = new Color(0.86f, 0.9f, 0.92f, 1f);

            var path = new Label(GetHierarchyPath(cutoffGroup.transform));
            path.AddToClassList("inline-note");

            var info = new Label($"Y: {cutoffGroup.transform.position.y:0.##}  Offset: {cutoffGroup.offsetHeight:0.##}");
            info.AddToClassList("inline-note");

            card.Add(title);
            card.Add(path);
            card.Add(info);

            return card;
        }

        private static void HighlightCutoffGroup(CutoffGroup cutoffGroup)
        {
            if (!cutoffGroup)
                return;

            Selection.activeGameObject = cutoffGroup.gameObject;
            EditorGUIUtility.PingObject(cutoffGroup.gameObject);
        }

        private static string GetHierarchyPath(Transform current)
        {
            if (current == null)
                return string.Empty;

            string path = current.name;
            while (current.parent != null)
            {
                current = current.parent;
                path = $"{current.name}/{path}";
            }

            return path;
        }

        private void AddProperty(VisualElement container, SerializedProperty property, SerializedObject owner)
        {
            if (container == null || property == null) return;
            var field = new PropertyField(property);
            field.Bind(owner);
            container.Add(field);
        }

        private void AddHelpLabel(VisualElement container, string text)
        {
            var label = new Label(text);
            label.AddToClassList("inline-note");
            container.Add(label);
        }

        private void ApplyHeroIcon(VisualElement root)
        {
            if (root == null) return;
            var icon = root.Q<VisualElement>("heroIcon");
            if (icon == null) return;

            Sprite sprite = LoadSprite(IconsPath, AlphaClipperIconName);
            if (sprite == null) return;

            icon.style.backgroundImage = new StyleBackground(sprite);
            icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            icon.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            icon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            icon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        private void ApplyTitleFont(VisualElement root)
        {
            if (root == null) return;
            var title = root.Q<Label>(className: "hero-title");
            if (title == null) return;

            var font = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            if (font == null) return;

            title.style.unityFontDefinition = FontDefinition.FromFont(font);
        }

        private Sprite LoadSprite(string path, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0) return null;

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                    return sprite;
            }

            return null;
        }
    }
}
