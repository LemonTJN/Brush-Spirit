using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>钟摆刃口触发伤害（挂在摆臂末端子物体上）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartPendulumBladeHit : MonoBehaviour
    {
        public float damagePerHit = 5.5f;
        public float hitCooldown = 0.45f;

        float _hitCd;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        void Update()
        {
            if (_hitCd > 0f) _hitCd -= Time.deltaTime;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player") || _hitCd > 0f) return;
            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            if (stats == null) return;
            stats.TakeDamage(damagePerHit, gameObject);
            _hitCd = hitCooldown;
            var rb = other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 d = (Vector2)other.transform.position - (Vector2)transform.position;
                if (d.sqrMagnitude < 0.0001f) d = Vector2.right;
                d.Normalize();
                rb.velocity += d * 4.5f + Vector2.up * 2f;
            }
        }
    }
}
