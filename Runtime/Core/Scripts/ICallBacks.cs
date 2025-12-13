using System;
using Concept.Core;
using Twinny.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


namespace Twinny.Core
{
    public interface ICallbacks
    {
        #region Experience Callbacks
        void OnPlatformInitializing();
        void OnPlatformInitialized();
        void OnExperienceReady();
        void OnExperienceStarting();
        void OnExperienceStarted();
        void OnExperienceEnding();
        void OnExperienceEnded(bool isRunning);
        void OnSceneLoadStart(string sceneName);
        void OnSceneLoaded(Scene scene);
        void OnTeleportToLandMark(int landMarkIndex);
        #endregion
    }

/*
    [Serializable]
    public class CallBackEvents : ICallbacks
    {
        public OnLoadScene onLoadScene;

        public CallBackEvents()
        {
            CallbackHub.RegisterCallback<ICallbacks>(this);
        }
        public void OnPlatformInitializing() { }
        public void OnPlatformInitialized() { }

        public void Unregister()
        {
            CallbackHub.UnregisterCallback<ICallbacks>(this);
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
        public void OnLoadScene()
        {
            onLoadScene?.Invoke();
        }

        public void OnLoadSceneFeature()
        {
        }


        public void OnStandby(bool status)
        {
        }

        public void OnSceneLoadStart()
        {
        }

        public void OnSwitchManager(int source)
        {
        }

        public void OnUnloadSceneFeature()
        {
        }

    }
*/
}
