using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>斜堤：站立时略减水平目标速度（模拟坡上滞脚）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartRampTerrain : MonoBehaviour
    {
        [Range(0.55f, 1f)]
        public float horizontalMoveRetain = 0.82f;

        [Tooltip("沿坡面向下的加速度（世界单位/秒²）；站立时叠加出下滑感。")]
        public float downhillSlideAccel = 22f;

        [Tooltip("站在该斜坡上时额外向下的加速度（加快顺坡下落 vy）。")]
        public float extraDownwardWhileOnRamp = 20f;

        public Vector2 EvaluateSlideAcceleration()
        {
            if (downhillSlideAccel <= 0f) return Vector2.zero;
            Vector2 t = transform.right;
            if (t.sqrMagnitude < 1e-6f) return Vector2.zero;
            t.Normalize();
            Vector2 a = Vector2.Dot(t, Vector2.down) >= Vector2.Dot(-t, Vector2.down) ? t : -t;
            if (Vector2.Dot(a, Vector2.down) < 0.04f) return Vector2.zero;
            return a * downhillSlideAccel;
        }

        void Reset()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = false;
        }
    }
}
