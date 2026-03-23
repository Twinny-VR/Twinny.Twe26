#if UNITY_EDITOR
using System.Linq;
using Twinny.Shaders;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

namespace Twinny.Editor.Shaders
{
    internal static class AlphaClipperOverlayIds
    {
        internal const string PanelOverlayId = "Twinny.AlphaClipper";
        internal const string ToolbarOverlayId = "Twinny.AlphaClipperToolbar";
        internal const string OpenPanelButtonId = "Twinny.AlphaClipperToolbar.OpenPanel";
        internal const string SecondaryButtonId = "Twinny.AlphaClipperToolbar.Secondary";
    }

    [InitializeOnLoad]
    internal sealed class AlphaClipperSceneOverlay
    {
        private const string CutoffHeightPropertyName = "_CutoffHeight";
        private const string MinMaxWallHeightPropertyName = "_minMaxWallHeight";
        private const string OverlayIconAssetPath = "Packages/com.twinny.twe26/Editor/Icons/icons.png";
        private const string OverlayIconSpriteName = "icons_0";
        private const string SecondaryIconSpriteName = "icons_15";
        private const float DefaultMinHeight = 0f;
        private const float DefaultMaxHeight = 3f;
        private const float PanelWidth = 64f;
        private const float PanelHeight = 220f;
        private const float SliderWidth = 22f;
        private const float SliderHeight = 118f;
        private const float TrackWidth = 4f;
        private const float ThumbSize = 12f;
        private const float RangeFieldWidth = 40f;
        private const float RangeLabelWidth = 16f;
        private const float RangeFieldHeight = 16f;
        private const string ExpandedPanelTitle = "Alpha\nClipper";
        private const string MissingAlphaClipperWarning = "No AlphaClipper found in scene.";
        private const string MultipleAlphaClippersWarning = "More than one AlphaClipper found in scene.";

        private static readonly int SliderControlId = "Twinny.AlphaClipperOverlay.Slider".GetHashCode();
        private static readonly Dictionary<string, Texture2D> s_IconCache = new Dictionary<string, Texture2D>();
        private static bool s_IsPanelOverlayVisible;
        private static AlphaClipperSceneOverlay s_PanelGuiInstance;
        private static VisualElement s_FloatingPanelRoot;
        private static IMGUIContainer s_FloatingPanelContainer;
        private static VisualElement s_LastAnchor;

        private bool _hasInitializedValue;
        private float _currentHeight;
        private GUIStyle _compactNumberFieldStyle;
        private GUIStyle _rangeLabelStyle;

        static AlphaClipperSceneOverlay()
        {
            EditorApplication.hierarchyChanged -= RefreshAllSceneViewOverlays;
            EditorApplication.hierarchyChanged += RefreshAllSceneViewOverlays;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            SceneView.duringSceneGui -= OnSceneViewGUI;
            SceneView.duringSceneGui += OnSceneViewGUI;

            EditorApplication.delayCall += RefreshAllSceneViewOverlays;
        }

