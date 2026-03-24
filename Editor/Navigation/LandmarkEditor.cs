using Twinny.Navigation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor.Navigation
{
    [CustomEditor(typeof(Landmark), true)]
    [CanEditMultipleObjects]
    public class LandmarkEditor : UnityEditor.Editor
    {
        private const string UxmlPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uxml";
        private const string UssPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uss";
        private const string IconsPath = "Packages/com.twinny.twe26/Editor/Icons/icons.png";
        private const string LandmarkIconName = "icons_12";
        private const string TitleFontPath = "Packages/com.twinny.twe26/Editor/SetupGuide/Resources/Fonts/DINNextLTPro-Condensed.otf";

        private VisualTreeAsset _visualTree;
        private StyleSheet _styleSheet;

        public override VisualElement CreateInspectorGUI()
        {
            LoadAssets();

            var root = _visualTree.CloneTree();
            root.styleSheets.Add(_styleSheet);

            ApplyHeader(root);
            ApplyTitleFont(root);

            VisualElement contentRoot = GetContentRoot(root);
            BuildSections(contentRoot);

            return root;
        }

        protected virtual string InspectorTitle => "Landmark";

        protected virtual string InspectorSubtitle => "Scene navigation point and skybox context";

        protected virtual string HeroIconPath => IconsPath;

        protected virtual string HeroIconName => LandmarkIconName;

        protected virtual void BuildSections(VisualElement contentRoot)
        {
            AddIdentitySection(CreateSection(contentRoot, "Identity"));
            AddSettingsSection(CreateSection(contentRoot, "Settings"));
            AddAdditionalSections(contentRoot);
            AddRuntimeSection(CreateSection(contentRoot, "Runtime"));
        }

        protected virtual void AddAdditionalSections(VisualElement contentRoot)
        {
        }

        private void LoadAssets()
        {
            if (_visualTree == null)
            {
                _visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            }

            if (_styleSheet == null)
            {
                _styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            }
        }

        protected static VisualElement GetContentRoot(VisualElement root)
        {
            return root.Q<VisualElement>(className: "root") ?? root;
        }

        protected static VisualElement CreateSection(VisualElement root, string title)
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

        private void ApplyHeader(VisualElement root)
        {
            ApplyTitle(root);
            ApplyHeroIcon(root);
        }

        protected virtual void ApplyTitle(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            Label title = root.Q<Label>(className: "hero-title");
            if (title != null)
            {
                title.text = InspectorTitle;
            }

            Label subtitle = root.Q<Label>(className: "hero-subtitle");
            if (subtitle != null)
            {
                subtitle.text = InspectorSubtitle;
            }
        }

        protected virtual void ApplyHeroIcon(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            VisualElement icon = root.Q<VisualElement>("heroIcon");
            if (icon == null)
            {
                return;
            }

            Sprite sprite = LoadSprite(HeroIconPath, HeroIconName);
            if (sprite == null)
            {
                return;
            }

            icon.style.backgroundImage = new StyleBackground(sprite);
            icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            icon.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
            icon.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
            icon.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
        }

        protected virtual void ApplyTitleFont(VisualElement root)
        {
            if (root == null)
            {
                return;
            }

            Label title = root.Q<Label>(className: "hero-title");
            if (title == null)
            {
                return;
            }

            Font font = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            if (font == null)
            {
                return;
            }

            title.style.unityFontDefinition = FontDefinition.FromFont(font);
        }

        protected virtual void AddIdentitySection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            SerializedProperty guidProperty = serializedObject.FindProperty("landmarkGuid");

            var guidRow = new VisualElement();
            guidRow.style.flexDirection = FlexDirection.Row;
            guidRow.style.justifyContent = Justify.FlexEnd;
            guidRow.style.alignItems = Align.Center;
            container.Add(guidRow);

            var guidLabel = new Label(guidProperty?.stringValue ?? string.Empty);
            guidLabel.tooltip = "Generated automatically and used by LandmarkHub.";
            guidLabel.style.color = new StyleColor(new Color(0.62f, 0.62f, 0.62f));
            guidLabel.style.fontSize = 10f;
            guidLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            guidLabel.style.flexShrink = 1f;
            guidLabel.style.whiteSpace = WhiteSpace.Normal;
            guidLabel.style.marginRight = 8f;
            guidRow.Add(guidLabel);

            var copyButton = new Button(() =>
            {
                if (guidProperty == null)
                {
                    return;
                }

                EditorGUIUtility.systemCopyBuffer = guidProperty.stringValue;
            })
            {
                text = "Copy GUID"
            };

            copyButton.SetEnabled(!serializedObject.isEditingMultipleObjects && guidProperty != null && !string.IsNullOrWhiteSpace(guidProperty.stringValue));
            guidRow.Add(copyButton);

            AddHelpLabel(container, "The GUID is generated automatically and should be used as the lookup key in LandmarkHub.");

            if (serializedObject.isEditingMultipleObjects)
            {
                AddHelpLabel(container, "Copy is disabled while editing multiple landmarks.");
            }
        }

        protected virtual void AddSettingsSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddProperty(container, "landName");
            AddProperty(container, "skyBoxMaterial");
            AddProperty(container, "hdriOffsetRotation");
        }

        protected virtual void AddRuntimeSection(VisualElement container)
        {
            if (container == null || serializedObject.isEditingMultipleObjects)
            {
                return;
            }

            Landmark landmark = target as Landmark;
            if (landmark == null)
            {
                return;
            }

            AddHelpLabel(container, $"GameObject: {landmark.gameObject.name}");
            AddHelpLabel(container, $"Position: {landmark.position.x:0.##}, {landmark.position.y:0.##}, {landmark.position.z:0.##}");
            AddHelpLabel(container, $"Rotation: {landmark.rotation.eulerAngles.x:0.#}, {landmark.rotation.eulerAngles.y:0.#}, {landmark.rotation.eulerAngles.z:0.#}");
        }

        protected void AddProperty(VisualElement container, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (container == null || property == null)
            {
                return;
            }

            var field = new PropertyField(property);
            field.Bind(serializedObject);
            container.Add(field);
        }

        protected void AddHelpLabel(VisualElement container, string text)
        {
            var box = new VisualElement();
            box.AddToClassList("inline-note-box");

            var label = new Label(text);
            label.AddToClassList("inline-note");
            box.Add(label);

            container.Add(box);
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                return null;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite sprite && sprite.name == spriteName)
                {
                    return sprite;
                }
            }

            return null;
        }
    }
}
