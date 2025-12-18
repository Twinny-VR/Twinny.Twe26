
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

namespace Twinny.Core
{
    public interface IGameMode
    {

        // Called when entering the current state.
        void Enter();

        // Called when exiting the current state.
        void Exit();

        // Called once per frame while in the current state.
        void Update();

         Task StartExperience(int buildIndex,int landMarkIndex);
         Task StartExperience(string sceneName,int landMarkIndex);
         void RestartExperience();
        Task<Scene> ChangeScene(int buildIndex, int landMarkIndex = -1, Action<float> onSceneLoading = null);
        Task<Scene> ChangeScene(string sceneName, int landMarkIndex = -1, Action<float> onSceneLoading = null);

        void Quit();

        void NavigateTo(int landMarkIndex);
    }

}