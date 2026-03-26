#if UNITY_EDITOR

using Concept.SmartTools;
using Concept.SmartTools.Editor;
using Concept.UI;
using Concept.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Twinny.Core;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UIElements;


namespace Twinny.Editor
{
    public class SetupGuideWindow : EditorWindow
    {
        private const int SplashDurationMs = 3000;
        private const string SplashVideoPath = "Packages/com.twinny.twe26/Editor/SetupGuide/Resources/Media/TESplash.mp4";

        private class RegisteredModule
        {
            public ModuleInfo info;
            public Type type;
        }

        private static readonly Dictionary<string, RegisteredModule> m_moduleRegistry = new Dictionary<string, RegisteredModule>();

        public static SetupGuideWindow instance;
        private static Vector2 _windowSize = new Vector2(800, 600);
        private static string s_pendingSectionName = "welcome";
        private static int s_pendingSectionTabIndex;
        private static bool s_hasPlayedFullSplashThisSession;


        [SerializeField] private SetupConfig _config;
        [SerializeField] private VisualTreeAsset wellcomeLayout;

        private SetupSidebarElement m_welcomeButton;

        private VisualElement m_root;
        private VisualElement m_splashScreen;
        private IMGUIContainer m_splashOverlay;
        private bool m_showLegacySplash;
        private bool m_playFullSplashThisOpen;
        private bool m_modulesInitialized;
        private double m_splashEndTime;
        private VideoClip m_splashClip;
        private GameObject m_videoHost;
        private VideoPlayer m_videoPlayer;
        private RenderTexture m_splashRenderTexture;
        
        private ScrollView m_SideBar;
        private VisualElement m_MainContent;
        private SmartBuilderView m_embeddedSmartBuilderView;


        //Overlay Elements
        private VisualElement m_overlay;
        public HintElement hint;

        private static string m_RootPath;

        public string packageVersion = "?.?.?";
        public VisualElement OverlayHost => m_overlay;


        [MenuItem("Twinny/Setup Guide &T")]
        public static void Open()
        {
            if (instance == null)
                instance = Resources.FindObjectsOfTypeAll<SetupGuideWindow>().FirstOrDefault();

            if (instance == null)
                instance = CreateInstance<SetupGuideWindow>();

            var pkgInfo = SmartTools.GetPackageInfo(typeof(TwinnyManager));
            instance.titleContent = new GUIContent(pkgInfo.displayName);
            instance.minSize = instance.maxSize = _windowSize;
            instance.ShowUtility();
            instance.Focus();
        }

        public static void OpenSection(string sectionName, int tabIndex = 0)
        {
            string requestedSectionName = string.IsNullOrWhiteSpace(sectionName) ? "welcome" : sectionName.ToLowerInvariant();
            int requestedTabIndex = tabIndex;
            s_pendingSectionName = requestedSectionName;
            s_pendingSectionTabIndex = requestedTabIndex;
            Open();

            TryShowSection(requestedSectionName, requestedTabIndex);
            EditorApplication.delayCall += () => TryShowSection(requestedSectionName, requestedTabIndex);
        }
        private void OnEnable()
        {
            instance = this;
            var script = MonoScript.FromScriptableObject(this);
            var fullPath = AssetDatabase.GetAssetPath(script);
            m_RootPath = Path.GetDirectoryName(fullPath);
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            if (m_embeddedSmartBuilderView != null && m_embeddedSmartBuilderView.BlocksHostClose)
            {
               // Debug.Log("[SetupGuideWindow] Window is closing during upload. Cancelling embedded Smart Builder upload.");//
                m_embeddedSmartBuilderView.CancelActiveUpload();
            }
            BindEmbeddedSmartBuilderView(null);
            StopLegacySplash();
            if (ReferenceEquals(instance, this))
                instance = null;
        }

        private void OnDestroy()
        {
            StopLegacySplash();
            if (ReferenceEquals(instance, this))
                instance = null;
        }

        public void CreateGUI()
        {
            m_modulesInitialized = false;

            // Carrega o layout
            m_root = _config.visualTreeAsset.Instantiate();
            m_root.style.flexGrow = 1;
            rootVisualElement.Add(m_root);
            CreateSplashOverlay();

            m_splashScreen = m_root.Q<VisualElement>("SplashScreen");
            if (m_splashScreen != null)
                m_splashScreen.style.display = DisplayStyle.None;

            // Pega referências
            m_SideBar = m_root.Q<ScrollView>("Sidebar");
            m_MainContent = m_root.Q<VisualElement>("mainContent");
            m_welcomeButton = m_root.Q<SetupSidebarElement>("WelcomeButton");
            m_welcomeButton.RegisterCallback<ClickEvent>(evt =>
            {
                SelectButton(m_welcomeButton);
                ShowSection("welcome");
            });
            var versionInfo = m_root.Q<Label>("VersionInfo");
            packageVersion = versionInfo.text = "v" + GetPackageVersion();


            //overlay

            m_overlay = m_root.Q<VisualElement>("Overlay");
            hint = m_root.Q<HintElement>("HintElement");
            HintElementExtensions.SetHint(hint);

            InitSections();
            StartLegacySplash();
        }

