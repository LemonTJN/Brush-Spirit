using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 烬谷「余烬带 / 烬口」：进入时瞬间灼伤并施加灼烧 debuff；停留时持续小额伤害。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EmberCinderZone : MonoBehaviour
    {
        [Header("接触")]
        public float touchDamage = 4f;

        [Header("停留（区域内）")]
        public float damagePerTick = 2.6f;
        public float tickInterval = 0.38f;

        [Header("灼烧 Debuff（离开后仍持续）")]
        public float debuffDuration = 3.2f;
        public float debuffDamagePerTick = 2.1f;
        public float debuffTickInterval = 0.45f;

        float _sinceTick;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            if (stats == null) return;

            if (touchDamage > 0f)
                stats.TakeDamage(touchDamage, gameObject);

            ApplyBurnDebuff(stats, this);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            if (stats == null) return;

            _sinceTick += Time.deltaTime;
            if (_sinceTick < tickInterval) return;
            _sinceTick = 0f;
            stats.TakeDamage(damagePerTick, gameObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                _sinceTick = 0f;
        }

        static void ApplyBurnDebuff(PlayerStats stats, EmberCinderZone zone)
        {
            var burn = stats.GetComponent<EmberCinderBurnDebuff>();
            if (burn == null)
                burn = stats.gameObject.AddComponent<EmberCinderBurnDebuff>();

            burn.Apply(zone.debuffDuration, zone.debuffDamagePerTick, zone.debuffTickInterval, zone.gameObject);
        }
    }
}
