using UnityEngine;

namespace Twinny.Core
{
public static class GameMode
{
        public static IGameMode currentMode { get; private set; }


        public static void ChangeState(IGameMode newState)
        {
            if (currentMode == newState) return;

            currentMode?.Exit();
            currentMode = newState;
            currentMode?.Enter();
        }
    }
}