using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 绘心「褪色域」：可选「菌斑」模式——核心区玩家高伤，边缘仅减速且敌人更耐打；或水平边缘伤害衰减。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartDesaturationZone : MonoBehaviour
    {
        [Tooltip("菌斑模式：水平方向内侧为核心区（高伤），外侧为边缘（玩家不伤血只减速；对敌人Outgoing伤害降低）。")]
        public bool useCoreEdgeBehavior;

        [Range(0.12f, 0.55f)]
        public float coreWidthFraction = 0.38f;

        [Tooltip("核心区对玩家的每 tick 伤害")]
        public float coreDamagePerTick = 3.6f;

        [Tooltip("非菌斑模式下的基础每 tick 伤害")]
        public float damagePerTick = 2.4f;

        public float tickInterval = 0.4f;

        [Range(0.35f, 1f)]
        public float edgePlayerHorizontalSlow = 0.66f;

        [Range(0.45f, 1f)]
        [Tooltip("菌斑核心区：水平移速倍率（仍吃伤；边缘用 edgePlayerHorizontalSlow）。")]
        public float corePlayerHorizontalSlow = 0.86f;

        [Range(0.35f, 1f)]
        [Tooltip("整块均匀雾（如池心）内水平移速；默认 1 不减速。")]
        public float uniformPlayerHorizontalSlow = 1f;

        [Range(0.2f, 0.95f)]
        public float edgeEnemyDamageTakenMultiplier = 0.48f;

        [Tooltip("为 true 时远离中心伤害衰减（与菌斑模式互斥时优先菌斑）。")]
        public bool useHorizontalFalloff;

        [Range(0.05f, 0.48f)]
        public float edgeBandFraction = 0.32f;

        [Range(0.15f, 1f)]
        public float edgeDamageMultiplier = 0.42f;

        float _sinceTick;

        Collider2D Col => _col != null ? _col : (_col = GetComponent<Collider2D>());
        Collider2D _col;

        /// <summary>供环境查询（移动减速、对敌承伤倍率）使用。</summary>
        public Collider2D ZoneCollider => Col;

        void Awake()
        {
            if (Col != null)
                Col.isTrigger = true;
        }

        /// <summary>归一化水平距离：0 为中心，1 为左右边缘（盒半宽）。</summary>
        public float NormalizedHorizontalDistance(Vector2 worldPos)
        {
            var b = Col.bounds;
            float halfW = Mathf.Max(0.04f, b.extents.x);
            return Mathf.Clamp01(Mathf.Abs(worldPos.x - b.center.x) / halfW);
        }

        /// <summary>玩家是否处于本区「核心」带内。</summary>
        public bool IsPlayerInCore(Vector2 worldPos)
        {
            if (!useCoreEdgeBehavior) return true;
            return NormalizedHorizontalDistance(worldPos) <= coreWidthFraction;
        }

        /// <summary>对位于 worldPos 的敌人：普攻/子弹等最终伤害倍率（菌斑边缘更耐打；水平衰减区同步衰减）。</summary>
        public float EvaluateOutgoingDamageMultiplierAt(Vector2 worldPos)
        {
            if (Col == null) return 1f;
            if (!Col.OverlapPoint(worldPos)) return 1f;
            if (useCoreEdgeBehavior)
            {
                if (NormalizedHorizontalDistance(worldPos) <= coreWidthFraction) return 1f;
                return edgeEnemyDamageTakenMultiplier;
            }

            if (useHorizontalFalloff) return FalloffDamageMul(worldPos);
            return 1f;
        }

        float FalloffDamageMul(Vector2 worldPos)
        {
            if (!useHorizontalFalloff || useCoreEdgeBehavior) return 1f;
            float nx = NormalizedHorizontalDistance(worldPos);
            float inner = 1f - edgeBandFraction;
            if (nx <= inner) return 1f;
            float u = (nx - inner) / Mathf.Max(0.05f, edgeBandFraction);
            return Mathf.Lerp(1f, edgeDamageMultiplier, Mathf.Clamp01(u));
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player")) return;
            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            if (stats == null) return;

            Vector2 p = other.transform.position;

            if (useCoreEdgeBehavior)
            {
                if (!IsPlayerInCore(p))
                {
                    _sinceTick = 0f;
                    return;
                }

                _sinceTick += Time.deltaTime;
                if (_sinceTick < tickInterval) return;
                _sinceTick = 0f;
                stats.TakeDamage(coreDamagePerTick, gameObject);
                return;
            }

            _sinceTick += Time.deltaTime;
            if (_sinceTick < tickInterval) return;
            _sinceTick = 0f;
            float dmg = damagePerTick * FalloffDamageMul(p);
            stats.TakeDamage(dmg, gameObject);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
                _sinceTick = 0f;
        }
    }
}
