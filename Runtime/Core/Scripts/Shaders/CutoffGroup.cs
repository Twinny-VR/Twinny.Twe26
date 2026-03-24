using UnityEngine;

namespace Twinny.Shaders
{
    public class CutoffGroup : MonoBehaviour
    {
        public float offsetHeight = 0f;

        private void Awake() => Register();

        private void OnEnable() => Register();

        private void OnDestroy() => Unregister();

        private void Register() => AlphaClipper.Register(this);

        private void Unregister() => AlphaClipper.Unregister(this);
    }
}
