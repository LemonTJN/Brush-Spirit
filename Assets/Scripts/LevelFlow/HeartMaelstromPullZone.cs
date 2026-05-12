using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>墨涡：将玩家向锚点轻微牵引（加速度叠加在速度上）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartMaelstromPullZone : MonoBehaviour
    {
        public Transform pullTarget;
        public float acceleration = 14f;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
            if (pullTarget == null)
                pullTarget = transform;
        }

        public Vector2 GetAccelerationAt(Vector2 worldPos)
        {
            Vector2 center = pullTarget.position;
            Vector2 d = center - worldPos;
            float s = d.sqrMagnitude;
            if (s < 0.01f)
                return Vector2.zero;
            d.Normalize();
            return d * acceleration;
        }
    }
}
