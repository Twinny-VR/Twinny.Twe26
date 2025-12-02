using System;
using Concept.Core;
using Twinny.Core;
using UnityEngine;
using UnityEngine.Events;


namespace Twinny.UI
{
    public interface IUICallBacks
    {
       void OnHudStatusChanged(bool status);
        #region Experience Callbacks

        void OnPlatformInitialize();
        void OnExperienceReady();
        void OnExperienceFinished(bool isRunning);
        void OnLoadExtensionMenu(GameObject menu, bool isStatic = false);
        void OnStartLoadScene();
        void OnLoadScene();
        void OnLoadSceneFeature();
        void OnUnloadSceneFeature();
        void OnExperienceStarting();
        void OnExperienceStarted();

        #endregion

        void OnSwitchManager(int source);

        #region Camera Callbacks

        void OnStandby(bool status);

        void OnCameraChanged(Transform camera, string type);
        void OnCameraLocked(Transform target);
        #endregion

    }

    [Serializable] public class OnCameraChangedEvent : UnityEvent { }
    [Serializable] public class OnLoadScene : UnityEvent { }

    [Serializable]
    public class UICallBackEvents : IUICallBacks
    {
        public OnCameraChangedEvent onCameraChanged;
        public OnLoadScene onLoadScene;


        public UICallBackEvents()
        {
            CallbackHub.RegisterCallback<IUICallBacks>(this);
        }

        public void Unregister()
        {
            CallbackHub.UnregisterCallback<IUICallBacks>(this);
        }

        public void OnCameraChanged(Transform camera, string type)
        {
            onCameraChanged?.Invoke();
        }

        public void OnCameraLocked(Transform target)
        {
        }

        public void OnExperienceFinished(bool isRunning)
        {
        }

        public void OnExperienceReady()
        {
        }

        public void OnExperienceStarted()
        {
        }

        public void OnExperienceStarting()
        {
        }

        public void OnHudStatusChanged(bool status)
        {
        }

        public void OnLoadExtensionMenu(GameObject menu, bool isStatic)
        {
        }

        public void OnLoadScene()
        {
            onLoadScene?.Invoke();
        }

        public void OnLoadSceneFeature()
        {
        }

        public void OnPlatformInitialize()
        {
        }

        public void OnStandby(bool status)
        {
        }

        public void OnStartLoadScene()
        {
        }

        public void OnSwitchManager(int source)
        {
        }

        public void OnUnloadSceneFeature()
        {
        }

        public void OnOrientationChanged(ScreenOrientation orientation)
        {
        }

    }
}