        private async void InitSections()
        {
            var modules = m_moduleRegistry.Values
                .Where(module => module.info != null && !string.Equals(module.info.moduleName, "welcome", StringComparison.OrdinalIgnoreCase))
                .OrderBy(module => module.info.sortOrder)
                .ThenBy(module => module.info.moduleDisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(module => module.info);

            foreach (var module in modules)
                await AddSidebarButton(module);

            ShowSection(s_pendingSectionName, s_pendingSectionTabIndex);
            m_modulesInitialized = true;
        }

        private void StartLegacySplash()
        {
            StopLegacySplash();

            m_playFullSplashThisOpen = !s_hasPlayedFullSplashThisSession;

            m_splashClip = AssetDatabase.LoadAssetAtPath<VideoClip>(SplashVideoPath);
            if (m_splashClip == null)
                return;

            m_videoHost = EditorUtility.CreateGameObjectWithHideFlags(
                "SetupGuideSplashVideo",
                HideFlags.HideAndDontSave,
                typeof(VideoPlayer));

            m_videoPlayer = m_videoHost.GetComponent<VideoPlayer>();
            m_videoPlayer.playOnAwake = false;
            m_videoPlayer.waitForFirstFrame = true;
            m_videoPlayer.skipOnDrop = false;
            m_videoPlayer.isLooping = false;
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
            m_videoPlayer.source = VideoSource.VideoClip;
            m_videoPlayer.clip = m_splashClip;
            m_videoPlayer.renderMode = VideoRenderMode.RenderTexture;

            EnsureSplashRenderTexture();
            m_videoPlayer.targetTexture = m_splashRenderTexture;
            m_videoPlayer.Prepare();
            m_videoPlayer.Play();

            m_showLegacySplash = true;
            m_splashEndTime = m_playFullSplashThisOpen
                ? EditorApplication.timeSinceStartup + (SplashDurationMs / 1000.0)
                : EditorApplication.timeSinceStartup;
            s_hasPlayedFullSplashThisSession = true;
            if (m_splashOverlay != null)
            {
                m_splashOverlay.style.display = DisplayStyle.Flex;
                m_splashOverlay.MarkDirtyRepaint();
            }
        }

        private void StopLegacySplash()
        {
            m_showLegacySplash = false;
            if (m_splashOverlay != null)
                m_splashOverlay.style.display = DisplayStyle.None;

            if (m_videoPlayer != null)
            {
                m_videoPlayer.Stop();
                m_videoPlayer.targetTexture = null;
            }

            if (m_videoHost != null)
                DestroyImmediate(m_videoHost);

            m_videoHost = null;
            m_videoPlayer = null;
            m_splashClip = null;

            if (m_splashRenderTexture != null)
            {
                m_splashRenderTexture.Release();
                DestroyImmediate(m_splashRenderTexture);
                m_splashRenderTexture = null;
            }
        }

        private void EnsureSplashRenderTexture()
        {
            int width = Mathf.Max(1, Mathf.RoundToInt(position.width));
            int height = Mathf.Max(1, Mathf.RoundToInt(position.height));

            if (m_splashRenderTexture != null &&
                m_splashRenderTexture.width == width &&
                m_splashRenderTexture.height == height)
                return;

            if (m_splashRenderTexture != null)
            {
                m_splashRenderTexture.Release();
                DestroyImmediate(m_splashRenderTexture);
            }

            m_splashRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            m_splashRenderTexture.Create();

            if (m_videoPlayer != null)
                m_videoPlayer.targetTexture = m_splashRenderTexture;
        }

        private void OnEditorUpdate()
        {
            if (!m_showLegacySplash)
                return;

            bool splashFinished = !m_playFullSplashThisOpen || EditorApplication.timeSinceStartup >= m_splashEndTime;
            if (m_modulesInitialized && splashFinished)
            {
                StopLegacySplash();
                m_splashOverlay?.MarkDirtyRepaint();
                return;
            }

            EnsureSplashRenderTexture();
            m_splashOverlay?.MarkDirtyRepaint();
        }

        private void CreateSplashOverlay()
        {
            if (m_splashOverlay != null)
                return;

            m_splashOverlay = new IMGUIContainer(DrawLegacySplash)
            {
                pickingMode = PickingMode.Ignore
            };
            m_splashOverlay.style.position = Position.Absolute;
            m_splashOverlay.style.left = 0;
            m_splashOverlay.style.top = 0;
            m_splashOverlay.style.right = 0;
            m_splashOverlay.style.bottom = 0;
            m_splashOverlay.style.display = DisplayStyle.None;
            rootVisualElement.Add(m_splashOverlay);
        }

        private void DrawLegacySplash()
        {
            if (!m_showLegacySplash)
                return;

            Rect splashRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(splashRect, Color.black);

            if (Event.current.type == EventType.Repaint && m_splashRenderTexture != null)
                GUI.DrawTexture(splashRect, m_splashRenderTexture, ScaleMode.StretchToFill, false);

            DrawSplashOverlay(splashRect);
        }

        private void DrawSplashOverlay(Rect splashRect)
        {
            Rect yearRect = new Rect(splashRect.x + (splashRect.width * .65f), splashRect.y + (splashRect.height * 0.54f), 120f, 28f);
            Rect loadingRect = new Rect(splashRect.x + 12f, splashRect.yMax - 34f, 220f, 24f);

            GUIStyle yearStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.UpperRight,
                fontStyle = FontStyle.Bold,
                fontSize = 18,
                normal = { textColor = new Color(210f / 255f, 255f / 255f, 173f / 255f, 0.72f) }
            };

            GUIStyle loadingStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            GUI.Label(yearRect, "2026", yearStyle);
            GUI.Label(loadingRect, "Loading Modules...", loadingStyle);
        }

