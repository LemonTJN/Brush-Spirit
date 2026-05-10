using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    public class BossInkTree : MonoBehaviour, IDamageable
    {
        public float maxHp = 420f;
        public float moveSpeed = 2.8f;

        [Header("近战 — 墨爪冲刺")]
        public float slamRadius = 2.2f;
        public float slamDamage = 24f;
        public float meleeWindupTime = 0.28f;
        public float dashSpeed = 25f;
        public float dashDuration = 1.0f;   // 25×1.0 ≈ 25格，覆盖整屏

        [Header("远程 — 墨弹（追踪）")]
        public float boltDamage = 16f;
        public float boltSpeed = 9f;
        public float rangedWindupTime = 0.35f;
        public float boltHomingDuration  = 1f;
        public float boltHomingTurnSpeed = 150f;

        public int xpReward = 80;
        public EquipmentData colorGearDrop;
        public Transform player;
        public System.Action OnDefeated;

        float _hp;
        float _t;
        int _phase;
        bool _windupFxShown;
        bool _isRangedAttack;   // 当前轮选的攻击类型
        float _dashT;
        float _dashDirX;
        bool _dashHit;

        enum BossState { Chase, Windup, Attack, Recover }
        BossState _state = BossState.Chase;

        SpriteRenderer _sr;
        WorldHealthBar _bar;
        float _knockVelX;

        public float knockDecayPerSec = 16f;
        public float bossKnockbackMul = 0.55f;

        float AttackInterval => _phase == 0 ? 2.4f : _phase == 1 ? 1.85f : 1.45f;
        float RecoverTime    => _phase == 0 ? 0.75f : _phase == 1 ? 0.55f : 0.38f;

        void Start()
        {
            _hp = maxHp;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 1.35f);
            _sr = GetComponent<SpriteRenderer>();
            ResolvePlayerTransform();
        }

        void ResolvePlayerTransform()
        {
            if (player != null) return;
            if (PlayerStats.Active != null) { player = PlayerStats.Active.transform; return; }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            _hp -= amount;
            _bar?.SetHp(_hp);
            if (knockbackImpulseX > 0f)
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker)
                              * knockbackImpulseX * bossKnockbackMul;
            if (_sr != null) { _sr.color = Color.white; Invoke(nameof(ResetColor), 0.08f); }

            if (_hp <= maxHp * 0.55f) _phase = 1;
            if (_hp <= maxHp * 0.28f) _phase = 2;
            if (_hp <= 0f) Die(attacker);
        }

        void ResetColor()
        {
            if (_sr != null) _sr.color = new Color(0.1f, 0.1f, 0.12f);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);
            if (colorGearDrop != null)
                Pickup.SpawnAt(transform.position + Vector3.up * 0.6f, colorGearDrop);
            OnDefeated?.Invoke();
            Destroy(gameObject);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Mathf.Abs(_knockVelX) > 0.02f)
            {
                transform.position += new Vector3(_knockVelX * dt, 0f, 0f);
                _knockVelX = Mathf.MoveTowards(_knockVelX, 0f, knockDecayPerSec * dt);
            }

            if (player == null) ResolvePlayerTransform();
            if (player == null) return;
            _t += dt;

            switch (_state)
            {
                case BossState.Chase:   TickChase(dt);   break;
                case BossState.Windup:  TickWindup();    break;
                case BossState.Attack:  TickAttack(dt);  break;
                case BossState.Recover: TickRecover();   break;
            }
        }

        // ── 追击：到时间后随机选攻击类型 ──
        void TickChase(float dt)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * dt, 0f, 0f);

            if (_t < AttackInterval) return;
            _t = 0f;
            _windupFxShown = false;

            float dist = Mathf.Abs(dx);
            // 距离远 or Phase2 随机 → 远程；否则随机各50%
            if (dist > 5f || (_phase >= 2 && Random.value < 0.4f))
                _isRangedAttack = true;
            else
                _isRangedAttack = Random.value < 0.5f;

            _state = BossState.Windup;
        }

        // ── 前摇：显示预警条 ──
        void TickWindup()
        {
            if (!_windupFxShown)
            {
                _windupFxShown = true;
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                if (_isRangedAttack)
                {
                    // 紫色细线 = 远程
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, 13f,
                        rangedWindupTime,
                        new Color(0.4f, 0.05f, 0.9f, 0.55f),
                        slashHeight: 0.35f);
                }
                else
                {
                    // 红色宽条 = 近战冲刺
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, slamRadius,
                        meleeWindupTime,
                        new Color(0.95f, 0.18f, 0.08f, 0.68f),
                        slashHeight: 1.1f);
                    _dashDirX = dir;
                    _dashT    = dashDuration;
                    _dashHit  = false;
                }
            }

            float windupDone = _isRangedAttack ? rangedWindupTime : meleeWindupTime;
            if (_t >= windupDone)
            {
                _t = 0f;
                if (_isRangedAttack)
                    ExecuteShoot();
                // 近战进入 Attack 状态（冲刺）
                _state = _isRangedAttack ? BossState.Recover : BossState.Attack;
            }
        }

        // ── 冲刺（近战）──
        void TickAttack(float dt)
        {
            _dashT -= dt;
            transform.position += new Vector3(_dashDirX * dashSpeed * dt, 0f, 0f);

            if (!_dashHit && player != null)
            {
                float dist = Vector2.Distance(player.position, transform.position);
                if (dist <= slamRadius * 0.65f)
                {
                    _dashHit = true;
                    var stats = PlayerStats.Active ?? player.GetComponent<PlayerStats>();
                    stats?.TakeDamage(slamDamage, gameObject);
                    _dashT = 0f;
                }
            }

            if (_dashT <= 0f)
            {
                _t = 0f;
                _state = BossState.Recover;
            }
        }

        // ── 发射 6 发扇形追踪墨弹 ──
        void ExecuteShoot()
        {
            if (player == null) return;
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            Vector2 baseDir = new Vector2(dirX, 0f);

            // 6发：以玩家方向为轴，±10° / ±30° / ±50° 散开
            float[] angles = { -50f, -30f, -10f, 10f, 30f, 50f };
            foreach (float a in angles)
            {
                Vector2 vel = InkBolt.Rotate(baseDir * boltSpeed, a);
                SpawnBolt(transform.position, vel);
            }
        }

        void SpawnBolt(Vector2 origin, Vector2 vel)
        {
            var go = new GameObject("InkBolt");
            go.transform.position = (Vector3)origin + new Vector3(vel.x > 0 ? 0.7f : -0.7f, 0.1f, 0f);
            go.transform.localScale = new Vector3(0.5f, 0.2f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color  = new Color(0.22f, 0.04f, 0.48f, 0.92f);
            sr.sortingOrder = 7;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity         = vel;
            bolt.damage           = boltDamage;
            bolt.isHoming         = true;
            bolt.homingDuration   = boltHomingDuration;
            bolt.homingTurnSpeed  = boltHomingTurnSpeed;
        }

        // ── 恢复 ──
        void TickRecover()
        {
            if (_t >= RecoverTime)
            {
                _t = 0f;
                _windupFxShown = false;
                _state = BossState.Chase;
            }
        }
    }
}
