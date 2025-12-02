using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{

    [Serializable, UxmlElement("SetupSidebarElement")]

    public partial class SetupSidebarElement : VisualElement
    {
        public VisualElement thumbnail;
        [UxmlAttribute("thumbnail")]
        public Sprite thumbnailIcon
        {
            get => thumbnail.style.backgroundImage.value.sprite;
            set
            {
                thumbnail.style.backgroundImage = new StyleBackground(value);
            }
        }


        public Label descriptionLabel;
        public SetupSidebarElement()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("SetupSidebarElement");
            if (visualTree == null)
            {
                Debug.LogError("SetupSidebarElement não encontrado em Resources!");
                return;
            }
            visualTree.CloneTree(this);

            AddToClassList("sidebar-button");
            descriptionLabel = this.Q<Label>("label");
            thumbnail =  this.Q<Image>("icon");
        }
    }
}