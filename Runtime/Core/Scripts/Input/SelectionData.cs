using UnityEngine;

namespace Twinny.Core.Input
{
    /// <summary>
    /// Selection payload that can carry only a target or full raycast hit data.
    /// </summary>
    public readonly struct SelectionData
    {
        public GameObject Target { get; }
        public bool HasHit { get; }
        public RaycastHit Hit { get; }

        public Vector3 Point => HasHit ? Hit.point : (Target != null ? Target.transform.position : Vector3.zero);

        public SelectionData(GameObject target)
        {
            Target = target;
            Hit = default;
            HasHit = false;
        }

        public SelectionData(RaycastHit hit)
        {
            Target = hit.collider != null ? hit.collider.gameObject : null;
            Hit = hit;
            HasHit = true;
        }
    }
}