        private void DrawOverlayGUI()
        {
            EnsureStyles();

            AlphaClipper[] alphaClippers = FindSceneAlphaClippers();
            AlphaClipper alphaClipper = alphaClippers.FirstOrDefault();
            if (alphaClipper == null)
                return;

            if (alphaClippers.Length > 1)
                return;

            Vector2 minMaxHeight = GetMinMaxWallHeight(alphaClipper);
            float minHeight = Mathf.Min(minMaxHeight.x, minMaxHeight.y);
            float maxHeight = Mathf.Max(minMaxHeight.x, minMaxHeight.y);

            if (!_hasInitializedValue)
            {
                float shaderValue = Shader.GetGlobalFloat(CutoffHeightPropertyName);
                float fallbackValue = Mathf.Approximately(shaderValue, 0f) && minHeight > 0f
                    ? maxHeight
                    : shaderValue;
                _currentHeight = Mathf.Clamp(fallbackValue, minHeight, maxHeight);
                ApplyCutoffHeight(_currentHeight);
                _hasInitializedValue = true;
            }
            else
            {
                _currentHeight = Mathf.Clamp(_currentHeight, minHeight, maxHeight);
            }

            GUILayout.Label(ExpandedPanelTitle, EditorStyles.centeredGreyMiniLabel);
            GUILayout.Space(1f);

            Rect sliderRect = GUILayoutUtility.GetRect(SliderWidth, SliderHeight, GUILayout.Width(PanelWidth), GUILayout.Height(SliderHeight));
            sliderRect = new Rect((PanelWidth - SliderWidth) * 0.5f, sliderRect.y, SliderWidth, SliderHeight);

            float nextHeight = DrawVerticalSlider(sliderRect, _currentHeight, minHeight, maxHeight);
            if (!Mathf.Approximately(nextHeight, _currentHeight))
            {
                _currentHeight = Mathf.Clamp(nextHeight, minHeight, maxHeight);
                ApplyCutoffHeight(_currentHeight);
            }

            GUILayout.Label(_currentHeight.ToString("F2"), EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            DrawRangeFields(alphaClipper, ref minHeight, ref maxHeight);
        }

        private static AlphaClipper FindActiveAlphaClipper()
        {
            return FindSceneAlphaClippers().FirstOrDefault();
        }

        private static AlphaClipper[] FindSceneAlphaClippers()
        {
            return Object.FindObjectsByType<AlphaClipper>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component != null && component.gameObject.scene.IsValid())
                .ToArray();
        }

        private static Vector2 GetMinMaxWallHeight(AlphaClipper alphaClipper)
        {
            if (alphaClipper == null)
                return new Vector2(DefaultMinHeight, DefaultMaxHeight);

            SerializedObject serializedObject = new SerializedObject(alphaClipper);
            SerializedProperty minMaxProperty = serializedObject.FindProperty(MinMaxWallHeightPropertyName);
            if (minMaxProperty == null || minMaxProperty.propertyType != SerializedPropertyType.Vector2)
                return new Vector2(DefaultMinHeight, DefaultMaxHeight);

            return minMaxProperty.vector2Value;
        }

        private static void ApplyCutoffHeight(float height)
        {
            AlphaClipper.SetCutoffHeight(height);
            SceneView.RepaintAll();
        }

        private static float DrawVerticalSlider(Rect sliderRect, float currentValue, float minHeight, float maxHeight)
        {
            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(SliderControlId, FocusType.Passive, sliderRect);
            Rect trackRect = new Rect(
                sliderRect.x + (sliderRect.width - TrackWidth) * 0.5f,
                sliderRect.y + ThumbSize * 0.5f,
                TrackWidth,
                sliderRect.height - ThumbSize);

            float normalizedValue = Mathf.InverseLerp(minHeight, maxHeight, currentValue);
            float thumbCenterY = Mathf.Lerp(trackRect.yMax, trackRect.yMin, normalizedValue);
            Rect fillRect = new Rect(trackRect.x, thumbCenterY, trackRect.width, trackRect.yMax - thumbCenterY);
            Rect thumbRect = new Rect(
                sliderRect.x + (sliderRect.width - ThumbSize) * 0.5f,
                thumbCenterY - ThumbSize * 0.5f,
                ThumbSize,
                ThumbSize);

            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && sliderRect.Contains(currentEvent.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        GUIUtility.keyboardControl = controlId;
                        currentEvent.Use();
                        return ValueFromMousePosition(currentEvent.mousePosition.y, trackRect, minHeight, maxHeight);
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        currentEvent.Use();
                        return ValueFromMousePosition(currentEvent.mousePosition.y, trackRect, minHeight, maxHeight);
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;
                case EventType.Repaint:
                    EditorGUI.DrawRect(trackRect, new Color(0.26f, 0.26f, 0.26f, 1f));
                    EditorGUI.DrawRect(new Rect(trackRect.x, trackRect.y, trackRect.width, 1f), new Color(0.45f, 0.45f, 0.45f, 1f));
                    EditorGUI.DrawRect(fillRect, new Color(0.55f, 0.55f, 0.55f, 1f));
                    EditorGUI.DrawRect(thumbRect, GUIUtility.hotControl == controlId
                        ? new Color(0.86f, 0.86f, 0.86f, 1f)
                        : new Color(0.74f, 0.74f, 0.74f, 1f));
                    EditorGUI.DrawRect(new Rect(thumbRect.x, thumbRect.y, thumbRect.width, 1f), new Color(0.95f, 0.95f, 0.95f, 1f));
                    EditorGUI.DrawRect(new Rect(thumbRect.x, thumbRect.yMax - 1f, thumbRect.width, 1f), new Color(0.30f, 0.30f, 0.30f, 1f));
                    EditorGUI.DrawRect(new Rect(thumbRect.x, thumbRect.y, 1f, thumbRect.height), new Color(0.95f, 0.95f, 0.95f, 1f));
                    EditorGUI.DrawRect(new Rect(thumbRect.xMax - 1f, thumbRect.y, 1f, thumbRect.height), new Color(0.30f, 0.30f, 0.30f, 1f));
                    break;
            }

            return currentValue;
        }

