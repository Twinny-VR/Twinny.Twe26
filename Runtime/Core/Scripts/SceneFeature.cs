using System;
using System.Threading.Tasks;
using Concept.Helpers;
using UnityEngine;

namespace Twinny.Core
{
    /// <summary>
    /// Scene layout type VR(Virtual), MR(Mixed), MOBILE(Mobile)
    /// </summary>
    [Serializable]
    public enum SceneType
    {
        VR,//Virtual Reallity
        MR,//Mixed Reallity
        MOBILE //Mobile
    }

    public abstract class SceneFeature : TSingleton<SceneFeature>
    {
        [SerializeField] private Material _sceneSkyBox;
        public Material sceneSkybox { get => _sceneSkyBox; }
        [NonSerialized]
        public Transform[] interestPoints;

        public bool fadeOnAwake = true;

        protected override void Start()
        {
            base.Start();
            if(_sceneSkyBox != null)
            TwinnyManager.SetHDRI(_sceneSkyBox);
        }

        public virtual void TeleportToLandMark(int landMarkIndex) { }



    }
}
