#if UNITY_EDITOR

using Concept.SmartTools;
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
using UnityEngine.UIElements;


namespace Twinny.Editor
{
    public class SetupGuideWindow : EditorWindow
    {
        private static Dictionary<string, Type> m_moduleRegistry = new Dictionary<string, Type>();

        public static SetupGuideWindow instance;
        private static Vector2 _windowSize = new Vector2(800, 600);


        [SerializeField] private SetupConfig _config;
        [SerializeField] private VisualTreeAsset wellcomeLayout;

        private SetupSidebarElement m_welcomeButton;

        private VisualElement m_root;
        private VisualElement m_splashScreen;
        
        private ScrollView m_SideBar;
        private VisualElement m_MainContent;


        //Overlay Elements
        private VisualElement m_overlay;
        public HintElement hint;

        private static string m_RootPath;

        public string packageVersion = "?.?.?";


        [MenuItem("Twinny/Setup Guide &T")]
        public static void Open()
        {
            instance = CreateInstance<SetupGuideWindow>();
            var pkgInfo = SmartTools.GetPackageInfo(typeof(TwinnyManager));
            instance.titleContent = new GUIContent(pkgInfo.displayName);
            instance.minSize = instance.maxSize = _windowSize;
            instance.ShowUtility();
            var path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(instance));
        }
        private void OnEnable()
        {
            var script = MonoScript.FromScriptableObject(this);
            var fullPath = AssetDatabase.GetAssetPath(script);
            m_RootPath = Path.GetDirectoryName(fullPath);
            // _plusIcon = EditorImageUtils.LoadSpriteFromProjectPath(PLUS_ICON_PATH);
        }

        public void CreateGUI()
        {
            // Carrega o layout
            m_root = _config.visualTreeAsset.Instantiate();
            m_root.style.flexGrow = 1;
            rootVisualElement.Add(m_root);

            m_splashScreen = m_root.Q<VisualElement>("SplashScreen");
            m_splashScreen.style.display = DisplayStyle.Flex;

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
            VideoElement videoElement = m_splashScreen.Q<VideoElement>();
            videoElement.style.display = DisplayStyle.None;
            videoElement.OnVideoReady += () =>
            {
            videoElement.style.display = DisplayStyle.Flex;
            InitSections();
            };
        }

        private async void InitSections()
        {
            foreach (var module in _config.modules)
               await AddSidebarButton(module);
            ShowSection("welcome");

               await Task.Delay(3000);
          m_splashScreen.style.display = DisplayStyle.None;

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


        void ShowSection(string name)
        {
            name = name.ToLowerInvariant();

            m_MainContent.Clear(); // limpa conteúdo antigo

            if (m_moduleRegistry.TryGetValue(name, out var moduleType))
            {
                var module = (IModuleSetup)Activator.CreateInstance(moduleType);
                var moduleElement = module as VisualElement;
                moduleElement.AddToClassList("content");
                m_MainContent.Add(moduleElement);
                module.OnShowSection(this);
            }
            else
            {
                Debug.LogWarning($"Módulo '{name}' não registrado.");
            }


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
            return;
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

            if (!typeof(IModuleSetup).IsAssignableFrom(moduleType))
            {
                Debug.LogError($"O tipo {moduleType.Name} não implementa IModuleSetup.");
                return;
            }

            m_moduleRegistry[name] = moduleType;
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