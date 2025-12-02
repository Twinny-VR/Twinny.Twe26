using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{

    public enum AdviceType
    {
        INFO,
        ERROR,
        VALID,
        WARNING
    }


    [UxmlElement]
    public partial class AdvicePanel : VisualElement
    {
        private Label m_label;
        private VisualElement m_advicePanel;

        [UxmlAttribute]
        public string text
        {
            get => m_label?.text;
            set => m_label.text = value;
        }

        private AdviceType m_adviceType;
        [UxmlAttribute]
        public AdviceType adviceType 
        {
            get => m_adviceType;
            set
            {
                m_adviceType = value;
                foreach (AdviceType item in Enum.GetValues(typeof(AdviceType)))
                {
                    m_advicePanel.EnableInClassList(item.ToString().ToLowerInvariant(), adviceType == item);
                }
            }
        }

        public AdvicePanel()
        {
            var visualTree = Resources.Load<VisualTreeAsset>(GetType().Name);
            if (visualTree == null)
            {
                Debug.LogError(GetType().Name + " Resource not found!");
                return;
            }
            styleSheets.Add(Resources.Load<StyleSheet>(GetType().Name + "Styles"));

            visualTree.CloneTree(this);

            m_label = this.Q<Label>("AdviceLabel");
            m_advicePanel = this.Q<VisualElement>("UIAdvicePanel");
        }


        public void ShowAdvice(string text, AdviceType adviceType, string yesBtText = "Yes", Action yesCallback = null, string noBtText = "No", Action noCallback = null)
        {

            m_label.text = text;
            Button oldYesButton = this.Q<Button>("YesButton");
            if (oldYesButton != null)
                oldYesButton.RemoveFromHierarchy();

            if (yesCallback != null)
            {
                Button button = new Button();
                button.name = "YesButton";
                button.text = yesBtText;
                button.clicked += yesCallback;
                button.AddToClassList("button");
                m_advicePanel.Add(button);
            }

            Button oldNoButton = this.Q<Button>("NoButton");
            if (oldNoButton != null)
                oldNoButton.RemoveFromHierarchy();

            if (noCallback != null)
            {
                Button button = new Button();
                button.name = "NoButton";
                button.text = noBtText;
                button.clicked += noCallback;
                button.AddToClassList("button");
                m_advicePanel.Add(button);
            }

            this.adviceType = adviceType;

            /*
            foreach (AdviceType item in Enum.GetValues(typeof(AdviceType)))
            {
                m_advicePanel.EnableInClassList(item.ToString().ToLowerInvariant(), adviceType == item);
            }
            */
            style.display = DisplayStyle.Flex;
        }


    }
}
