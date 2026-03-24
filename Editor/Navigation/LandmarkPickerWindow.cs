using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Twinny.Editor.Navigation
{
    public class LandmarkPickerWindow : EditorWindow
    {
        private const float WindowWidth = 560f;
        private const float MinWindowHeight = 480f;
        private const float MaxWindowHeight = 720f;

        private Action<LandmarkProjectDatabase.Entry> _onSelected;
        private Vector2 _scrollPosition;
        private string _searchTerm = string.Empty;
        private readonly Dictionary<string, bool> _sceneFoldouts = new();
        private string _requiredLandmarkTypeName;
        private bool _hasAttemptedInitialRefresh;

        public static void ShowModal(
            Action<LandmarkProjectDatabase.Entry> onSelected,
            Rect preferredHostRect,
            string requiredLandmarkTypeName = null,
            string windowTitle = "Landmark Search")
        {
            var window = CreateInstance<LandmarkPickerWindow>();
            window._onSelected = onSelected;
            window._requiredLandmarkTypeName = requiredLandmarkTypeName;
            window.titleContent = new GUIContent(windowTitle);
            window.minSize = new Vector2(WindowWidth, MinWindowHeight);
            window.maxSize = new Vector2(WindowWidth, MaxWindowHeight);
            window.position = BuildCenteredRect(preferredHostRect, window.minSize);
            window.ShowModal();
        }

        private static Rect BuildCenteredRect(Rect hostRect, Vector2 windowSize)
        {
            Rect fallbackHost = GetFallbackHostRect();
            Rect targetHost = hostRect.width > 0f && hostRect.height > 0f ? hostRect : fallbackHost;

            float width = WindowWidth;
            float height = Mathf.Min(windowSize.y, Mathf.Max(320f, targetHost.height - 40f));
            float x = targetHost.x + (targetHost.width - width) * 0.5f;
            float y = targetHost.y + (targetHost.height - height) * 0.5f;

            return new Rect(x, y, width, height);
        }

        private static Rect GetFallbackHostRect()
        {
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                return focusedWindow.position;
            }

            EditorWindow mouseOverWindow = EditorWindow.mouseOverWindow;
            if (mouseOverWindow != null)
            {
                return mouseOverWindow.position;
            }

            return new Rect(120f, 120f, 1200f, 800f);
        }

        private void OnGUI()
        {
            TryInitialRefreshIfNeeded();
            DrawToolbar();

            if (!string.IsNullOrWhiteSpace(LandmarkProjectDatabase.instance.LastRefreshBlockedReason))
            {
                EditorGUILayout.HelpBox(LandmarkProjectDatabase.instance.LastRefreshBlockedReason, MessageType.Warning);
            }

            IReadOnlyList<LandmarkProjectDatabase.Entry> entries = LandmarkProjectDatabase.instance.Entries;
            List<LandmarkProjectDatabase.Entry> filteredEntries = FilterEntries(entries, _searchTerm);

            GUILayout.Space(6f);

            if (filteredEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No landmarks found for the current search.", MessageType.Info);
                return;
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (IGrouping<string, LandmarkProjectDatabase.Entry> sceneGroup in GroupEntries(filteredEntries))
            {
                DrawSceneGroup(sceneGroup);
                GUILayout.Space(8f);
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Landmark Search", EditorStyles.boldLabel);
            if (!string.IsNullOrWhiteSpace(_requiredLandmarkTypeName))
            {
                GUILayout.Space(8f);
                GUILayout.Label($"Type: {_requiredLandmarkTypeName}", EditorStyles.miniLabel);
            }
            GUILayout.FlexibleSpace();

            string nextSearch = GUILayout.TextField(_searchTerm, GUI.skin.FindStyle("ToolbarSeachTextField") ?? EditorStyles.toolbarTextField, GUILayout.Width(240f));
            if (!string.Equals(nextSearch, _searchTerm, StringComparison.Ordinal))
            {
                _searchTerm = nextSearch;
                Repaint();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(56f)))
            {
                LandmarkProjectDatabase.instance.Refresh();
                Repaint();
            }

            if (GUILayout.Button("Close", EditorStyles.toolbarButton, GUILayout.Width(48f)))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawCard(LandmarkProjectDatabase.Entry entry)
        {
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };
            GUIStyle miniStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                wordWrap = false
            };
            GUIStyle disabledStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight
            };
            disabledStyle.normal.textColor = EditorStyles.centeredGreyMiniLabel.normal.textColor;

            Rect cardRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            Rect contentRect = GUILayoutUtility.GetRect(1f, 42f, GUILayout.ExpandWidth(true));

            const float buttonWidth = 72f;
            const float padding = 8f;
            float contentWidth = Mathf.Max(120f, contentRect.width - buttonWidth - padding);
            float columnWidth = Mathf.Max(60f, contentWidth * 0.5f - 6f);

            Rect leftTopRect = new Rect(contentRect.x, contentRect.y, columnWidth, 18f);
            Rect rightTopRect = new Rect(contentRect.x + columnWidth + 12f, contentRect.y, columnWidth, 18f);
            Rect leftBottomRect = new Rect(contentRect.x, contentRect.y + 16f, columnWidth, 16f);
            Rect rightBottomRect = new Rect(contentRect.x + columnWidth + 12f, contentRect.y + 16f, columnWidth, 16f);
            Rect buttonRect = new Rect(cardRect.xMax - buttonWidth - padding, contentRect.y - 1f, buttonWidth, 22f);
            Rect guidRect = new Rect(contentRect.x, contentRect.y + 31f, contentRect.width, 14f);

            GUI.Label(leftTopRect, FormatOrDefault(entry.SceneName), headerStyle);
            GUI.Label(rightTopRect, FormatOrDefault(entry.GameObjectName), headerStyle);
            GUI.Label(leftBottomRect, FormatOrDefault(entry.LandmarkTypeName), miniStyle);
            GUI.Label(rightBottomRect, FormatLandName(entry.LandName), miniStyle);
            GUI.Label(guidRect, FormatOrDefault(entry.LandmarkGuid), disabledStyle);

            if (GUI.Button(buttonRect, "Select"))
            {
                _onSelected?.Invoke(entry);
                Close();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneGroup(IGrouping<string, LandmarkProjectDatabase.Entry> sceneGroup)
        {
            string sceneKey = sceneGroup.Key;
            bool expanded = GetFoldoutState(sceneKey);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            bool nextExpanded = EditorGUILayout.Foldout(
                expanded,
                $"{sceneKey} ({sceneGroup.Count()})",
                true,
                new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold
                });

            if (nextExpanded != expanded)
            {
                _sceneFoldouts[sceneKey] = nextExpanded;
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(expanded ? "Collapse" : "Expand", EditorStyles.miniButton, GUILayout.Width(70f)))
            {
                _sceneFoldouts[sceneKey] = !expanded;
                nextExpanded = !expanded;
            }
            EditorGUILayout.EndHorizontal();

            if (nextExpanded)
            {
                GUILayout.Space(4f);
                foreach (LandmarkProjectDatabase.Entry entry in sceneGroup)
                {
                    DrawCard(entry);
                    GUILayout.Space(4f);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private List<LandmarkProjectDatabase.Entry> FilterEntries(IReadOnlyList<LandmarkProjectDatabase.Entry> entries, string searchTerm)
        {
            var filtered = new List<LandmarkProjectDatabase.Entry>();
            string normalizedSearch = searchTerm?.Trim();

            for (int i = 0; i < entries.Count; i++)
            {
                LandmarkProjectDatabase.Entry entry = entries[i];
                if (!MatchesRequiredType(entry))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(normalizedSearch) || Matches(entry, normalizedSearch))
                {
                    filtered.Add(entry);
                }
            }

            return filtered;
        }

        private IEnumerable<IGrouping<string, LandmarkProjectDatabase.Entry>> GroupEntries(List<LandmarkProjectDatabase.Entry> entries)
        {
            return entries
                .GroupBy(entry => string.IsNullOrWhiteSpace(entry.SceneName) ? "[No Scene]" : entry.SceneName)
                .OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase);
        }

        private bool GetFoldoutState(string sceneKey)
        {
            if (_sceneFoldouts.TryGetValue(sceneKey, out bool expanded))
            {
                return expanded;
            }

            _sceneFoldouts[sceneKey] = true;
            return true;
        }

        private static bool Matches(LandmarkProjectDatabase.Entry entry, string searchTerm)
        {
            return Contains(entry.SceneName, searchTerm)
                || Contains(entry.GameObjectName, searchTerm)
                || Contains(entry.LandName, searchTerm)
                || Contains(entry.LandmarkTypeName, searchTerm)
                || Contains(entry.LandmarkGuid, searchTerm)
                || Contains(entry.HierarchyPath, searchTerm);
        }

        private bool MatchesRequiredType(LandmarkProjectDatabase.Entry entry)
        {
            if (string.IsNullOrWhiteSpace(_requiredLandmarkTypeName))
            {
                return true;
            }

            return string.Equals(entry.LandmarkTypeName, _requiredLandmarkTypeName, StringComparison.Ordinal);
        }

        private static bool Contains(string source, string searchTerm)
        {
            return !string.IsNullOrWhiteSpace(source)
                && source.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string FormatOrDefault(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "[default]" : value;
        }

        private static string FormatLandName(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "NoName" : value;
        }

        private void TryInitialRefreshIfNeeded()
        {
            if (_hasAttemptedInitialRefresh)
            {
                return;
            }

            _hasAttemptedInitialRefresh = true;
            if (LandmarkProjectDatabase.instance.Entries.Count > 0)
            {
                return;
            }

            LandmarkProjectDatabase.instance.Refresh();
        }
    }
}
