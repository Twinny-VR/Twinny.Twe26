using Concept.Helpers;
using System.Collections;
using UnityEngine;

namespace Twinny.Shaders
{

    public class AlphaClipper : TSingleton<AlphaClipper>
    {
        private const string CutoffHeightPropertyName = "_CutoffHeight";

        [SerializeField] private Vector2 _minMaxWallHeight = new Vector2(0,3f);
        [SerializeField] private float _transitionDuration = 0.35f;
        [SerializeField] private CutoffGroup[] _cutoffGroups;

        public static Vector2 MinMaxWallHeight = new Vector2(0,3f);
        private Coroutine _cutoffTransitionRoutine;


        private void OnEnable()
        {
            MinMaxWallHeight = _minMaxWallHeight;
            UpdateCutoffGroupsVisibility(Shader.GetGlobalFloat(CutoffHeightPropertyName));
        }

        private void OnDisable() => MinMaxWallHeight = new Vector2(0, 3f);

        public static void SetCutoffHeight(float targetHeight, bool clampHeight = false)
        {
            float height = clampHeight ? ClampHeight(targetHeight) : targetHeight;
            Shader.SetGlobalFloat(CutoffHeightPropertyName, height);

            if (Instance)
                Instance.UpdateCutoffGroupsVisibility(height);
        }

        public static void TransitionCutoffHeight(float targetHeight)
        {
            if (!Instance)
            {
                SetCutoffHeight(targetHeight, true);
                return;
            }

            Instance.AnimateCutoffHeight(targetHeight);
        }

        private void AnimateCutoffHeight(float targetHeight)
        {
            float clampedTarget = ClampHeight(targetHeight);

            if (_cutoffTransitionRoutine != null)
                StopCoroutine(_cutoffTransitionRoutine);

            _cutoffTransitionRoutine = StartCoroutine(AnimateCutoffHeightRoutine(clampedTarget));
        }

        private IEnumerator AnimateCutoffHeightRoutine(float targetHeight)
        {
            float startHeight = Shader.GetGlobalFloat(CutoffHeightPropertyName);
            float duration = Mathf.Max(0.0001f, _transitionDuration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float height = Mathf.Lerp(startHeight, targetHeight, Mathf.SmoothStep(0f, 1f, t));
                SetCutoffHeight(height);
                yield return null;
            }

            SetCutoffHeight(targetHeight);
            _cutoffTransitionRoutine = null;
        }

        private static float ClampHeight(float height)
        {
            float minHeight = Mathf.Min(MinMaxWallHeight.x, MinMaxWallHeight.y);
            float maxHeight = Mathf.Max(MinMaxWallHeight.x, MinMaxWallHeight.y);
            return Mathf.Clamp(height, minHeight, maxHeight);
        }

        private void UpdateCutoffGroupsVisibility(float cutoffHeight)
        {
            if (_cutoffGroups == null || _cutoffGroups.Length == 0)
                return;

            for (int i = 0; i < _cutoffGroups.Length; i++)
            {
                CutoffGroup cutoffGroup = _cutoffGroups[i];
                if (!cutoffGroup)
                    continue;

                float groupHeight = cutoffGroup.transform.position.y + cutoffGroup.offsetHeight;
                bool shouldShow = cutoffHeight >= groupHeight;
                if (cutoffGroup.gameObject.activeSelf != shouldShow)
                    cutoffGroup.gameObject.SetActive(shouldShow);
            }
        }
    }

}
