using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>裂谷侧缘：持续高额伤害（勿跌入纸缝）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartPitDamageZone : MonoBehaviour
    {
        public float damagePerTick = 6f;
        public float tickInterval = 0.28f;

        float _t;

        void Awake()
        {
            var c = GetComponent<Collider2D>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            if (stats == null) return;
            _t += Time.deltaTime;
            if (_t < tickInterval) return;
            _t = 0f;
            stats.TakeDamage(damagePerTick, gameObject);
        }
    }
}
