using UnityEngine;

namespace Twinny.Navigation
{
    public class Landmark : MonoBehaviour
    {
        [SerializeField] private string landmarkGuid;
        public string landName;
        public Material skyBoxMaterial;
        [Range(0f, 360f)]
        public float hdriOffsetRotation;

        public string LandmarkGuid => landmarkGuid;
        public Vector3 position => transform.position;
        public Quaternion rotation => transform.rotation;

        private void Awake()
        {
            EnsureLandmarkGuid();
        }

        private void OnEnable()
        {
            LandmarkHub.Register(this);
        }

        private void OnDisable()
        {
            LandmarkHub.Unregister(this);
        }

        private void OnDestroy()
        {
            LandmarkHub.Unregister(this);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureLandmarkGuid();
#if UNITY_EDITOR
            EnsureUniqueEditorGuid();
#endif
        }
#endif

        internal void EnsureLandmarkGuid()
        {
            if (!string.IsNullOrWhiteSpace(landmarkGuid))
            {
                return;
            }

            landmarkGuid = System.Guid.NewGuid().ToString("N");
        }

#if UNITY_EDITOR
        private void EnsureUniqueEditorGuid()
        {
            if (string.IsNullOrWhiteSpace(landmarkGuid))
            {
                landmarkGuid = System.Guid.NewGuid().ToString("N");
            }

            Landmark[] loadedLandmarks = Resources.FindObjectsOfTypeAll<Landmark>();
            for (int i = 0; i < loadedLandmarks.Length; i++)
            {
                Landmark other = loadedLandmarks[i];
                if (other == null || other == this)
                {
                    continue;
                }

                if (UnityEditor.EditorUtility.IsPersistent(other) || UnityEditor.EditorUtility.IsPersistent(this))
                {
                    continue;
                }

                if (!string.Equals(other.landmarkGuid, landmarkGuid, System.StringComparison.Ordinal))
                {
                    continue;
                }

                landmarkGuid = System.Guid.NewGuid().ToString("N");
                UnityEditor.EditorUtility.SetDirty(this);
                break;
            }
        }
#endif
    }
}
