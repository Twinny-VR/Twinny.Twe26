using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
    public interface IModuleSetup
    {

        void OnShowSection(SetupGuideWindow guideWindow, int tabIndex = 0);

        void OnApply();
    }

}