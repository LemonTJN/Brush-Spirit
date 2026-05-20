using System.Collections;
using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    public class SimpleEnemy : MonoBehaviour, IDamageable
    {
        public float maxHp = 35f;
        public float moveSpeed = 2.2f;
        [Tooltip("与玩家的平面距离小于此值则尝试近战（原仅用水平距 + 过小范围，玩家后退时永远打不到）")]
        public float attackRange = 2.65f;
        public float attackDamage = 4.5f;
        public float attackCooldown = 0.85f;
        public int xpReward = 12;
        public float whiteDropChance = 0.35f;

        public EquipmentData whiteDropA;
        public EquipmentData whiteDropB;
        public Transform player;

        [Tooltip("攻击前摇（变红膨胀的持续时长，结束后才出招扣血）")]
        public float attackWindupTime = 0.60f;

        float _hp;
        float _cd;
        bool _attackPending;
        float _windupT;
        WorldHealthBar _bar;
        float _knockVelX;
        Color _idleTint;

        [Tooltip("击退水平速度每秒衰减量")]
        public float knockDecayPerSec = 22f;

        [Header("平台小怪（由 PlatformEnemySpawn + SetPlatformPatrol 启用）")]
        [Tooltip("与玩家高度差小于此值且水平在台面上，视为「已上台」开始追击")]
        public float samePlatformMaxDy = 0.78f;

        [Tooltip("玩家中心 X 可超出台面 patrol 范围的最大容差")]
        public float onPlatformXSlack = 0.42f;

        const float PatrolEdgeSlack = 0.12f;

        bool _platformMode;
        float _platformMinX;
        float _platformMaxX;
        float _lockedY;
        float _patrolDir = 1f;

        /// <summary>将小怪限制在 [minX,maxX]，Y 锁定；未上台时巡逻，上台后追击（仍不离开台面）。</summary>
        public void SetPlatformPatrol(float minX, float maxX)
        {
            if (maxX <= minX) return;
            _platformMode = true;
            _platformMinX = minX;
            _platformMaxX = maxX;
            _lockedY = transform.position.y;
            _patrolDir = Random.value < 0.5f ? -1f : 1f;
        }

        void Start()
        {
            _hp = maxHp;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _idleTint = _sr.color;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 0.58f);
            ResolvePlayerTransform();
        }

        void ResolvePlayerTransform()
        {
            if (player != null) return;
            if (PlayerStats.Active != null)
            {
                player = PlayerStats.Active.transform;
                return;
            }

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            _hp -= amount;
            _bar?.SetHp(_hp);
            if (knockbackImpulseX > 0f)
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker) * knockbackImpulseX;
            Flash();
            if (_hp <= 0f)
                Die(attacker);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Mathf.Abs(_knockVelX) > 0.02f)
            {
                transform.position += new Vector3(_knockVelX * dt, 0f, 0f);
                _knockVelX = Mathf.MoveTowards(_knockVelX, 0f, knockDecayPerSec * dt);
            }

            if (_platformMode)
            {
                if (Mathf.Abs(_knockVelX) <= 0.02f)
                {
                    if (player == null) ResolvePlayerTransform();
                    if (player != null)
                        TickPlatformAi(dt);
                }

                ClampToPlatform();
                return;
            }

            if (player == null) ResolvePlayerTransform();
            if (player == null) return;

            // 一旦起手就必须完整完成攻击：不再因为玩家走出范围而取消
            if (_attackPending)
            {
                TickInRangeAttack(dt);
                return;
            }

            Vector2 delta = (Vector2)(player.position - transform.position);
            float dist = delta.magnitude;
            if (dist > attackRange)
            {
                float dx = delta.x;
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * dt, 0f, 0f);
                _cd = Mathf.Max(0f, _cd - dt);
            }
            else
            {
                TickInRangeAttack(dt);
            }
        }

        bool PlayerOnThisPlatform()
        {
            if (player == null) return false;
            Vector3 pp = player.position;
            if (Mathf.Abs(pp.y - transform.position.y) > samePlatformMaxDy) return false;
            return pp.x >= _platformMinX - onPlatformXSlack && pp.x <= _platformMaxX + onPlatformXSlack;
        }

        void TickPlatformAi(float dt)
        {
            Vector3 p = transform.position;

            // 攻击起手期间不移动、不巡逻，只推进 windup
            if (_attackPending)
            {
                TickInRangeAttack(dt);
                p.y = _lockedY;
                transform.position = p;
                return;
            }

            bool aggro = PlayerOnThisPlatform();
            if (aggro)
            {
                Vector2 delta = (Vector2)(player.position - p);
                float dist = delta.magnitude;
                if (dist > attackRange)
                {
                    float dx = delta.x;
                    p.x += Mathf.Sign(dx) * moveSpeed * dt;
                }
                else
                {
                    TickInRangeAttack(dt);
                }
            }
            else
            {
                _cd = Mathf.Max(0f, _cd - dt);
                p.x += _patrolDir * moveSpeed * dt;
                if (p.x >= _platformMaxX - PatrolEdgeSlack)
                {
                    p.x = _platformMaxX - PatrolEdgeSlack;
                    _patrolDir = -1f;
                }
                else if (p.x <= _platformMinX + PatrolEdgeSlack)
                {
                    p.x = _platformMinX + PatrolEdgeSlack;
                    _patrolDir = 1f;
                }
            }

            p.y = _lockedY;
            transform.position = p;
        }

        void ClampToPlatform()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, _platformMinX, _platformMaxX);
            p.y = _lockedY;
            transform.position = p;
        }

        void TickInRangeAttack(float dt)
        {
            if (_attackPending)
            {
                _windupT -= dt;
                if (_windupT <= 0f)
                {
                    _attackPending = false;
                    TryHitPlayer();
                    _cd = attackCooldown;
                    StartCoroutine(StrikeLungeAndClaw());
                }
            }
            else
            {
                _cd -= dt;
                if (_cd <= 0f)
                {
                    _attackPending = true;
                    _windupT = attackWindupTime;
                    // 起手时锁定挥击方向：之后玩家绕到背后也按这里的方向打
                    _attackDirX = player != null
                        ? Mathf.Sign(player.position.x - transform.position.x)
                        : 1f;
                    if (Mathf.Approximately(_attackDirX, 0f)) _attackDirX = 1f;
                    StartCoroutine(WindupTellRoutine());
                }
            }
        }

        /// <summary>
        /// 出招瞬间结算：只在「攻击锁定方向那一侧」+「挥击半径内」才扣血。
        ///   - 玩家绕到背后 → 不掉血（怪物空挥）
        ///   - 玩家在身前但跑出范围 → 不掉血
        /// 这就是主流动作游戏的「单向 hitbox」做法，参考 Dark Souls / Hollow Knight 的小怪挥击判定。
        /// </summary>
        void TryHitPlayer()
        {
            var stats = PlayerStats.Active != null ? PlayerStats.Active : player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats == null) stats = Object.FindObjectOfType<PlayerStats>();
            if (stats == null) return;

            Vector2 toPlayer = (Vector2)stats.transform.position - (Vector2)transform.position;

            // 单向判定：玩家必须在攻击锁定方向那一侧（允许 0.18 单位的「贴脸容差」，避免和怪重叠时判定抽搐）
            float forwardOffset = toPlayer.x * _attackDirX;
            if (forwardOffset < -0.18f) return;

            // 距离限制：lunge 前冲 0.45 单位，再加点宽容
            float hitReach = attackRange + 0.55f;
            if (toPlayer.magnitude > hitReach) return;

            stats.TakeDamage(attackDamage, gameObject);
        }

        // ── 攻击前摇视觉：身体逐渐变红 + 放大，无需动画 ──
        IEnumerator WindupTellRoutine()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            Color baseCol = _idleTint;
            Vector3 baseScale = transform.localScale;
            Vector3 maxScale = baseScale * 1.18f;
            Color targetTint = new Color(1f, 0.30f, 0.20f);
            float t = 0f;
            while (_attackPending && t < attackWindupTime)
            {
                t += Time.deltaTime;
                float u = Mathf.Clamp01(t / attackWindupTime);
                // 越接近出招颜色越红、体型越涨；u 平方让前段缓慢、后段急促
                float k = u * u;
                if (_sr != null) _sr.color = Color.Lerp(baseCol, targetTint, k * 0.85f);
                transform.localScale = Vector3.Lerp(baseScale, maxScale, k);
                yield return null;
            }
            // 出招或被打断后立即恢复
            if (_sr != null) _sr.color = baseCol;
            transform.localScale = baseScale;
        }

        // ── 出招视觉：用起手时锁定的方向突进 + 爪痕扇形 ──
        IEnumerator StrikeLungeAndClaw()
        {
            float dir = _attackDirX;
            SpawnClawArc(dir);

            // Lunge：先快速前冲 0.45 单位，再回弹
            Vector3 origin = transform.position;
            Vector3 forwardTarget = origin + new Vector3(dir * 0.45f, 0f, 0f);
            float t = 0f, dur = 0.08f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(origin, forwardTarget, t / dur);
                yield return null;
            }
            t = 0f; dur = 0.16f;
            Vector3 lungePeak = transform.position;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(lungePeak, origin, t / dur);
                yield return null;
            }
            transform.position = origin;
        }

        /// <summary>在攻击方向画三条扇形细爪痕，替代红色长方形 telegraph。</summary>
        void SpawnClawArc(float dir)
        {
            Vector3 center = transform.position + new Vector3(dir * attackRange * 0.55f, 0.05f, 0f);
            float[] angles = { -22f, 0f, 22f };
            foreach (float a in angles)
            {
                var go = new GameObject("EnemyClawMark");
                go.transform.position = center;
                float baseRotZ = dir < 0f ? 180f : 0f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, baseRotZ + a);
                go.transform.localScale = new Vector3(attackRange * 0.7f, 0.07f, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
                // 爪痕：白灰主体 + 微透明，附在出招位置一闪即逝
                sr.color = new Color(0.92f, 0.92f, 0.96f, 0.88f);
                sr.sortingOrder = 56;
                Object.Destroy(go, 0.16f);
            }
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);

            // 不再随机掉落白方块装备占位（占位贴图视觉不佳）；改由 WeaponDropDirector 统一掉落「剑 / 枪」形态拾取物。
            WeaponDropDirector.OnEnemyKilled(transform.position);

            Destroy(gameObject);
        }

        SpriteRenderer _sr;
        float _flashT;

        // 锁定起手方向，避免玩家中途绕到背后导致 lunge 朝反方向
        float _attackDirX = 1f;

        void Flash()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _sr.color = Color.white;
                _flashT = 0.08f;
            }
        }

        void LateUpdate()
        {
            if (_flashT > 0f)
            {
                _flashT -= Time.deltaTime;
                if (_flashT <= 0f && _sr != null)
                    _sr.color = _idleTint;
            }
        }
    }
}
