#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Twinny.Shaders;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor.Shaders
{
    [InitializeOnLoad]
    internal static class AlphaClipperToolRegistrar
    {
        static AlphaClipperToolRegistrar()
        {
            Twinny.Editor.TwinnyToolbar.RegisterButton(new Twinny.Editor.TwinnyToolbarButtonDefinition(
                "Twinny.Toolbar.AlphaClipper",
                () => new AlphaClipperToolbarButton(),
                sortOrder: 100));
        }
    }

    internal static class AlphaClipperTool
    {
        private const string CutoffHeightPropertyName = "_CutoffHeight";
        private const string MinMaxWallHeightPropertyName = "_minMaxWallHeight";
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

        private static bool s_IsPanelVisible;
        private static PanelDrawer s_PanelDrawer;
        private static VisualElement s_FloatingPanelRoot;
        private static IMGUIContainer s_FloatingPanelContainer;
        private static VisualElement s_LastAnchor;

        static AlphaClipperTool()
        {
            EditorApplication.hierarchyChanged -= RefreshAllSceneViewOverlays;
            EditorApplication.hierarchyChanged += RefreshAllSceneViewOverlays;

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            SceneView.duringSceneGui -= OnSceneViewGUI;
            SceneView.duringSceneGui += OnSceneViewGUI;

            EditorApplication.delayCall += RefreshAllSceneViewOverlays;
        }

        internal static Texture2D GetToolbarIcon()
        {
            return Twinny.Editor.TwinnyToolbar.GetIconTexture("icons_15");
        }

        internal static bool IsPanelVisible()
        {
            return s_IsPanelVisible;
        }

        internal static void TogglePanel(VisualElement anchor)
        {
            if (anchor != null)
                AttachFloatingPanelTo(anchor);

            int alphaClipperCount = FindSceneAlphaClippers().Length;
            if (alphaClipperCount != 1)
            {
                if (alphaClipperCount > 1)
                    ShowWarningOnAllSceneViews(MultipleAlphaClippersWarning);
                else
                    ShowWarningOnAllSceneViews(MissingAlphaClipperWarning);

                return;
            }

            s_IsPanelVisible = !s_IsPanelVisible;
            if (s_IsPanelVisible && s_LastAnchor != null)
                AttachFloatingPanelTo(s_LastAnchor);

            UpdateFloatingPanelVisibility();
            SceneView.RepaintAll();
        }

        private static void ShowWarningOnAllSceneViews(string message)
        {
            foreach (SceneView sceneView in SceneView.sceneViews.OfType<SceneView>())
                sceneView.ShowNotification(new GUIContent(message));
        }

        private static void AttachFloatingPanelTo(VisualElement anchor)
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

            s_FloatingPanelRoot.style.left = Mathf.Clamp(left, 0f, maxLeft);
            s_FloatingPanelRoot.style.top = Mathf.Clamp(top, 0f, maxTop);
            s_FloatingPanelRoot.style.right = StyleKeyword.Null;
            s_FloatingPanelRoot.BringToFront();
            UpdateFloatingPanelVisibility();
        }

        private static VisualElement CreateFloatingPanel()
        {
            s_PanelDrawer ??= new PanelDrawer();

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

            s_FloatingPanelContainer = new IMGUIContainer(s_PanelDrawer.Draw);
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

            s_FloatingPanelRoot.style.display = s_IsPanelVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (s_IsPanelVisible)
                s_FloatingPanelRoot.BringToFront();

            s_FloatingPanelContainer?.MarkDirtyRepaint();
        }

        private static AlphaClipper[] FindSceneAlphaClippers()
        {
            return Object.FindObjectsByType<AlphaClipper>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(component => component != null && component.gameObject.scene.IsValid())
                .ToArray();
        }

        private static void RefreshAllSceneViewOverlays()
        {
            if (FindSceneAlphaClippers().Length == 1)
                return;

            s_IsPanelVisible = false;
            s_LastAnchor = null;
            UpdateFloatingPanelVisibility();
        }

        private static void OnSceneViewGUI(SceneView _)
        {
            RefreshAllSceneViewOverlays();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange _)
        {
            RefreshAllSceneViewOverlays();
        }

        private sealed class PanelDrawer
        {
            private bool _hasInitializedValue;
            private float _currentHeight;
            private GUIStyle _compactNumberFieldStyle;
            private GUIStyle _rangeLabelStyle;

            public void Draw()
            {
                EnsureStyles();

                AlphaClipper[] alphaClippers = FindSceneAlphaClippers();
                AlphaClipper alphaClipper = alphaClippers.FirstOrDefault();
                if (alphaClipper == null || alphaClippers.Length > 1)
                    return;

                Vector2 minMaxHeight = GetMinMaxWallHeight(alphaClipper);
                float minHeight = Mathf.Min(minMaxHeight.x, minMaxHeight.y);
                float maxHeight = Mathf.Max(minMaxHeight.x, minMaxHeight.y);

                if (!_hasInitializedValue)
                {
                    float shaderValue = Shader.GetGlobalFloat(CutoffHeightPropertyName);
                    float fallbackValue = Mathf.Approximately(shaderValue, 0f) && minHeight > 0f ? maxHeight : shaderValue;
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

            private void DrawRangeFields(AlphaClipper alphaClipper, ref float minHeight, ref float maxHeight)
            {
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

                    _currentHeight = Mathf.Approximately(nextMax, previousMaxValue)
                        ? Mathf.Clamp(_currentHeight, minHeight, maxHeight)
                        : maxHeight;

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
                return fieldStyle != null ? EditorGUI.FloatField(fieldRect, value, fieldStyle) : EditorGUI.FloatField(fieldRect, value);
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
                    fontSize = Mathf.Max(8, Mathf.RoundToInt(sourceStyle.fontSize * 0.8f)),
                    fixedHeight = RangeFieldHeight,
                    alignment = TextAnchor.MiddleCenter
                };

                GUIStyle sourceLabelStyle = EditorStyles.miniLabel ?? GUI.skin?.label;
                if (sourceLabelStyle == null)
                    return;

                _rangeLabelStyle = new GUIStyle(sourceLabelStyle)
                {
                    fontSize = Mathf.Max(7, Mathf.RoundToInt(sourceLabelStyle.fontSize * 0.8f)),
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 0, 0),
                    padding = new RectOffset(0, 0, 0, 0),
                    fixedHeight = RangeFieldHeight
                };
            }
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
    }

    internal sealed class AlphaClipperToolbarButton : EditorToolbarButton
    {
        public AlphaClipperToolbarButton()
        {
            icon = AlphaClipperTool.GetToolbarIcon();
            tooltip = "Open Alpha Clipper";
            clicked += () => AlphaClipperTool.TogglePanel(this);
            RegisterCallback<AttachToPanelEvent>(_ => ScheduleVisualRefresh());
            RefreshVisualState();
        }

        private void ScheduleVisualRefresh()
        {
            schedule.Execute(RefreshVisualState).Every(100);
            RefreshVisualState();
        }

        private void RefreshVisualState()
        {
            bool isActive = AlphaClipperTool.IsPanelVisible();
            style.backgroundColor = isActive
                ? new Color(0.23f, 0.41f, 0.64f, 1f)
                : StyleKeyword.Null;
        }
    }
}
#endif
