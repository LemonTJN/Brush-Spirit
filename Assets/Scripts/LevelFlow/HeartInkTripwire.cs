using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 墨丝绊索：奔跑/冲刺经过时短暂束缚水平速度，并向廊道中缝拽一小段——鼓励跳越节奏。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartInkTripwire : MonoBehaviour
    {
        [Tooltip("水平速度超过此值即视为「奔跑」触发绊索（略低于冲刺也可触发）。")]
        public float tripHorizontalSpeed = 4.2f;

        [Tooltip("绊索后水平速度乘数（越小越像被缚）。")]
        public float snareHorizontalFactor = 0.22f;

        [Tooltip("向世界 X=0 中缝拉拢的冲量（朝中心为正或负由位置决定）。")]
        public float pullTowardCenterImpulse = 3.6f;

        [Tooltip("绊索后最小上抛，避免完全贴地粘死。")]
        public float minKickUp = 1.85f;

        [Tooltip("同一玩家再次触发前的间隔（秒），避免 Stay 每帧反复缚足。")]
        public float retripCooldown = 0.55f;

        float _nextTripAllowed;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        void ApplyTrip(Rigidbody2D rb, Collider2D playerCol)
        {
            if (rb == null) return;
            if (Time.time < _nextTripAllowed) return;
            float vx = rb.velocity.x;
            float ax = Mathf.Abs(vx);
            var pm = playerCol.GetComponentInParent<PlayerMovement>() ?? playerCol.GetComponent<PlayerMovement>();
            bool dashing = pm != null && pm.IsDashing;
            if (ax < tripHorizontalSpeed && !dashing) return;

            float px = rb.position.x;
            float towardCenter = px > 0.05f ? -1f : (px < -0.05f ? 1f : 0f);
            if (Mathf.Abs(towardCenter) < 0.01f)
                towardCenter = -Mathf.Sign(vx);

            rb.velocity = new Vector2(
                vx * snareHorizontalFactor + towardCenter * pullTowardCenterImpulse,
                Mathf.Max(rb.velocity.y, minKickUp));
            _nextTripAllowed = Time.time + retripCooldown;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var rb = other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponent<Rigidbody2D>();
            ApplyTrip(rb, other);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var rb = other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponent<Rigidbody2D>();
            ApplyTrip(rb, other);
        }
    }
}