        private async Task AddSidebarButton(ModuleInfo module)
        {

            bool enabled = await SmartTools.IsPackageInstalledAsync(module.moduleName);// IsPackageInstalledSync(module.moduleName);


            SetupSidebarElement button = new SetupSidebarElement();
            button.RegisterCallback<ClickEvent>(evt =>
            {
                if (button.ClassListContains("disabled"))
                {
                    InstallPlatformRequest(module);
                    return;
                }
                SelectButton(button);
                ShowSection(module.moduleName);
            });
            if (!enabled)
                button.tooltip = $"Install {module.moduleDisplayName} platform";

            button.descriptionLabel.text = module.moduleDisplayName;

            if (module.moduleIcon != null)
            {
                button.thumbnailIcon = (module.moduleIcon);
            }

            button.AddToClassList(enabled ? "enabled" : "disabled");

            m_SideBar.Add(button);
        }

        private void SelectButton(SetupSidebarElement selectedButton)
        {
            m_welcomeButton.EnableInClassList("selected", (selectedButton == m_welcomeButton));

            foreach (var child in m_SideBar.Children())
            {
                bool active = child == selectedButton;
                child.EnableInClassList("selected", active);
            }
        }

        private static void TryShowSection(string sectionName, int tabIndex)
        {
            if (instance == null || instance.m_root == null || !instance.m_modulesInitialized || instance.m_MainContent == null)
                return;

            instance.ShowSection(sectionName, tabIndex);
        }


        void ShowSection(string name)
        {
            ShowSection(name, 0);
        }

        void ShowSection(string name, int tabIndex)
        {
            name = name.ToLowerInvariant();

            if (name == "welcome" && m_welcomeButton != null)
                SelectButton(m_welcomeButton);

            m_MainContent.Clear(); // limpa conteúdo antigo

            if (m_moduleRegistry.TryGetValue(name, out var registeredModule))
            {
                var module = (IModuleSetup)Activator.CreateInstance(registeredModule.type);
                var moduleElement = module as VisualElement;
                moduleElement.AddToClassList("content");
                m_MainContent.Add(moduleElement);
                BindEmbeddedSmartBuilderView(moduleElement?.Q<SmartBuilderView>());
                module.OnShowSection(this, tabIndex);
            }
            else
            {
                BindEmbeddedSmartBuilderView(null);
                Debug.LogWarning($"Módulo '{name}' não registrado.");
            }

            s_pendingSectionName = "welcome";
            s_pendingSectionTabIndex = 0;


        }

