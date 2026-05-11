using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 烬谷「余烬带」：玩家在触发区内持续受到小额伤害（绕开暗红裂纹的教学目标）。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EmberCinderZone : MonoBehaviour
    {
        public float damagePerTick = 2.6f;
        public float tickInterval = 0.38f;

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
    }
}
