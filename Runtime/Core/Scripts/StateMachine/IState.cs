namespace Twinny.Core
{

public interface IState
{

    // Called when entering the current state.
    void Enter();

    // Called when exiting the current state.
    void Exit();

    // Called once per frame while in the current state.
    void Update();
}

}