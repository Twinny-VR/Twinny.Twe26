#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    public sealed class TwinnyToolbarButtonDefinition
    {
        public string Id { get; }
        public int SortOrder { get; }
        public Func<EditorToolbarButton> CreateButton { get; }

        public TwinnyToolbarButtonDefinition(string id, Func<EditorToolbarButton> createButton, int sortOrder = 0)
        {
            Id = id;
            CreateButton = createButton;
            SortOrder = sortOrder;
        }
    }

    [InitializeOnLoad]
    public static class TwinnyToolbar
    {
        private const string ToolbarIconAssetPath = "Packages/com.twinny.twe26/Editor/Icons/icons.png";
        private static readonly Dictionary<string, TwinnyToolbarButtonDefinition> s_ButtonRegistry = new();
        private static readonly Dictionary<string, Texture2D> s_IconCache = new();

        static TwinnyToolbar()
        {
            RegisterButton(new TwinnyToolbarButtonDefinition(
                "Twinny.Toolbar.OpenSetupGuide",
                () => new OpenSetupGuideToolbarButton(),
                sortOrder: 0));
        }

        public static void RegisterButton(TwinnyToolbarButtonDefinition definition)
        {
            if (definition == null || string.IsNullOrWhiteSpace(definition.Id) || definition.CreateButton == null)
                return;

            s_ButtonRegistry[definition.Id] = definition;
            TwinnyToolbarOverlay.RefreshAll();
        }

        public static void UnregisterButton(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return;

            if (s_ButtonRegistry.Remove(id))
                TwinnyToolbarOverlay.RefreshAll();
        }

        internal static IReadOnlyList<TwinnyToolbarButtonDefinition> GetButtons()
        {
            return s_ButtonRegistry.Values
                .OrderBy(button => button.SortOrder)
                .ThenBy(button => button.Id, StringComparer.Ordinal)
                .ToArray();
        }

        internal static Texture2D GetIconTexture(string spriteName)
        {
            if (string.IsNullOrWhiteSpace(spriteName))
                return null;

            if (s_IconCache.TryGetValue(spriteName, out Texture2D cachedIcon) && cachedIcon != null)
                return cachedIcon;

            Sprite iconSprite = AssetDatabase.LoadAllAssetsAtPath(ToolbarIconAssetPath)
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
                name = $"{sprite.name}_ToolbarIcon",
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
                UnityEngine.Object.DestroyImmediate(iconTexture);
                return null;
            }
        }
    }

    [Overlay(
        typeof(SceneView),
        "Twinny.Toolbar",
        "Twinny Toolbar",
        true,
        defaultDockZone = DockZone.RightColumn,
        defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 0,
        defaultLayout = Layout.VerticalToolbar)]
    internal sealed class TwinnyToolbarOverlay : Overlay, ICreateVerticalToolbar, ICreateHorizontalToolbar
    {
        private static readonly HashSet<TwinnyToolbarOverlay> s_Instances = new();
        private OverlayToolbar _verticalToolbar;
        private OverlayToolbar _horizontalToolbar;

        public TwinnyToolbarOverlay()
        {
            collapsedIcon = TwinnyToolbar.GetIconTexture("icons_0");
        }

        public override void OnCreated()
        {
            base.OnCreated();
            s_Instances.Add(this);
            RebuildToolbars();
        }

        public override void OnWillBeDestroyed()
        {
            s_Instances.Remove(this);
            base.OnWillBeDestroyed();
        }

        internal static void RefreshAll()
        {
            foreach (TwinnyToolbarOverlay instance in s_Instances)
                instance.RebuildToolbars();

            SceneView.RepaintAll();
        }

        public override VisualElement CreatePanelContent()
        {
            return CreateVerticalToolbarContent();
        }

        public OverlayToolbar CreateVerticalToolbarContent()
        {
            _verticalToolbar = new OverlayToolbar();
            PopulateToolbar(_verticalToolbar);
            return _verticalToolbar;
        }

        public OverlayToolbar CreateHorizontalToolbarContent()
        {
            _horizontalToolbar = new OverlayToolbar();
            PopulateToolbar(_horizontalToolbar);
            return _horizontalToolbar;
        }

        private void RebuildToolbars()
        {
            PopulateToolbar(_verticalToolbar);
            PopulateToolbar(_horizontalToolbar);
        }

        private static void PopulateToolbar(OverlayToolbar toolbar)
        {
            if (toolbar == null)
                return;

            toolbar.Clear();
            foreach (TwinnyToolbarButtonDefinition definition in TwinnyToolbar.GetButtons())
                toolbar.Add(definition.CreateButton());
        }
    }

    internal sealed class OpenSetupGuideToolbarButton : EditorToolbarButton
    {
        public OpenSetupGuideToolbarButton()
        {
            icon = TwinnyToolbar.GetIconTexture("icons_0");
            tooltip = "Open Setup Guide";
            clicked += SetupGuideWindow.Open;
        }
    }
}
#endif
