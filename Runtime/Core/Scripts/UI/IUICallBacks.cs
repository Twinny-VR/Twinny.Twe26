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
      //  void OnLoadExtensionMenu(GameObject menu, bool isStatic = false);
        #endregion

     //   void OnSwitchManager(int source); //Todo Send  to NetworkCallbacks


    }

    /*

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
    */
}
