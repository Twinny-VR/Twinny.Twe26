#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ConceptFactory.UIEssentials.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Core.Editor
{
    [CustomEditor(typeof(TwinnyRuntime), true)]
    [CanEditMultipleObjects]
    public class TwinnyRuntimeEditor : UnityEditor.Editor
    {
        private const string UxmlPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uxml";
        private const string UssPath = "Packages/com.twinny.twe26/Editor/Shared/ComponentInspectorTemplate.uss";
        private const string IconsPath = "Packages/com.twinny.twe26/Editor/Icons/icons.png";
        private const string RuntimeIconName = "icons_2";
        private const string TitleFontPath = "Packages/com.twinny.twe26/Editor/SetupGuide/Resources/Fonts/DINNextLTPro-Condensed.otf";

        private VisualTreeAsset _visualTree;
        private StyleSheet _styleSheet;
        private VisualElement _fadeTimeField;
        private VisualElement _targetFrameRateField;

        protected virtual string InspectorTitle => "Twinny Runtime";

        protected virtual string InspectorSubtitle => "Build profile and bootstrap defaults";

        protected virtual string HeroIconPath => IconsPath;

        protected virtual string HeroIconName => RuntimeIconName;

        public override VisualElement CreateInspectorGUI()
        {
            LoadAssets();

            VisualElement root = _visualTree != null ? _visualTree.CloneTree() : new VisualElement();
            if (_styleSheet != null)
            {
                root.styleSheets.Add(_styleSheet);
            }

            root.AddToClassList("is-runtime");

            ApplyHeader(root);
            ApplyTitleFont(root);

            VisualElement contentRoot = GetContentRoot(root);
            BuildSections(contentRoot);

            root.TrackSerializedObjectValue(serializedObject, _ => OnRuntimeChanged());
            UpdateConditionalVisibility();

            return root;
        }

        protected virtual void BuildSections(VisualElement contentRoot)
        {
            AddProfileSection(CreateSection(contentRoot, "Profile"));
            AddDisplaySection(CreateSection(contentRoot, "Display"));
            AddScenesSection(CreateSection(contentRoot, "Scenes"));
            AddIntegrationSection(CreateSection(contentRoot, "Integration"));
            AddNotesSection(CreateSection(contentRoot, "Notes"));
        }

        protected virtual void AddProfileSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddProperty(container, "buildType");
            AddHelpLabel(container, "Build Type is mirrored to Smart Tools automatically.");
        }

        protected virtual void AddDisplaySection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddProperty(container, "useCanvasTransition");
            _fadeTimeField = AddProperty(container, "fadeTime");
            AddProperty(container, "forceFrameRate");
            _targetFrameRateField = AddProperty(container, "targetFrameRate");
        }

        protected virtual void AddScenesSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddSceneNameField(container);
            AddProperty(container, "defaultSkybox");
        }

        protected virtual void AddIntegrationSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddProperty(container, "webHookUrl");
            AddHelpLabel(container, "Webhook is intended for release build flows.");
        }

        protected virtual void AddNotesSection(VisualElement container)
        {
            if (container == null)
            {
                return;
            }

            AddHelpLabel(container, "Create the preset in Assets/Resources so TwinnyRuntime can load it automatically.");
            AddHelpLabel(container, "Scene and skybox defaults are used as runtime fallbacks when feature-specific presets are missing.");
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

        protected PropertyField AddProperty(VisualElement container, string propertyName)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (container == null || property == null)
            {
                return null;
            }

            var field = new PropertyField(property);
            field.Bind(serializedObject);
            container.Add(field);
            return field;
        }

        protected virtual void AddSceneNameField(VisualElement container)
        {
            SerializedProperty property = serializedObject.FindProperty("defaultSceneName");
            if (container == null || property == null)
            {
                return;
            }

            var row = new VisualElement();
            row.AddToClassList("row");

            var label = new Label("Default Scene Name");
            label.AddToClassList("row-label");
            row.Add(label);

            var field = new TextField
            {
                value = property.stringValue
            };
            field.AddToClassList("row-field");
            field.BindProperty(property);

            var values = new VisualElement();
            values.AddToClassList("inline-values");
            values.style.alignItems = Align.Center;
            values.Add(field);

            var searchButton = new Button
            {
                text = string.Empty
            };
            searchButton.tooltip = "Search";
            searchButton.clicked += () => ShowScenePicker(searchButton, property);
            searchButton.style.width = 28f;
            searchButton.style.minWidth = 28f;
            searchButton.style.unityTextAlign = TextAnchor.MiddleCenter;

            Texture searchIcon = EditorGUIUtility.IconContent("Search Icon").image;
            if (searchIcon is Texture2D searchTexture)
            {
                searchButton.style.backgroundImage = new StyleBackground(searchTexture);
                searchButton.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
                searchButton.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
                searchButton.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
                searchButton.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            }
            else
            {
                searchButton.text = "Q";
            }

            values.Add(searchButton);
            row.Add(values);
            container.Add(row);
        }

        private void ShowScenePicker(Button anchor, SerializedProperty property)
        {
            if (anchor == null || property == null)
            {
                return;
            }

            UnityEditor.PopupWindow.Show(anchor.worldBound, new ScenePickerPopup(property.stringValue, OnSceneSelected));
        }

        private void OnSceneSelected(ScenePickerEntry scene)
        {
            if (scene == null)
            {
                return;
            }

            if (scene.IsMissing)
            {
                bool removeScene = EditorUtility.DisplayDialog(
                    "Scene Missing",
                    $"The scene entry '{scene.Name}' no longer exists in the project.\n\nDo you want to remove it from Build Settings?",
                    "Remove Scene",
                    "Keep It");

                if (removeScene)
                {
                    RemoveSceneFromBuildSettings(scene);
                }

                return;
            }

            if (!scene.InBuildSettings)
            {
                bool addToBuild = EditorUtility.DisplayDialog(
                    "Scene Not In Build Settings",
                    $"The scene '{scene.Name}' is not in Build Settings.\n\nDo you want to add it now?",
                    "Add Scene",
                    "Cancel");

                if (!addToBuild)
                {
                    return;
                }

                AddSceneToBuildSettings(scene);
            }

            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty("defaultSceneName");
            if (property == null)
            {
                return;
            }

            property.stringValue = scene.Name;
            serializedObject.ApplyModifiedProperties();
        }

        private static void AddSceneToBuildSettings(ScenePickerEntry scene)
        {
            if (scene == null || string.IsNullOrWhiteSpace(scene.Path))
            {
                return;
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            var updatedScenes = new List<EditorBuildSettingsScene>(existingScenes)
            {
                new EditorBuildSettingsScene(scene.Path, true)
            };

            EditorBuildSettings.scenes = updatedScenes.ToArray();
        }

        private static void RemoveSceneFromBuildSettings(ScenePickerEntry scene)
        {
            if (scene == null || string.IsNullOrWhiteSpace(scene.Path))
            {
                return;
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            var updatedScenes = new List<EditorBuildSettingsScene>();

            for (int i = 0; i < existingScenes.Length; i++)
            {
                EditorBuildSettingsScene buildScene = existingScenes[i];
                if (buildScene == null || string.Equals(buildScene.path, scene.Path, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                updatedScenes.Add(buildScene);
            }

            EditorBuildSettings.scenes = updatedScenes.ToArray();
        }

        protected void AddHelpLabel(VisualElement container, string text)
        {
            if (container == null)
            {
                return;
            }

            var box = new VisualElement();
            box.AddToClassList("inline-note-box");

            var label = new Label(text);
            label.AddToClassList("inline-note");
            box.Add(label);

            container.Add(box);
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

        private void OnRuntimeChanged()
        {
            UpdateConditionalVisibility();

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] is not TwinnyRuntime runtime)
                {
                    continue;
                }

                TwinnyBuildTypeSync.SyncFromRuntime(runtime);
                EditorUtility.SetDirty(runtime);
            }
        }

        private void UpdateConditionalVisibility()
        {
            bool showFadeTime = serializedObject.FindProperty("useCanvasTransition")?.boolValue ?? false;
            bool showTargetFrameRate = serializedObject.FindProperty("forceFrameRate")?.boolValue ?? false;

            if (_fadeTimeField != null)
            {
                _fadeTimeField.style.display = showFadeTime ? DisplayStyle.Flex : DisplayStyle.None;
            }

            if (_targetFrameRateField != null)
            {
                _targetFrameRateField.style.display = showTargetFrameRate ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private static Sprite LoadSprite(string path, string spriteName)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
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
#endif
