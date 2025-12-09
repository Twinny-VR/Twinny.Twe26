using System;
using UnityEngine.LowLevel;

namespace Twinny.Core
{


public static class StateMachine {

        public static IState state { get; private set; }

        private static readonly PlayerLoopSystem.UpdateFunction _updateFunction = FixedUpdate;


        static StateMachine()
        {
            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            for (int i = 0; i < playerLoop.subSystemList.Length; i++)
            {
                if (playerLoop.subSystemList[i].type == typeof(UnityEngine.PlayerLoop.Update))
                {
                    var updateSystem = playerLoop.subSystemList[i];
                    updateSystem.updateDelegate += _updateFunction;
                    playerLoop.subSystemList[i] = updateSystem;
                    break;
                }
            }
            PlayerLoop.SetPlayerLoop(playerLoop);
        }


        public static void ChangeState(IState newState)
        {
            if (state == newState) return;
            state?.Exit();
            state = newState;
            state?.Enter();
        }

        private static void FixedUpdate()
        {
            state?.Update();
        }


    }

}