        private void BindEmbeddedSmartBuilderView(SmartBuilderView newView)
        {
            if (ReferenceEquals(m_embeddedSmartBuilderView, newView))
            {
                UpdateCloseGuardState();
                return;
            }

            if (m_embeddedSmartBuilderView != null)
            {
                m_embeddedSmartBuilderView.HostCloseGuardChanged -= OnEmbeddedSmartBuilderCloseGuardChanged;
                m_embeddedSmartBuilderView.SetOverlayHost(null);
            }

            m_embeddedSmartBuilderView = newView;

            if (m_embeddedSmartBuilderView != null)
            {
                m_embeddedSmartBuilderView.HostCloseGuardChanged += OnEmbeddedSmartBuilderCloseGuardChanged;
                m_embeddedSmartBuilderView.SetOverlayHost(m_overlay);
            }

            UpdateCloseGuardState();
        }

        private void OnEmbeddedSmartBuilderCloseGuardChanged(SmartBuilderView _)
        {
            UpdateCloseGuardState();
        }

        private void UpdateCloseGuardState()
        {
            bool blocked = m_embeddedSmartBuilderView != null && m_embeddedSmartBuilderView.BlocksHostClose;
            hasUnsavedChanges = blocked;
            saveChangesMessage = blocked
                ? m_embeddedSmartBuilderView.HostCloseBlockedMessage
                : string.Empty;
        }

        public void ShowSection(ModuleInfo section)
        {
            if (section == null) return;


            /*
            if (section.layout != null)
            {
                var clone = section.sectionLayout.CloneTree();
                clone.style.flexGrow = 1f;
                m_MainContent.Add(clone);
            }
            SelectButton(section.sectionButton);
        */
        }


        /*
        public static void ShowAdvice(VisualElement panel, string text, AdviceType adviceType, string buttonText = "", Action callback = null)
        {

            Label label = panel.Q<Label>();
            label.text = text;
            Button oldButton = panel.Q<Button>();
            if (oldButton != null)
                oldButton.RemoveFromHierarchy();


            if (callback != null)
            {
                Button button = new Button();
                button.text = buttonText;
                button.clicked += callback;
                button.AddToClassList("button");
                panel.Add(button);
            }



            foreach (AdviceType item in Enum.GetValues(typeof(AdviceType)))
            {
                panel.EnableInClassList(item.ToString().ToLowerInvariant(), adviceType == item);
            }

            panel.style.display = DisplayStyle.Flex;
        }
        */