        private static float ValueFromMousePosition(float mouseY, Rect trackRect, float minHeight, float maxHeight)
        {
            float normalized = Mathf.InverseLerp(trackRect.yMax, trackRect.yMin, mouseY);
            return Mathf.Lerp(minHeight, maxHeight, normalized);
        }

        private void DrawRangeFields(AlphaClipper alphaClipper, ref float minHeight, ref float maxHeight)
        {
            if (alphaClipper == null)
                return;

            SerializedObject serializedObject = new SerializedObject(alphaClipper);
            SerializedProperty minMaxProperty = serializedObject.FindProperty(MinMaxWallHeightPropertyName);
            if (minMaxProperty == null || minMaxProperty.propertyType != SerializedPropertyType.Vector2)
                return;

            Vector2 currentRange = minMaxProperty.vector2Value;
            float previousMaxValue = currentRange.y;

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical(GUILayout.Width(RangeLabelWidth + 2f + RangeFieldWidth));

            GUIStyle fieldStyle = _compactNumberFieldStyle ?? EditorStyles.numberField ?? GUI.skin?.textField;
            GUIStyle labelStyle = _rangeLabelStyle ?? EditorStyles.miniLabel;
            EditorGUI.BeginChangeCheck();
            float nextMax = DrawRangeFieldRow("Max", currentRange.y, fieldStyle, labelStyle);
            float nextMin = DrawRangeFieldRow("Min", currentRange.x, fieldStyle, labelStyle);
            if (EditorGUI.EndChangeCheck())
            {
                Vector2 nextRange = new Vector2(nextMin, nextMax);
                Undo.RecordObject(alphaClipper, "Update Alpha Clipper Range");
                minMaxProperty.vector2Value = nextRange;
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(alphaClipper);

                AlphaClipper.MinMaxWallHeight = nextRange;
                minHeight = Mathf.Min(nextRange.x, nextRange.y);
                maxHeight = Mathf.Max(nextRange.x, nextRange.y);

                if (!Mathf.Approximately(nextMax, previousMaxValue))
                    _currentHeight = maxHeight;
                else
                    _currentHeight = Mathf.Clamp(_currentHeight, minHeight, maxHeight);

                ApplyCutoffHeight(_currentHeight);
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private static float DrawRangeFieldRow(string label, float value, GUIStyle fieldStyle, GUIStyle labelStyle)
        {
            Rect rowRect = GUILayoutUtility.GetRect(
                RangeLabelWidth + 2f + RangeFieldWidth,
                RangeFieldHeight,
                GUILayout.Width(RangeLabelWidth + 2f + RangeFieldWidth),
                GUILayout.Height(RangeFieldHeight));

            Rect labelRect = new Rect(rowRect.x, rowRect.y, RangeLabelWidth, RangeFieldHeight);
            Rect fieldRect = new Rect(rowRect.x + RangeLabelWidth + 2f, rowRect.y, RangeFieldWidth, RangeFieldHeight);

            GUI.Label(labelRect, label, labelStyle);
            return fieldStyle != null
                ? EditorGUI.FloatField(fieldRect, value, fieldStyle)
                : EditorGUI.FloatField(fieldRect, value);
        }

        private void EnsureStyles()
        {
            if (_compactNumberFieldStyle != null)
                return;

            GUIStyle sourceStyle = EditorStyles.numberField ?? GUI.skin?.GetStyle("TextField") ?? GUI.skin?.textField;
            if (sourceStyle == null)
                return;

            _compactNumberFieldStyle = new GUIStyle(sourceStyle)
            {
                fontSize = Mathf.Max(8, Mathf.RoundToInt(sourceStyle.fontSize * 0.8f))
            };
            _compactNumberFieldStyle.fixedHeight = RangeFieldHeight;
            _compactNumberFieldStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle sourceLabelStyle = EditorStyles.miniLabel ?? GUI.skin?.label;
            if (sourceLabelStyle == null)
                return;

            _rangeLabelStyle = new GUIStyle(sourceLabelStyle)
            {
                fontSize = Mathf.Max(7, Mathf.RoundToInt(sourceLabelStyle.fontSize * 0.8f)),
                alignment = TextAnchor.MiddleCenter
            };
            _rangeLabelStyle.margin = new RectOffset(0, 0, 0, 0);
            _rangeLabelStyle.padding = new RectOffset(0, 0, 0, 0);
            _rangeLabelStyle.fixedHeight = RangeFieldHeight;
        }

        private static void ShowWarningOnAllSceneViews()
        {
            foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
                sceneView.ShowNotification(new GUIContent(MultipleAlphaClippersWarning));
        }

        private static void ShowMissingAlphaClipperWarningOnAllSceneViews()
        {
            foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
                sceneView.ShowNotification(new GUIContent(MissingAlphaClipperWarning));
        }

        private static void ClearWarningOnAllSceneViews()
        {
            foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
                sceneView.RemoveNotification();
        }

        internal static Texture2D GetToolbarIcon()
        {
            return GetIconTexture(OverlayIconSpriteName);
        }

        internal static Texture2D GetSecondaryToolbarIcon()
        {
            return GetIconTexture(SecondaryIconSpriteName);
        }

        internal static bool IsPanelOverlayVisible()
        {
            return s_IsPanelOverlayVisible;
        }

        internal static void TogglePanelOverlay()
        {
            int alphaClipperCount = FindSceneAlphaClippers().Length;
            if (alphaClipperCount != 1)
            {
                if (alphaClipperCount > 1)
                    ShowWarningOnAllSceneViews();
                else
                    ShowMissingAlphaClipperWarningOnAllSceneViews();

                return;
            }

            s_IsPanelOverlayVisible = !s_IsPanelOverlayVisible;
            if (s_IsPanelOverlayVisible && s_LastAnchor != null)
                AttachFloatingPanelTo(s_LastAnchor);

            UpdateFloatingPanelVisibility();
            SceneView.RepaintAll();
        }

        private static Texture2D GetIconTexture(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName))
                return null;

            if (s_IconCache.TryGetValue(spriteName, out Texture2D cachedIcon) && cachedIcon != null)
                return cachedIcon;

            Sprite iconSprite = AssetDatabase.LoadAllAssetsAtPath(OverlayIconAssetPath)
                .OfType<Sprite>()
                .FirstOrDefault(sprite => sprite.name == spriteName);
            if (iconSprite == null)
                return null;

            Texture2D iconTexture = CreateTextureFromSprite(iconSprite) ?? iconSprite.texture;
            s_IconCache[spriteName] = iconTexture;
            return iconTexture;
        }

