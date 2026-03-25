using UnityEngine;

namespace Twinny.Shaders
{
    [ExecuteAlways]
    public class CutoffGroup : MonoBehaviour
    {
        public float offsetHeight = 0f;

        private void Awake() => Register();

        private void OnEnable() => Register();

        private void OnValidate() => Register();

        private void OnDestroy() => Unregister();

        private void Register() => AlphaClipper.Register(this);

        private void Unregister() => AlphaClipper.Unregister(this);

        public void SetChildrenVisible(bool isVisible)
        {
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child != null && child.gameObject.activeSelf != isVisible)
                    child.gameObject.SetActive(isVisible);
            }
        }
    }
}
