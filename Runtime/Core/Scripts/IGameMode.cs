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
        void ChangeScene(string sceneName);
        void ChangeScene(int sceneBuildIndex);
        void NavigateTo(int landMarkIndex);
    }

}