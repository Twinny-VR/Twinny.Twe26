using UnityEngine;

namespace Twinny.Shaders
{

    public class AlphaClipper : MonoBehaviour
    {

        private static int s_activeInstanceCount;

        public static bool HasActiveInstance => s_activeInstanceCount > 0;

        private void OnEnable() => s_activeInstanceCount++;
        private void OnDisable() => s_activeInstanceCount = Mathf.Max(0, s_activeInstanceCount - 1);
    }

}