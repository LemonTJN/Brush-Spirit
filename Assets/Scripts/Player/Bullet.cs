using System.Collections.Generic;
using BrushSpirit.Core;
using BrushSpirit.LevelFlow;
using UnityEngine;

namespace BrushSpirit.Player
{
    /// <summary>
    /// 玩家手枪发射的子弹：直线飞、撞到 Hurtbox 扣血、超时自销毁。
    /// 支持「巨弹模式」：穿透多个敌人 + 命中触发 AOE 爆开（参考 DMC5 Charge Shot / Doom BFG）。
    /// </summary>
    public class Bullet : MonoBehaviour
    {
        public float speed = 18f;
        public float lifetime = 1.2f;
        public float damage = 8f;
        public float knockbackImpulse = 4f;

        [Header("巨弹模式（默认 1=普通子弹）")]
        [Tooltip("最多可穿透多少个目标，1=首次命中即销毁")]
        public int maxPierces = 1;
        [Tooltip("命中（含撞墙）时触发的 AOE 半径，0=不触发")]
        public float aoeRadiusOnHit = 0f;
        [Tooltip("AOE 伤害相对主弹伤害的倍率（0.6 = 范围伤害是主弹的 60%）")]
        public float aoeDamageMul = 0.6f;
        [Tooltip("AOE 检测层。一般设成 PlayerCombat 的 hurtboxMask")]
        public LayerMask hurtboxMaskForAoe;
        [Tooltip("命中反馈强度：0=Light, 1=Medium, 2=Heavy")]
        public int hitFeedbackLevel = 0;
        [Tooltip("是否无视地形（巨弹打的就是穿山贯石）。普通子弹保持 false。")]
        public bool ignoreTerrain = false;

        Vector2 _dir = Vector2.right;
        GameObject _owner;
        float _aliveT;
        int _piercesUsed;
        HashSet<GameObject> _alreadyHit;

        public void Launch(Vector2 direction, GameObject owner, float dmg)
        {
            _dir = direction.normalized;
            if (_dir.sqrMagnitude < 0.01f) _dir = Vector2.right;
            _owner = owner;
            damage = dmg;
            transform.right = (Vector3)_dir;
        }

        void Update()
        {
            _aliveT += Time.deltaTime;
            if (_aliveT >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(_dir * speed * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;
            if (other.CompareTag("Player")) return;
            if (other.CompareTag("Ground"))
            {
                if (ignoreTerrain) return; // 巨弹直接穿过地形
                TriggerAoe();
                Destroy(gameObject);
                return;
            }
            var d = other.GetComponent<IDamageable>();
            if (d == null) d = other.GetComponentInParent<IDamageable>();
            if (d == null) return;

            // 穿透模式去重：同一目标只算一次主弹命中
            var dComp = d as Component;
            GameObject targetGO = dComp != null ? dComp.gameObject : other.gameObject;
            if (_alreadyHit != null && _alreadyHit.Contains(targetGO)) return;

            float dmgMul = HeartSceneEnvironment.GetOutgoingDamageMultiplierOnTarget(other.bounds.center);
            float focusMul = PlayerStats.OutgoingDamageMul(); // 完美闪避奖励
            d.TakeDamage(damage * dmgMul * focusMul, _owner, _dir.x * knockbackImpulse);
            PlayHitFeedback();

            TriggerAoe();

            if (maxPierces > 1)
            {
                if (_alreadyHit == null) _alreadyHit = new HashSet<GameObject>();
                _alreadyHit.Add(targetGO);
                _piercesUsed++;
                if (_piercesUsed >= maxPierces) Destroy(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>命中 / 撞墙时的范围爆开。aoeRadiusOnHit ≤ 0 时无操作。</summary>
        void TriggerAoe()
        {
            if (aoeRadiusOnHit <= 0f) return;
            float focusMul = PlayerStats.OutgoingDamageMul();
            var hits = Physics2D.OverlapCircleAll(transform.position, aoeRadiusOnHit, hurtboxMaskForAoe);
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.CompareTag("Player")) continue;
                if (_owner != null && h.transform.IsChildOf(_owner.transform)) continue;
                var d = h.GetComponent<IDamageable>() ?? h.GetComponentInParent<IDamageable>();
                if (d == null) continue;
                var dComp = d as Component;
                GameObject targetGO = dComp != null ? dComp.gameObject : h.gameObject;
                if (_alreadyHit != null && _alreadyHit.Contains(targetGO)) continue; // 主弹刚打过的不再叠 AOE
                float envMul = HeartSceneEnvironment.GetOutgoingDamageMultiplierOnTarget(h.bounds.center);
                d.TakeDamage(damage * aoeDamageMul * envMul * focusMul, _owner, _dir.x * knockbackImpulse * 0.5f);
            }
        }

        void PlayHitFeedback()
        {
            switch (hitFeedbackLevel)
            {
                case 2: HitFeedback.Heavy(); break;
                case 1: HitFeedback.Medium(); break;
                default: HitFeedback.Light(); break;
            }
        }
    }
}
