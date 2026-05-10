using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>最终Boss：墨魔王。三阶段，含近战冲刺和远程墨弹两种攻击模式。</summary>
    public class BossDemonKing : MonoBehaviour, IDamageable
    {
        [Header("基础属性")]
        public float maxHp = 500f;
        public float moveSpeed = 2.8f;
        public int xpReward = 200;
        public EquipmentData colorGearDrop;
        public Transform player;
        public System.Action OnDefeated;

        [Header("近战 — 墨爪冲刺")]
        public float meleeRange = 3.2f;
        public float meleeDamage = 30f;
        public float meleeWindupTime = 0.55f;
        [Tooltip("冲刺速度")]
        public float dashSpeed = 18f;
        [Tooltip("冲刺持续时间（秒）")]
        public float dashDuration = 0.18f;

        [Header("远程 — 墨弹")]
        public float rangedDamage = 20f;
        public float projectileSpeed = 9f;
        public float rangedWindupTime = 0.7f;
        [Tooltip("超过此距离时优先选择远程攻击")]
        public float rangedPreferDist = 5f;

        [Header("击退")]
        public float knockDecayPerSec = 14f;
        public float bossKnockbackMul = 0.42f;

        // ── 运行时状态 ──
        float _hp;
        int _phase;          // 0 / 1 / 2
        float _t;
        bool _fxShown;
        float _dashT;
        float _dashDirX;

        SpriteRenderer _sr;
        WorldHealthBar _bar;
        float _knockVelX;

        enum State { Chase, WindupMelee, Dash, WindupRanged, Shoot, Recover }
        State _state = State.Chase;

        // 每阶段攻击间隔
        float AttackInterval => _phase == 0 ? 2.6f : _phase == 1 ? 1.9f : 1.35f;
        float RecoverTime    => _phase == 0 ? 0.85f : _phase == 1 ? 0.6f : 0.4f;

        void Start()
        {
            _hp = maxHp;
            _sr = GetComponent<SpriteRenderer>();
            _bar = WorldHealthBar.AddTo(transform, maxHp, 1.6f);
            ResolvePlayer();
        }

        void ResolvePlayer()
        {
            if (player != null) return;
            if (PlayerStats.Active != null) { player = PlayerStats.Active.transform; return; }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // ── 受击 ──
        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            _hp -= amount;
            _bar?.SetHp(_hp);
            if (knockbackImpulseX > 0f)
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker)
                              * knockbackImpulseX * bossKnockbackMul;
            if (_sr) { _sr.color = Color.white; Invoke(nameof(ResetColor), 0.08f); }

            if (_hp <= maxHp * 0.6f && _phase == 0) EnterPhase(1);
            if (_hp <= maxHp * 0.3f && _phase == 1) EnterPhase(2);
            if (_hp <= 0f) Die(attacker);
        }

        void EnterPhase(int p)
        {
            _phase = p;
            // 进阶段时闪白提示
            if (_sr) { _sr.color = Color.white; Invoke(nameof(ResetColor), 0.25f); }
        }

        void ResetColor()
        {
            if (_sr) _sr.color = new Color(0.06f, 0.04f, 0.10f);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);
            if (colorGearDrop != null)
                Pickup.SpawnAt(transform.position + Vector3.up * 0.9f, colorGearDrop);
            OnDefeated?.Invoke();
            Destroy(gameObject);
        }

        // ── 主循环 ──
        void Update()
        {
            float dt = Time.deltaTime;

            // 击退衰减
            if (Mathf.Abs(_knockVelX) > 0.02f)
            {
                transform.position += new Vector3(_knockVelX * dt, 0f, 0f);
                _knockVelX = Mathf.MoveTowards(_knockVelX, 0f, knockDecayPerSec * dt);
            }

            if (player == null) ResolvePlayer();
            if (player == null) return;

            _t += dt;

            switch (_state)
            {
                case State.Chase:       TickChase(dt);       break;
                case State.WindupMelee: TickWindupMelee();   break;
                case State.Dash:        TickDash(dt);        break;
                case State.WindupRanged:TickWindupRanged();  break;
                case State.Recover:     TickRecover();       break;
            }
        }

        // ── 追击阶段：决定下一个攻击 ──
        void TickChase(float dt)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.2f)
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * dt, 0f, 0f);

            if (_t < AttackInterval) return;
            _t = 0f;
            _fxShown = false;

            float dist = Mathf.Abs(dx);
            // Phase2 有40%概率强制远程，其余按距离判断
            bool forceRanged = _phase >= 2 && Random.value < 0.4f;
            if (dist >= rangedPreferDist || forceRanged)
                _state = State.WindupRanged;
            else
                _state = State.WindupMelee;
        }

        // ── 近战前摇 ──
        void TickWindupMelee()
        {
            if (!_fxShown)
            {
                _fxShown = true;
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                // 显示冲刺路径预警条
                GameRuntimeBootstrap.ShowAttackSlashFx(
                    transform.position, dir, meleeRange,
                    meleeWindupTime,
                    new Color(0.92f, 0.12f, 0.05f, 0.72f),
                    slashHeight: 1.1f);
            }
            if (_t >= meleeWindupTime)
            {
                _t = 0f;
                _dashDirX = Mathf.Sign(player.position.x - transform.position.x);
                _dashT = dashDuration;
                _state = State.Dash;
            }
        }

        // ── 冲刺（近战命中判定在冲刺过程中持续检测）──
        void TickDash(float dt)
        {
            _dashT -= dt;
            transform.position += new Vector3(_dashDirX * dashSpeed * dt, 0f, 0f);

            // 冲刺期间检测玩家是否在身旁
            if (player != null)
            {
                float dist = Vector2.Distance(player.position, transform.position);
                if (dist <= meleeRange * 0.6f)
                {
                    var stats = PlayerStats.Active ?? player.GetComponent<PlayerStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(meleeDamage * (_phase == 2 ? 1.3f : 1f), gameObject);
                        // 只打一次，立即结束冲刺
                        _dashT = 0f;
                    }
                }
            }

            if (_dashT <= 0f)
            {
                _t = 0f;
                _state = State.Recover;
            }
        }

        // ── 远程前摇 ──
        void TickWindupRanged()
        {
            if (!_fxShown)
            {
                _fxShown = true;
                float dir = Mathf.Sign(player.position.x - transform.position.x);
                // 紫色瞄准线，表示远程攻击
                GameRuntimeBootstrap.ShowAttackSlashFx(
                    transform.position, dir, 14f,
                    rangedWindupTime,
                    new Color(0.35f, 0.05f, 0.85f, 0.55f),
                    slashHeight: 0.38f);
            }
            if (_t >= rangedWindupTime)
            {
                _t = 0f;
                ExecuteShoot();
                _state = State.Recover;
            }
        }

        // ── 发射墨弹 ──
        void ExecuteShoot()
        {
            if (player == null) return;
            float dirX = Mathf.Sign(player.position.x - transform.position.x);

            if (_phase == 0)
            {
                // Phase 1：一发直线墨弹
                SpawnBolt(transform.position, new Vector2(dirX, 0f));
            }
            else if (_phase == 1)
            {
                // Phase 2：两发（直线 + 斜上）
                SpawnBolt(transform.position, new Vector2(dirX, 0f));
                SpawnBolt(transform.position, new Vector2(dirX * 0.85f, 0.52f).normalized);
            }
            else
            {
                // Phase 3：三发扇形（斜上、直线、斜下）
                SpawnBolt(transform.position, new Vector2(dirX * 0.85f,  0.52f).normalized);
                SpawnBolt(transform.position, new Vector2(dirX,          0f));
                SpawnBolt(transform.position, new Vector2(dirX * 0.85f, -0.52f).normalized);
            }
        }

        void SpawnBolt(Vector2 origin, Vector2 direction)
        {
            var go = new GameObject("InkBolt");
            go.transform.position = (Vector3)origin + new Vector3(direction.x * 0.7f, 0.1f, 0f);
            go.transform.localScale = new Vector3(0.55f, 0.22f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.22f, 0.04f, 0.48f, 0.92f);
            sr.sortingOrder = 7;

            // 弹体需要 Rigidbody2D 才能触发 OnTriggerEnter2D
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity = direction * projectileSpeed;
            bolt.damage   = rangedDamage * (_phase == 2 ? 1.25f : 1f);
        }

        // ── 恢复阶段 ──
        void TickRecover()
        {
            if (_t >= RecoverTime)
            {
                _t = 0f;
                _fxShown = false;
                _state = State.Chase;
            }
        }
    }

    // ── 墨弹飞行体（支持追踪） ──
    public class InkBolt : MonoBehaviour
    {
        public Vector2 velocity;
        public float damage;
        public bool isHoming;
        public float homingDuration  = 1f;
        public float homingTurnSpeed = 150f; // 每秒最大转向角度

        float _life = 6f;
        float _homingT;
        bool _hit;
        Transform _target;

        void Start()
        {
            if (!isHoming) return;
            if (PlayerStats.Active != null) { _target = PlayerStats.Active.transform; return; }
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) _target = p.transform;
        }

        void Update()
        {
            // 追踪阶段：旋转速度向量朝向玩家
            if (isHoming && _homingT < homingDuration && _target != null)
            {
                _homingT += Time.deltaTime;
                Vector2 toTarget = ((Vector2)_target.position - (Vector2)transform.position).normalized;
                float angle   = Vector2.SignedAngle(velocity, toTarget);
                float maxTurn = homingTurnSpeed * Time.deltaTime;
                velocity = Rotate(velocity, Mathf.Clamp(angle, -maxTurn, maxTurn));
            }

            transform.position += (Vector3)(velocity * Time.deltaTime);
            _life -= Time.deltaTime;
            if (_life <= 0f) Destroy(gameObject);
        }

        public static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_hit) return;
            if (other.CompareTag("Ground")) { Destroy(gameObject); return; }
            var stats = other.GetComponent<PlayerStats>()
                     ?? other.GetComponentInParent<PlayerStats>();
            if (stats == null) return;
            _hit = true;
            stats.TakeDamage(damage, gameObject);
            Destroy(gameObject);
        }
    }
}