        private static Texture2D CreateTextureFromSprite(Sprite sprite)
        {
            if (sprite == null)
                return null;

            Rect rect = sprite.textureRect;
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            if (width <= 0 || height <= 0)
                return null;

            Texture2D iconTexture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = $"{sprite.name}_OverlayIcon",
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                RenderTexture renderTexture = RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                RenderTexture previous = RenderTexture.active;

                try
                {
                    Vector2 textureSize = new Vector2(sprite.texture.width, sprite.texture.height);
                    Vector2 scale = new Vector2(rect.width / textureSize.x, rect.height / textureSize.y);
                    Vector2 offset = new Vector2(rect.x / textureSize.x, rect.y / textureSize.y);

                    Graphics.Blit(sprite.texture, renderTexture, scale, offset);
                    RenderTexture.active = renderTexture;
                    iconTexture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
                    iconTexture.Apply(false, true);
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(renderTexture);
                }

                return iconTexture;
            }
            catch
            {
                Object.DestroyImmediate(iconTexture);
                return null;
            }
        }

        internal static void AttachFloatingPanelTo(VisualElement anchor)
        {
            if (anchor == null)
                return;

            s_LastAnchor = anchor;
            VisualElement root = anchor;
            while (root.parent != null)
                root = root.parent;

            bool needsRecreate = s_FloatingPanelRoot == null
                || s_FloatingPanelContainer == null
                || s_FloatingPanelRoot.parent != root
                || s_FloatingPanelRoot.panel == null
                || s_FloatingPanelContainer.panel == null;

            if (needsRecreate)
            {
                s_FloatingPanelRoot?.RemoveFromHierarchy();
                s_FloatingPanelRoot = CreateFloatingPanel();
                root.Add(s_FloatingPanelRoot);
            }

            float rootWidth = root.resolvedStyle.width;
            if (float.IsNaN(rootWidth) || rootWidth <= 0f)
                rootWidth = root.worldBound.width;

            float rootHeight = root.resolvedStyle.height;
            if (float.IsNaN(rootHeight) || rootHeight <= 0f)
                rootHeight = root.worldBound.height;

            Vector2 localTopLeft = root.WorldToLocal(new Vector2(anchor.worldBound.xMin, anchor.worldBound.yMin));
            float left = localTopLeft.x - PanelWidth - 6f;
            float top = localTopLeft.y;

            if (left < 0f)
                left = localTopLeft.x + anchor.worldBound.width + 6f;

            float maxLeft = Mathf.Max(0f, rootWidth - PanelWidth);
            float maxTop = Mathf.Max(0f, rootHeight - PanelHeight);

            left = Mathf.Clamp(left, 0f, maxLeft);
            top = Mathf.Clamp(top, 0f, maxTop);

            s_FloatingPanelRoot.style.left = left;
            s_FloatingPanelRoot.style.top = top;
            s_FloatingPanelRoot.style.right = StyleKeyword.Null;
            s_FloatingPanelRoot.BringToFront();
            UpdateFloatingPanelVisibility();
        }

