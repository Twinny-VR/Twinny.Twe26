using Concept.Core;
using Concept.Helpers;
using UnityEngine;

namespace Twinny.Core.Input
{
public class InputRouter : TSingleton<InputRouter>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null) return;
        if (FindAnyObjectByType<InputRouter>() != null) return;

        var routerObject = new GameObject("InputRouter");
        routerObject.AddComponent<InputRouter>();
        DontDestroyOnLoad(routerObject);
    }

    [Header("Debug")]
    [SerializeField] private bool _logEvents;

    // === API PARA PROVIDERS ===

    public void PrimaryDown(float x, float y)
    {
        if (_logEvents) Debug.Log($"[Input] PrimaryDown {x}, {y}");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnPrimaryDown(x, y)
        );
    }

    public void PrimaryDrag(float dx, float dy)
    {
        if (_logEvents) Debug.Log($"[Input] PrimaryDrag {dx}, {dy}");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnPrimaryDrag(dx, dy)
        );
    }

    public void PrimaryUp(float x, float y)
    {
        if (_logEvents) Debug.Log($"[Input] PrimaryUp {x}, {y}");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnPrimaryUp(x, y)
        );
    }

    public void Select(GameObject target)
    {
        if (_logEvents) Debug.Log($"[Input] Select {target?.name}");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnSelect(target)
        );
    }

    public void Cancel()
    {
        if (_logEvents) Debug.Log("[Input] Cancel");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnCancel()
        );
    }

    public void Zoom(float delta)
    {
        if (_logEvents) Debug.Log($"[Input] Zoom {delta}");

        CallbackHub.CallAction<IInputCallbacks>(
            cb => cb.OnZoom(delta)
        );
    }

}

}