        private void CreatePropertyFieldsForClass(SerializedProperty parentProperty, VisualElement container)
        {
            container.Clear();

            SerializedProperty iterator = parentProperty.Copy();
            SerializedProperty endProperty = iterator.GetEndProperty();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, endProperty))
            {
                enterChildren = false;

                // Pula propriedades internas se necessário
                if (iterator.name == "m_Script") continue;

                VisualElement propertyField = CreatePropertyField(iterator.Copy());
                container.Add(propertyField);
            }
        }


        private VisualElement CreatePropertyField(SerializedProperty property)
        {
            // Verifica campos específicos que devem ter PathPicker
            if (property.propertyType == SerializedPropertyType.String &&
                (property.name == "buildPath" || property.name == "resourcesPath" || property.name.Contains("Path")))
            {
                return CreatePathPickerField(property, new PathPickerAttribute("Builds"));
            }

            // Para outros tipos, usa o método normal
            PropertyField propertyField = new PropertyField(property);
            propertyField.Bind(property.serializedObject);
            return propertyField;
        }

        private VisualElement CreatePathPickerField(SerializedProperty property, PathPickerAttribute attribute)
        {
            VisualElement container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;

            // REMOVE TODAS as margins manuais - deixa o padrão do Unity

            // Label - mesma configuração simples
            Label label = new Label(property.displayName);
            label.style.width = 120;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.marginLeft = 3;
            container.Add(label);

            // Campo de texto
            TextField textField = new TextField();
            textField.style.flexGrow = 1;
            textField.value = property.stringValue;
            container.Add(textField);

            // Botão de seleção
            Button button = new Button();
            button.text = "...";
            button.style.width = 30;
            button.style.marginLeft = 2;
            container.Add(button);
            PathPickerBinding binding = new PathPickerBinding(property, textField, button);
            textField.binding = binding;

            return container;
        }
        InspectorElement DrawScriptable(UnityEngine.Object scriptable)
        {

            InspectorElement inspector = new InspectorElement(scriptable);

            return inspector;
        }
        public static Sprite[] LoadSpritesFromAtlas(string path)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

            var sprites = assets.OfType<Sprite>().ToArray();

            return sprites;
        }

        public static string GetPackageVersion()
        {
            var pkgInfo = SmartTools.GetPackageInfo(typeof(TwinnyManager));
            return pkgInfo != null ? pkgInfo.version : "?.?.?";
        }

        void ShowInstallDialog(string caption, List<string> dependencies, Action onAgree)
        {
            var overlay = m_root.Q<VisualElement>("modalOverlay");
            overlay.style.display = DisplayStyle.Flex;
            var label = overlay.Q<Label>("caption");
            label.text = caption;

            var listView = overlay.Q<ListView>("packagesList");

            if (dependencies.Count() > 0)
            {
                listView.itemsSource = dependencies;
                listView.makeItem = () => new Label();
                listView.bindItem = (element, i) => (element as Label).text = dependencies[i];

                listView.SetEnabled(false);  // desabilita o controle visualmente
                listView.selectionType = SelectionType.None;
                listView.selectionChanged += _ => listView.ClearSelection();
            }
            else
                listView.style.display = DisplayStyle.None;


            m_root.Q<Button>("buttonInstall").clicked += onAgree;

            m_root.Q<Button>("buttonCancel").clicked += () =>
            {
                overlay.style.display = DisplayStyle.None;
            };

        }

        public void InstallPlatformRequest(ModuleInfo module)
        {
            var items = new List<string>();

            string msg = $"<align=center><b>Deseja instalar a plataforma {module.moduleDisplayName}?</b></align>";


            if (module.dependencies.Length > 0)
            {
                msg += "\n\nOs seguintes pacotes precisam ser instalados:";
                foreach (var pkg in module.dependencies)
                {
                    items.Add(pkg.name);
                }
            }



            ShowInstallDialog(
                msg,
                items,
                () => InstallPlatform(module.moduleInstallPath)
                );

        }


        public void InstallPlatform(string platformName)
        {
            Debug.Log($"Install {platformName} platform");
        }

        public static void RegisterModule(string name, Type moduleType)
        {
            RegisterModule(new ModuleInfo
            {
                moduleName = name,
                moduleDisplayName = name
            }, moduleType);
        }

        public static void RegisterModule(ModuleInfo moduleInfo, Type moduleType)
        {

            if (!typeof(IModuleSetup).IsAssignableFrom(moduleType))
            {
                Debug.LogError($"O tipo {moduleType.Name} não implementa IModuleSetup.");
                return;
            }

            if (moduleInfo == null)
            {
                Debug.LogError($"Module info for {moduleType.Name} is null.");
                return;
            }

            if (string.IsNullOrWhiteSpace(moduleInfo.moduleName))
            {
                Debug.LogError($"Module info for {moduleType.Name} must define moduleName.");
                return;
            }

            m_moduleRegistry[moduleInfo.moduleName.ToLowerInvariant()] = new RegisteredModule
            {
                info = moduleInfo,
                type = moduleType
            };
        }


        public static GameObject LoadFromResourcesByGUID(string guid)
        {
            // Converter GUID em caminho do asset
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(assetPath))
            {
                Debug.LogError($"Nenhum asset encontrado com GUID: {guid}");
                return null;
            }

            // Confirmar se está dentro de "Resources/"
            if (!assetPath.Contains("Resources/"))
            {
                Debug.LogError($"O asset '{assetPath}' não está dentro de uma pasta 'Resources/'.");
                return null;
            }

            // Pegar o caminho relativo à pasta Resources
            string relativePath = assetPath.Substring(assetPath.IndexOf("Resources/") + "Resources/".Length);
            relativePath = System.IO.Path.ChangeExtension(relativePath, null); // remove .prefab

            // Carregar o prefab
            GameObject prefab = Resources.Load<GameObject>(relativePath);
            if (prefab == null)
            {
                Debug.LogError($"Falha ao carregar prefab em: {relativePath}");
                return null;
            }

            return prefab;
        }

        public static bool IsInstanceOfPrefabGUID(GameObject sceneObject, string prefabGuid)
        {
            if (sceneObject == null)
            {
                Debug.LogError("GameObject em cena é nulo!");
                return false;
            }

            string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"Nenhum asset encontrado com GUID: {prefabGuid}");
                return false;
            }

            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefabAsset == null)
            {
                Debug.LogError($"Falha ao carregar prefab em: {prefabPath}");
                return false;
            }

            // Pega o "prefab original" da instância
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(sceneObject);

            // Compara se é o mesmo asset
            bool isSame = sourcePrefab == prefabAsset;

            return isSame;
        }

        public static void InjectOverlay(VisualElement element)
        {
            var overlay = instance.m_overlay;
            overlay.Clear();
            overlay.style.display = element == null ? DisplayStyle.None : DisplayStyle.Flex;

            if (element != null)
            {
                overlay.Add(element);
            }
        }

        public static void DisposeOverlay() => InjectOverlay(null);


    }
}
#endif