        private static AlphaClipperSceneOverlay GetPanelGuiInstance()
        {
            if (s_PanelGuiInstance == null)
                s_PanelGuiInstance = new AlphaClipperSceneOverlay();

            return s_PanelGuiInstance;
        }

        private static VisualElement CreateFloatingPanel()
        {
            AlphaClipperSceneOverlay panelGui = GetPanelGuiInstance();

            VisualElement root = new VisualElement();
            root.style.position = Position.Absolute;
            root.style.width = PanelWidth;
            root.style.minWidth = PanelWidth;
            root.style.height = PanelHeight;
            root.style.minHeight = PanelHeight;
            root.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
            root.style.borderTopLeftRadius = 6f;
            root.style.borderTopRightRadius = 6f;
            root.style.borderBottomLeftRadius = 6f;
            root.style.borderBottomRightRadius = 6f;
            root.style.paddingLeft = 0f;
            root.style.paddingRight = 0f;
            root.style.paddingTop = 0f;
            root.style.paddingBottom = 0f;

            s_FloatingPanelContainer = new IMGUIContainer(panelGui.DrawOverlayGUI);
            s_FloatingPanelContainer.style.position = Position.Absolute;
            s_FloatingPanelContainer.style.left = 0f;
            s_FloatingPanelContainer.style.top = 0f;
            s_FloatingPanelContainer.style.width = PanelWidth;
            s_FloatingPanelContainer.style.minWidth = PanelWidth;
            s_FloatingPanelContainer.style.height = PanelHeight;
            s_FloatingPanelContainer.style.minHeight = PanelHeight;
            root.Add(s_FloatingPanelContainer);
            root.schedule.Execute(() => s_FloatingPanelContainer?.MarkDirtyRepaint()).Every(100);
            return root;
        }

