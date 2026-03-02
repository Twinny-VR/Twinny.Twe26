namespace Twinny.Core.Input
{

    public interface IInputCallbacks
    {
        void OnPrimaryDown(float x, float y);
        void OnPrimaryUp(float x, float y);
        void OnPrimaryDrag(float dx, float dy);
        void OnSelect(SelectionData selection);
        void OnCancel();
        void OnZoom(float delta);
    }

}
