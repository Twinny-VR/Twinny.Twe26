using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Twinny.Editor.Navigation
{
    [FilePath("ProjectSettings/TwinnyLandmarkProjectDatabase.asset", FilePathAttribute.Location.ProjectFolder)]
    public class LandmarkProjectDatabase : ScriptableSingleton<LandmarkProjectDatabase>
    {
        [Serializable]
        public class Entry
        {
            public string ScenePath;
            public string SceneName;
            public string GameObjectName;
            public string HierarchyPath;
            public string LandName;
            public string LandmarkGuid;
            public string LandmarkTypeName;
        }

        [SerializeField] private List<Entry> _entries = new();
        [SerializeField] private bool _isDirty = true;
        [SerializeField] private string _lastRefreshUtc;
        [SerializeField] private string _lastRefreshBlockedReason;
        [NonSerialized] private bool _isRefreshing;
        [NonSerialized] private bool _suppressInvalidation;

        public IReadOnlyList<Entry> Entries => _entries;

        public bool IsDirty => _isDirty;

        public string LastRefreshBlockedReason => _lastRefreshBlockedReason;

        private void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            if (!string.IsNullOrWhiteSpace(_lastRefreshBlockedReason))
            {
                _lastRefreshBlockedReason = string.Empty;
            }
        }

        public void MarkDirty()
        {
            if (IsPlayModeEditingState() || _isRefreshing || _suppressInvalidation)
            {
                return;
            }

            _isDirty = true;
        }

        public bool Refresh()
        {
            if (IsPlayModeEditingState())
            {
                _lastRefreshBlockedReason = "Refresh is disabled while the editor is in Play Mode.";
                return false;
            }

            if (_isRefreshing)
            {
                _lastRefreshBlockedReason = "Refresh is already running.";
                return false;
            }

            _isRefreshing = true;
            _suppressInvalidation = true;
            _lastRefreshBlockedReason = string.Empty;

            var refreshedEntries = new List<Entry>();
            string[] sceneGuids = AssetDatabase.FindAssets("t:Scene");

            try
            {
                for (int i = 0; i < sceneGuids.Length; i++)
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(sceneGuids[i]);
                    if (string.IsNullOrWhiteSpace(scenePath))
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Twinny Landmark Database",
                        $"Scanning {scenePath}",
                        sceneGuids.Length == 0 ? 1f : (float)(i + 1) / sceneGuids.Length);

                    CollectSceneEntries(scenePath, refreshedEntries);
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                _isRefreshing = false;
                EditorApplication.delayCall += ReleaseInvalidationSuppression;
            }

            _entries = refreshedEntries
                .OrderBy(entry => entry.SceneName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.GameObjectName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            _lastRefreshUtc = DateTime.UtcNow.ToString("O");
            _isDirty = false;
            _lastRefreshBlockedReason = string.Empty;
            Save(true);
            return true;
        }

        private static void CollectSceneEntries(string scenePath, List<Entry> results)
        {
            string absolutePath = Path.GetFullPath(scenePath);
            if (!File.Exists(absolutePath))
            {
                return;
            }

            SceneYamlIndex index = SceneYamlIndex.Parse(scenePath, File.ReadAllLines(absolutePath));
            index.AppendEntries(results);
        }

        private void ReleaseInvalidationSuppression()
        {
            _suppressInvalidation = false;
        }

        private static bool IsPlayModeEditingState()
        {
            return EditorApplication.isPlayingOrWillChangePlaymode || Application.isPlaying;
        }

        private sealed class SceneYamlIndex
        {
            private readonly string _scenePath;
            private readonly string _sceneName;
            private readonly Dictionary<long, string> _gameObjectNames = new();
            private readonly Dictionary<long, TransformInfo> _transforms = new();
            private readonly Dictionary<long, StrippedGameObjectInfo> _strippedGameObjects = new();
            private readonly Dictionary<long, Dictionary<long, string>> _prefabInstanceNames = new();
            private readonly List<LandmarkInfo> _landmarks = new();

            private SceneYamlIndex(string scenePath)
            {
                _scenePath = scenePath;
                _sceneName = Path.GetFileNameWithoutExtension(scenePath);
            }

            public static SceneYamlIndex Parse(string scenePath, string[] lines)
            {
                var index = new SceneYamlIndex(scenePath);
                index.ParseDocuments(lines);
                return index;
            }

            public void AppendEntries(List<Entry> results)
            {
                for (int i = 0; i < _landmarks.Count; i++)
                {
                    LandmarkInfo landmark = _landmarks[i];
                    results.Add(new Entry
                    {
                        ScenePath = _scenePath,
                        SceneName = _sceneName,
                        GameObjectName = ResolveGameObjectName(landmark.GameObjectFileId),
                        HierarchyPath = BuildHierarchyPath(landmark.GameObjectFileId),
                        LandName = landmark.LandName,
                        LandmarkGuid = landmark.LandmarkGuid,
                        LandmarkTypeName = landmark.TypeName
                    });
                }
            }

            private void ParseDocuments(string[] lines)
            {
                var documentLines = new List<string>();

                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line.StartsWith("--- !u!", StringComparison.Ordinal))
                    {
                        FlushDocument(documentLines);
                        documentLines.Clear();
                    }

                    documentLines.Add(line);
                }

                FlushDocument(documentLines);
            }

            private void FlushDocument(List<string> documentLines)
            {
                if (documentLines == null || documentLines.Count < 2)
                {
                    return;
                }

                long fileId = ParseHeaderFileId(documentLines[0]);
                if (fileId == 0)
                {
                    return;
                }

                switch (documentLines[1].Trim())
                {
                    case "GameObject:":
                        ParseGameObject(fileId, documentLines);
                        break;
                    case "Transform:":
                        ParseTransform(fileId, documentLines);
                        break;
                    case "MonoBehaviour:":
                        ParseMonoBehaviour(documentLines);
                        break;
                    case "PrefabInstance:":
                        ParsePrefabInstance(fileId, documentLines);
                        break;
                }
            }

            private void ParseGameObject(long fileId, List<string> lines)
            {
                string name = FindValue(lines, "  m_Name: ");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    _gameObjectNames[fileId] = name;
                }

                long prefabInstanceFileId = ParseFileIdFromValue(FindValue(lines, "  m_PrefabInstance: "));
                long sourceFileId = ParseFileIdFromValue(FindValue(lines, "  m_CorrespondingSourceObject: "));
                if (prefabInstanceFileId != 0 || sourceFileId != 0)
                {
                    _strippedGameObjects[fileId] = new StrippedGameObjectInfo
                    {
                        PrefabInstanceFileId = prefabInstanceFileId,
                        CorrespondingSourceFileId = sourceFileId
                    };
                }
            }

            private void ParseTransform(long fileId, List<string> lines)
            {
                _transforms[fileId] = new TransformInfo
                {
                    GameObjectFileId = ParseFileIdFromValue(FindValue(lines, "  m_GameObject: ")),
                    ParentTransformFileId = ParseFileIdFromValue(FindValue(lines, "  m_Father: "))
                };
            }

            private void ParseMonoBehaviour(List<string> lines)
            {
                string landmarkGuid = FindValue(lines, "  landmarkGuid: ");
                string editorClassIdentifier = FindValue(lines, "  m_EditorClassIdentifier: ");
                if (!IsLandmarkLikeComponent(editorClassIdentifier, landmarkGuid))
                {
                    return;
                }

                _landmarks.Add(new LandmarkInfo
                {
                    GameObjectFileId = ParseFileIdFromValue(FindValue(lines, "  m_GameObject: ")),
                    LandmarkGuid = landmarkGuid,
                    LandName = FindValue(lines, "  landName: "),
                    TypeName = ExtractTypeName(editorClassIdentifier)
                });
            }

            private static bool IsLandmarkLikeComponent(string editorClassIdentifier, string landmarkGuid)
            {
                if (string.IsNullOrWhiteSpace(landmarkGuid))
                {
                    return false;
                }

                if (string.IsNullOrWhiteSpace(editorClassIdentifier))
                {
                    return true;
                }

                int typeSeparatorIndex = editorClassIdentifier.LastIndexOf("::", StringComparison.Ordinal);
                string typeName = typeSeparatorIndex >= 0
                    ? editorClassIdentifier[(typeSeparatorIndex + 2)..]
                    : editorClassIdentifier;

                return typeName.EndsWith("Landmark", StringComparison.Ordinal);
            }

            private static string ExtractTypeName(string editorClassIdentifier)
            {
                if (string.IsNullOrWhiteSpace(editorClassIdentifier))
                {
                    return "Landmark";
                }

                int typeSeparatorIndex = editorClassIdentifier.LastIndexOf("::", StringComparison.Ordinal);
                string typeName = typeSeparatorIndex >= 0
                    ? editorClassIdentifier[(typeSeparatorIndex + 2)..]
                    : editorClassIdentifier;

                int namespaceSeparatorIndex = typeName.LastIndexOf('.');
                return namespaceSeparatorIndex >= 0
                    ? typeName[(namespaceSeparatorIndex + 1)..]
                    : typeName;
            }

            private void ParsePrefabInstance(long fileId, List<string> lines)
            {
                Dictionary<long, string> names = null;
                long currentTargetFileId = 0;

                for (int i = 0; i < lines.Count; i++)
                {
                    string line = lines[i];
                    if (line.StartsWith("    - target: ", StringComparison.Ordinal))
                    {
                        currentTargetFileId = ParseFileIdFromValue(line.Substring("    - target: ".Length));
                        continue;
                    }

                    if (currentTargetFileId == 0 || !line.StartsWith("      propertyPath: ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string propertyPath = line.Substring("      propertyPath: ".Length);
                    if (!string.Equals(propertyPath, "m_Name", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (i + 1 >= lines.Count || !lines[i + 1].StartsWith("      value: ", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    names ??= new Dictionary<long, string>();
                    names[currentTargetFileId] = lines[i + 1].Substring("      value: ".Length);
                    currentTargetFileId = 0;
                }

                if (names != null && names.Count > 0)
                {
                    _prefabInstanceNames[fileId] = names;
                }
            }

            private string ResolveGameObjectName(long gameObjectFileId)
            {
                if (_gameObjectNames.TryGetValue(gameObjectFileId, out string name) && !string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                if (_strippedGameObjects.TryGetValue(gameObjectFileId, out StrippedGameObjectInfo stripped)
                    && _prefabInstanceNames.TryGetValue(stripped.PrefabInstanceFileId, out Dictionary<long, string> prefabNames)
                    && prefabNames.TryGetValue(stripped.CorrespondingSourceFileId, out string prefabName)
                    && !string.IsNullOrWhiteSpace(prefabName))
                {
                    return prefabName;
                }

                return "[Unnamed GameObject]";
            }

            private string BuildHierarchyPath(long gameObjectFileId)
            {
                long currentTransformFileId = FindTransformFileId(gameObjectFileId);
                if (currentTransformFileId == 0)
                {
                    return ResolveGameObjectName(gameObjectFileId);
                }

                var segments = new List<string>();
                var visited = new HashSet<long>();

                while (currentTransformFileId != 0 && visited.Add(currentTransformFileId))
                {
                    if (!_transforms.TryGetValue(currentTransformFileId, out TransformInfo transform))
                    {
                        break;
                    }

                    segments.Add(ResolveGameObjectName(transform.GameObjectFileId));
                    currentTransformFileId = transform.ParentTransformFileId;
                }

                segments.Reverse();
                return segments.Count == 0 ? ResolveGameObjectName(gameObjectFileId) : string.Join("/", segments);
            }

            private long FindTransformFileId(long gameObjectFileId)
            {
                foreach (KeyValuePair<long, TransformInfo> pair in _transforms)
                {
                    if (pair.Value.GameObjectFileId == gameObjectFileId)
                    {
                        return pair.Key;
                    }
                }

                return 0;
            }

            private static string FindValue(List<string> lines, string prefix)
            {
                for (int i = 0; i < lines.Count; i++)
                {
                    if (lines[i].StartsWith(prefix, StringComparison.Ordinal))
                    {
                        return lines[i].Substring(prefix.Length);
                    }
                }

                return string.Empty;
            }

            private static long ParseHeaderFileId(string header)
            {
                int ampersandIndex = header.IndexOf('&');
                if (ampersandIndex < 0)
                {
                    return 0;
                }

                int endIndex = header.IndexOf(' ', ampersandIndex + 1);
                string fileIdText = endIndex >= 0
                    ? header.Substring(ampersandIndex + 1, endIndex - ampersandIndex - 1)
                    : header[(ampersandIndex + 1)..];

                long.TryParse(fileIdText, out long fileId);
                return fileId;
            }

            private static long ParseFileIdFromValue(string value)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return 0;
                }

                const string marker = "fileID: ";
                int markerIndex = value.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex < 0)
                {
                    return 0;
                }

                int startIndex = markerIndex + marker.Length;
                int endIndex = value.IndexOfAny(new[] { ',', '}' }, startIndex);
                string fileIdText = endIndex >= 0
                    ? value.Substring(startIndex, endIndex - startIndex)
                    : value[startIndex..];

                long.TryParse(fileIdText, out long fileId);
                return fileId;
            }

            private sealed class TransformInfo
            {
                public long GameObjectFileId;
                public long ParentTransformFileId;
            }

            private sealed class StrippedGameObjectInfo
            {
                public long PrefabInstanceFileId;
                public long CorrespondingSourceFileId;
            }

            private sealed class LandmarkInfo
            {
                public long GameObjectFileId;
                public string LandmarkGuid;
                public string LandName;
                public string TypeName;
            }
        }
    }

    [InitializeOnLoad]
    public static class LandmarkProjectDatabaseUpdater
    {
        static LandmarkProjectDatabaseUpdater()
        {
            EditorApplication.projectChanged += HandleProjectChanged;
            EditorApplication.hierarchyChanged += HandleHierarchyChanged;
            EditorSceneManager.sceneSaved += HandleSceneSaved;
            Undo.undoRedoPerformed += HandleUndoRedoPerformed;
        }

        private static void HandleProjectChanged()
        {
            LandmarkProjectDatabase.instance.MarkDirty();
        }

        private static void HandleHierarchyChanged()
        {
            LandmarkProjectDatabase.instance.MarkDirty();
        }

        private static void HandleUndoRedoPerformed()
        {
            LandmarkProjectDatabase.instance.MarkDirty();
        }

        private static void HandleSceneSaved(UnityEngine.SceneManagement.Scene scene)
        {
            LandmarkProjectDatabase.instance.MarkDirty();
        }
    }
}
