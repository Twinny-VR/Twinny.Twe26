
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

        void StartExperience();
        Task<Scene> ChangeScene(int buildIndex, Action<float> onSceneLoading = null);
        Task<Scene> ChangeScene(string sceneName, Action<float> onSceneLoading = null);

        void NavigateTo(int landMarkIndex);
    }

}