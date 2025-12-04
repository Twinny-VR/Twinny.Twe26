using System;
using System.Collections;
using System.Threading.Tasks;
using Concept.Helpers;
using UnityEngine;

namespace Twinny.UI
{
    public class CanvasTransition : TSingleton<CanvasTransition>
    {
        private const string PREFAB_GUID = "m_prefab";

        [SerializeField] private Canvas m_overlayScreen;
        [SerializeField] private CanvasGroup m_fadeScreen;
        private Coroutine m_fadeCoroutine;
        public static bool isTransitioning { get; private set; }

        protected override void Start()
        {
            base.Start();
            if (m_overlayScreen == null) m_overlayScreen = GetComponent<Canvas>();
            m_overlayScreen.worldCamera = Camera.main;
            m_fadeScreen.alpha = 1.0f;
        }

        public static void EnsureInstance(bool hidden = false) {
            if(Instance != null) return;
            GameObject.Instantiate(Resources.Load<CanvasTransition>("CanvasTransition"));
        }

        public static void FadeScreen(bool fadeIn, float fadeTime, float delay = 0f, Action<bool> callback = null)
        {
            if (Instance == null) EnsureInstance();
            
            if (Instance.m_fadeCoroutine != null) Instance.StopCoroutine(Instance.m_fadeCoroutine);

            Instance.m_fadeCoroutine = Instance.StartCoroutine(Instance.FadeCoroutine(fadeIn, fadeTime, delay, callback));
        }

        /// <summary>
        /// Fade screen
        /// <para>Can be used between change scenes.</para>
        /// </summary>
        /// <param name="fadeIn">true to show, false to hide.</param>
        /// <param name="callback">bool return: Callback function (true for hided, false for showing)</param>
        public static async Task<bool> FadeScreenAsync(bool fadeIn, float fadeTime, float delay = default)
        {
            if (Instance == null) EnsureInstance();

            await Task.Delay((int)(delay * 1000));

            isTransitioning = true;
            float startAlpha = Instance.m_fadeScreen.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsedTime = 0f;
            //if(fadeTime == default) fadeTime = TwinnyManager.config.fadeTime;
            // Smooth fade progress
            while (elapsedTime < fadeTime)
            {
                Instance.m_fadeScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeTime);

                elapsedTime += Time.deltaTime;
                await Task.Yield();
            }

            isTransitioning = false;
            //Force final alpha result
            Instance.m_fadeScreen.alpha = targetAlpha;

            return true;

        }

        private IEnumerator FadeCoroutine(bool fadeIn, float fadeTime, float delay, Action<bool> callback)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            isTransitioning = true;

            float startAlpha = m_fadeScreen.alpha;
            float targetAlpha = fadeIn ? 1f : 0f;
            float elapsedTime = 0f;

            while (elapsedTime < fadeTime)
            {
                m_fadeScreen.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsedTime / fadeTime);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            m_fadeScreen.alpha = targetAlpha;
            isTransitioning = false;

            callback?.Invoke(true);
        }
    }
}
