using UnityEngine;

namespace Twinny.Core.Input
{

    public interface IInputCallbacks
    {
        void OnPrimaryDown(float x, float y);
        void OnPrimaryUp(float x, float y);
        void OnPrimaryDrag(float dx, float dy);
        void OnSelect(GameObject target);
        void OnCancel();
        void OnZoom(float delta);
    }

}