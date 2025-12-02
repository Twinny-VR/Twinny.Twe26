using UnityEngine;
using UnityEngine.UIElements;

namespace Twinny.Editor
{
  //  [CreateAssetMenu(menuName = "Twinny/Setup Config",fileName ="SetupConfig")]
    public class SetupConfig : ScriptableObject
    {
        public VisualTreeAsset visualTreeAsset;
        public ModuleInfo[] modules;


    }
}