        private static void UpdateFloatingPanelVisibility()
        {
            if (s_FloatingPanelRoot == null)
                return;

            s_FloatingPanelRoot.style.display = s_IsPanelOverlayVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (s_IsPanelOverlayVisible)
                s_FloatingPanelRoot.BringToFront();

            s_FloatingPanelContainer?.MarkDirtyRepaint();
        }

        private static void RefreshAllSceneViewOverlays()
        {
            int alphaClipperCount = FindSceneAlphaClippers().Length;
            if (alphaClipperCount != 1)
            {
                s_IsPanelOverlayVisible = false;
                s_LastAnchor = null;
                UpdateFloatingPanelVisibility();
            }
        }

        private static void OnSceneViewGUI(SceneView sceneView)
        {
            RefreshAllSceneViewOverlays();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            RefreshAllSceneViewOverlays();
        }
    }

    [Overlay(
        typeof(SceneView),
        AlphaClipperOverlayIds.ToolbarOverlayId,
        "Alpha Clipper Toolbar",
        true,
        defaultDockZone = DockZone.RightColumn,
        defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 0,
        defaultLayout = Layout.VerticalToolbar)]
    internal sealed class AlphaClipperToolbarOverlay : Overlay, ICreateVerticalToolbar, ICreateHorizontalToolbar
    {
        public AlphaClipperToolbarOverlay()
        {
            collapsedIcon = AlphaClipperSceneOverlay.GetToolbarIcon();
        }

        public override VisualElement CreatePanelContent()
        {
            return CreateVerticalToolbarContent();
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            toolbar.Add(new AlphaClipperOpenPanelButton());
            toolbar.Add(new AlphaClipperSecondaryButton());
            return toolbar;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            OverlayToolbar toolbar = new OverlayToolbar();
            toolbar.Add(new AlphaClipperOpenPanelButton());
            toolbar.Add(new AlphaClipperSecondaryButton());
            return toolbar;
        }
    }

    [EditorToolbarElement(AlphaClipperOverlayIds.OpenPanelButtonId, typeof(SceneView))]
    internal sealed class AlphaClipperOpenPanelButton : EditorToolbarButton
    {
        public AlphaClipperOpenPanelButton()
        {
            icon = AlphaClipperSceneOverlay.GetToolbarIcon();
            tooltip = "Open Setup Guide";
            clicked += Twinny.Editor.SetupGuideWindow.Open;
        }
    }

    [EditorToolbarElement(AlphaClipperOverlayIds.SecondaryButtonId, typeof(SceneView))]
    internal sealed class AlphaClipperSecondaryButton : EditorToolbarButton
    {
        public AlphaClipperSecondaryButton()
        {
            icon = AlphaClipperSceneOverlay.GetSecondaryToolbarIcon();
            tooltip = "Open Alpha Clipper";
            clicked += ToggleAnchoredPopup;
            RegisterCallback<AttachToPanelEvent>(_ => ScheduleVisualRefresh());
            RefreshVisualState();
        }

        private void ToggleAnchoredPopup()
        {
            AlphaClipperSceneOverlay.AttachFloatingPanelTo(this);
            AlphaClipperSceneOverlay.TogglePanelOverlay();
        }

        private void ScheduleVisualRefresh()
        {
            schedule.Execute(RefreshVisualState).Every(100);
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            bool isActive = AlphaClipperSceneOverlay.IsPanelOverlayVisible();
            style.backgroundColor = isActive
                ? new Color(0.23f, 0.41f, 0.64f, 1f)
                : StyleKeyword.Null;
        }
    }
}
#endif
