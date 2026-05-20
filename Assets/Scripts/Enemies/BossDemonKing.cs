using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>
    /// 最终 Boss：墨魔王。三阶段，五种攻击模式：
    ///   ① 墨爪冲刺 MeleeDash（贴身、高伤）
    ///   ② 墨弹齐射 RangedBolts（远程，阶段越高弹数越多）
    ///   ③ 墨柱地刺 GroundPillars（脚下伏击）
    ///   ④ 墨涌震波 SlamShockwave（跳起 → 双向地震波）
    ///   ⑤ 墨幕坠落 BulletCurtain（最终阶段专属）
    /// 强化参考：Hollow Knight、Dead Cells、Salt and Sanctuary 的 telegraph + 多模式 boss。
    /// </summary>
    public class BossDemonKing : MonoBehaviour, IDamageable
    {
        [Header("基础属性")]
        public float maxHp = 620f;
        public float moveSpeed = 3.0f;
        public int xpReward = 200;
        public EquipmentData colorGearDrop;
        public Transform player;
        public System.Action OnDefeated;

        [Header("近战 — 墨爪冲刺")]
        public float meleeRange = 3.4f;
        public float meleeDamage = 30f;
        public float meleeWindupTime = 0.75f;   // 延长前摇
        public float dashSpeed = 22f;
        public float dashDuration = 0.24f;

        [Header("远程 — 墨弹")]
        public float rangedDamage = 20f;
        public float projectileSpeed = 9f;
        public float rangedWindupTime = 0.85f;  // 延长瞄准窗口
        [Tooltip("超过此距离时优先选择远程攻击")]
        public float rangedPreferDist = 5f;

        [Header("墨柱地刺 GroundPillars")]
        public int pillarCount = 3;
        public float pillarSpacing = 2.4f;
        public float pillarWindupTime = 0.55f;
        public float pillarStrikeDuration = 0.45f;
        public float pillarDamage = 24f;
        public float pillarHalfWidth = 0.55f;
        public float pillarHeight = 2.6f;

        [Header("阶段切换无敌（红膜）")]
        public float phase1InvulnTime = 1.0f;
        public float phase2InvulnTime = 1.4f;

        [Header("墨涌震波 SlamShockwave")]
        public float slamWindupTime = 0.40f;
        public float slamApexHeight = 2.0f;
        public float slamAirTime = 0.55f;
        public float shockwaveSpeed = 8.0f;
        public float shockwaveDamage = 26f;
        public float shockwaveLife = 1.9f;

        [Header("墨幕坠落 BulletCurtain（终末期专属）")]
        public int curtainCount = 11;
        public float curtainSpacing = 1.05f;
        public float curtainHeight = 4.5f;
        public float curtainWindupTime = 0.65f;
        public float curtainFallSpeed = 7.0f;
        public float curtainDamage = 20f;

        [Header("击退")]
        public float knockDecayPerSec = 14f;
        public float bossKnockbackMul = 0.42f;

        // ── 运行时状态 ──
        enum AttackKind { MeleeDash, RangedBolts, GroundPillars, SlamShockwave, BulletCurtain }
        enum State { Chase, Windup, Attack, Recover }

        float _hp;
        int _phase;
        float _t;
        bool _fxShown;
        AttackKind _kind;
        State _state = State.Chase;

        float _dashT, _dashDirX;
        float _slamT;
        Vector3 _slamOrigin;
        float _baseY;
        float _invulnT;
        float[] _pillarTargetXs;

        SpriteRenderer _sr;
        WorldHealthBar _bar;
        float _knockVelX;

        float AttackInterval => _phase == 0 ? 2.5f : _phase == 1 ? 1.75f : 1.15f;
        float RecoverTime    => _phase == 0 ? 0.80f : _phase == 1 ? 0.55f : 0.32f;

        void Start()
        {
            _hp = maxHp;
            _sr = GetComponent<SpriteRenderer>();
            EnsureGroundFooting();
            _baseY = transform.position.y;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 1.85f);
            ResolvePlayer();
        }

        /// <summary>找最宽的 Ground 碰撞体（= 主地面，跳过平台/侧墙），把 boss 脚放到顶部。</summary>
        void EnsureGroundFooting()
        {
            if (_sr == null || _sr.sprite == null) return;
            float halfHeight = _sr.bounds.extents.y;

            var grounds = GameObject.FindGameObjectsWithTag("Ground");
            Collider2D best = null;
            float bestWidth = 0f;
            for (int i = 0; i < grounds.Length; i++)
            {
                var col = grounds[i].GetComponent<Collider2D>();
                if (col == null) continue;
                float w = col.bounds.size.x;
                if (w < 5f) continue;
                if (w > bestWidth) { bestWidth = w; best = col; }
            }
            if (best == null) return;
            Vector3 p = transform.position;
            p.y = best.bounds.max.y + halfHeight + 0.05f;
            transform.position = p;
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
            if (_invulnT > 0f)
            {
                if (_sr) _sr.color = new Color(1f, 0.55f, 0.55f);
                return;
            }

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
            _invulnT = p == 1 ? phase1InvulnTime : phase2InvulnTime;
            _t = 0f;
            _fxShown = false;
            _knockVelX = 0f;
            // 强制释放该阶段招牌技，逼迫玩家躲避：Phase1 → 震波，Phase2 → 弹幕雨
            _kind = p == 1 ? AttackKind.SlamShockwave : AttackKind.BulletCurtain;
            _state = State.Windup;
        }

        void ResetColor()
        {
            if (_sr == null) return;
            // 阶段越高，配色越暗沉 + 偏红，强化狂暴感
            _sr.color = _phase == 0
                ? new Color(0.06f, 0.04f, 0.10f)
                : _phase == 1
                    ? new Color(0.14f, 0.04f, 0.12f)
                    : new Color(0.28f, 0.03f, 0.10f);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);
            if (colorGearDrop != null)
                Pickup.SpawnAt(transform.position + Vector3.up * 0.9f, colorGearDrop);
            WeaponDropDirector.OnEnemyKilled(transform.position);
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

            // 阶段无敌：脉冲红膜显示，状态机继续推进让 boss 释放新阶段招式
            if (_invulnT > 0f)
            {
                _invulnT -= dt;
                if (_sr != null)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
                    _sr.color = Color.Lerp(new Color(0.55f, 0.05f, 0.10f),
                                           new Color(1.00f, 0.30f, 0.25f), pulse);
                }
                if (_invulnT <= 0f) ResetColor();
            }

            if (player == null) ResolvePlayer();
            if (player == null) return;

            _t += dt;

            switch (_state)
            {
                case State.Chase:   TickChase(dt);   break;
                case State.Windup:  TickWindup();    break;
                case State.Attack:  TickAttack(dt);  break;
                case State.Recover: TickRecover();   break;
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
            _kind = ChooseAttack(Mathf.Abs(dx));
            _state = State.Windup;
        }

        AttackKind ChooseAttack(float dist)
        {
            float r = Random.value;
            if (_phase == 0)
            {
                if (dist > rangedPreferDist) return AttackKind.RangedBolts;
                return r < 0.55f ? AttackKind.MeleeDash : AttackKind.RangedBolts;
            }
            if (_phase == 1)
            {
                if (dist > 6.5f) return r < 0.5f ? AttackKind.RangedBolts : AttackKind.GroundPillars;
                if (r < 0.25f) return AttackKind.MeleeDash;
                if (r < 0.50f) return AttackKind.GroundPillars;
                if (r < 0.78f) return AttackKind.SlamShockwave;
                return AttackKind.RangedBolts;
            }
            // Phase 2：狂暴期
            if (r < 0.18f) return AttackKind.MeleeDash;
            if (r < 0.36f) return AttackKind.RangedBolts;
            if (r < 0.56f) return AttackKind.GroundPillars;
            if (r < 0.78f) return AttackKind.SlamShockwave;
            return AttackKind.BulletCurtain;
        }

        // ── 前摇 ──
        void TickWindup()
        {
            if (!_fxShown)
            {
                _fxShown = true;
                PrepareAttackData();
                ShowWindupTelegraph();
            }

            float windupDone = WindupTimeFor(_kind);
            if (_t < windupDone) return;
            _t = 0f;

            switch (_kind)
            {
                case AttackKind.MeleeDash:      _state = State.Attack; break;
                case AttackKind.SlamShockwave:  _state = State.Attack; break;
                case AttackKind.RangedBolts:    ExecuteShoot();       _state = State.Recover; break;
                case AttackKind.GroundPillars:  ExecutePillars();     _state = State.Recover; break;
                case AttackKind.BulletCurtain:  ExecuteBulletCurtain(); _state = State.Recover; break;
            }
        }

        void PrepareAttackData()
        {
            switch (_kind)
            {
                case AttackKind.MeleeDash:
                    _dashDirX = Mathf.Sign(player.position.x - transform.position.x);
                    _dashT = dashDuration;
                    break;
                case AttackKind.SlamShockwave:
                    _slamT = 0f;
                    _slamOrigin = transform.position;
                    break;
            }
        }

        float WindupTimeFor(AttackKind k)
        {
            switch (k)
            {
                case AttackKind.MeleeDash:     return meleeWindupTime;
                case AttackKind.RangedBolts:   return rangedWindupTime;
                case AttackKind.GroundPillars: return pillarWindupTime;
                case AttackKind.SlamShockwave: return slamWindupTime;
                case AttackKind.BulletCurtain: return curtainWindupTime;
            }
            return 0.45f;
        }

        void ShowWindupTelegraph()
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            switch (_kind)
            {
                case AttackKind.MeleeDash:
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, meleeRange + dashSpeed * dashDuration * 0.4f,
                        meleeWindupTime, new Color(0.92f, 0.12f, 0.05f, 0.72f),
                        slashHeight: 1.1f);
                    break;
                case AttackKind.RangedBolts:
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, 14f,
                        rangedWindupTime, new Color(0.35f, 0.05f, 0.85f, 0.55f),
                        slashHeight: 0.38f);
                    break;
                case AttackKind.GroundPillars:
                    SpawnPillarTelegraphs();
                    break;
                case AttackKind.SlamShockwave:
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position + Vector3.down * 1.2f, 0f, 2.8f,
                        slamWindupTime, new Color(0.65f, 0.10f, 0.10f, 0.55f),
                        slashHeight: 0.55f);
                    break;
                case AttackKind.BulletCurtain:
                    if (player != null)
                    {
                        Vector3 pos = new Vector3(player.position.x, transform.position.y + 2.6f, 0f);
                        float halfSpan = curtainSpacing * (curtainCount - 1) * 0.5f + 0.6f;
                        GameRuntimeBootstrap.ShowAttackSlashFx(
                            pos, 1f, halfSpan,
                            curtainWindupTime, new Color(0.35f, 0.05f, 0.85f, 0.55f),
                            slashHeight: 0.55f);
                    }
                    break;
            }
        }

        // ── 攻击执行 ──
        void TickAttack(float dt)
        {
            switch (_kind)
            {
                case AttackKind.MeleeDash:     TickDash(dt);     break;
                case AttackKind.SlamShockwave: TickSlamArc(dt);  break;
                default:                       _state = State.Recover; break;
            }
        }

        void TickDash(float dt)
        {
            _dashT -= dt;
            transform.position += new Vector3(_dashDirX * dashSpeed * dt, 0f, 0f);

            if (player != null)
            {
                float dist = Vector2.Distance(player.position, transform.position);
                if (dist <= meleeRange * 0.6f)
                {
                    var stats = PlayerStats.Active ?? player.GetComponent<PlayerStats>();
                    if (stats != null)
                    {
                        stats.TakeDamage(meleeDamage * (_phase == 2 ? 1.3f : 1f), gameObject);
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

        void TickSlamArc(float dt)
        {
            _slamT += dt;
            float u = Mathf.Clamp01(_slamT / slamAirTime);
            float yOff = 4f * slamApexHeight * u * (1f - u);
            float lateralDir = player != null ? Mathf.Sign(player.position.x - _slamOrigin.x) : 0f;
            float lateral = u * 1.4f * lateralDir;
            transform.position = _slamOrigin + new Vector3(lateral, yOff, 0f);

            if (u >= 1f)
            {
                Vector3 landPos = _slamOrigin + new Vector3(lateral, 0f, 0f);
                landPos.y = _baseY;
                transform.position = landPos;
                SpawnShockwave(-1f);
                SpawnShockwave(+1f);
                _t = 0f;
                _state = State.Recover;
            }
        }

        // ── 远程墨弹（按阶段扇形增宽） ──
        void ExecuteShoot()
        {
            if (player == null) return;
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            Vector2 baseDir = new Vector2(dirX, 0f);
            float[] angles = _phase == 0
                ? new[] { -25f, 0f, 25f }
                : _phase == 1
                    ? new[] { -40f, -20f, -5f, 5f, 20f, 40f }
                    : new[] { -55f, -38f, -20f, -7f, 7f, 20f, 38f, 55f };
            bool homing = _phase >= 1;
            foreach (var a in angles)
            {
                Vector2 vel = InkBolt.Rotate(baseDir * projectileSpeed, a);
                SpawnBolt(transform.position, vel, rangedDamage * (_phase == 2 ? 1.25f : 1f), homing, 5f);
            }
        }

        // ── 墨柱地刺：前摇时锁定红圈 X，柱体一定爆在红圈位置（玩家可走位躲开） ──
        void SpawnPillarTelegraphs()
        {
            if (player == null) return;
            _pillarTargetXs = new float[pillarCount];
            for (int i = 0; i < pillarCount; i++)
            {
                float x = player.position.x + (i - (pillarCount - 1) * 0.5f) * pillarSpacing;
                _pillarTargetXs[i] = x;
                Vector3 pos = new Vector3(x, _baseY - 1.3f, 0f);
                GameRuntimeBootstrap.ShowAttackSlashFx(
                    pos, 0f, pillarHalfWidth * 1.5f,
                    pillarWindupTime, new Color(0.92f, 0.12f, 0.05f, 0.65f),
                    slashHeight: pillarHeight * 0.55f);
            }
        }

        void ExecutePillars()
        {
            if (_pillarTargetXs == null) return;
            for (int i = 0; i < _pillarTargetXs.Length; i++)
            {
                Vector3 pos = new Vector3(_pillarTargetXs[i], _baseY - 0.6f, 0f);
                SpawnPillar(pos);
            }
            _pillarTargetXs = null;
        }

        void SpawnPillar(Vector3 pos)
        {
            var go = new GameObject("InkPillar");
            go.transform.position = pos;
            go.transform.localScale = new Vector3(pillarHalfWidth * 2f, pillarHeight, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.20f, 0.04f, 0.30f, 0.94f);
            sr.sortingOrder = 9;
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            var hit = go.AddComponent<InkAreaHit>();
            hit.damage = pillarDamage;
            hit.life = pillarStrikeDuration;
        }

        // ── 地震波 ──
        void SpawnShockwave(float dirX)
        {
            var go = new GameObject("InkShockwave");
            Vector3 origin = transform.position + new Vector3(dirX * 1.0f, -1.55f, 0f);
            go.transform.position = origin;
            go.transform.localScale = new Vector3(1.05f, 0.55f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.20f, 0.04f, 0.30f, 0.88f);
            sr.sortingOrder = 6;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = 0.46f;
            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity = new Vector2(dirX * shockwaveSpeed, 0f);
            bolt.damage = shockwaveDamage;
            bolt.life = shockwaveLife;
            bolt.destroyOnGround = false;
            bolt.isHoming = false;
        }

        // ── 墨幕坠落 ──
        void ExecuteBulletCurtain()
        {
            if (player == null) return;
            float centerX = player.position.x;
            float topY = transform.position.y + curtainHeight;
            for (int i = 0; i < curtainCount; i++)
            {
                float x = centerX + (i - (curtainCount - 1) * 0.5f) * curtainSpacing;
                Vector3 pos = new Vector3(x, topY, 0f);
                float lateral = (Random.value - 0.5f) * 0.6f;
                Vector2 vel = new Vector2(lateral, -1f).normalized * curtainFallSpeed;
                SpawnBolt(pos, vel, curtainDamage, false, 3.5f);
            }
        }

        void SpawnBolt(Vector3 origin, Vector2 vel, float dmg, bool homing, float life)
        {
            var go = new GameObject("InkBolt");
            go.transform.position = origin + new Vector3(vel.x > 0 ? 0.7f : (vel.x < 0 ? -0.7f : 0f), 0.1f, 0f);
            go.transform.localScale = new Vector3(0.55f, 0.22f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.22f, 0.04f, 0.48f, 0.92f);
            sr.sortingOrder = 7;
            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true; rb.gravityScale = 0f;
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true; col.radius = 0.2f;
            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity = vel;
            bolt.damage = dmg;
            bolt.isHoming = homing;
            bolt.life = life;
        }

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

    // ── 墨弹飞行体（支持追踪 / 自定义生命 / 是否被地面打断） ──
    public class InkBolt : MonoBehaviour
    {
        public Vector2 velocity;
        public float damage;
        public bool isHoming;
        public float homingDuration  = 1f;
        public float homingTurnSpeed = 150f; // 每秒最大转向角度

        /// <summary>飞行寿命（秒）。0 / 负数会立即销毁。</summary>
        public float life = 6f;

        /// <summary>是否在触碰 Ground 时销毁。地震波等沿地面飞行的弹体设为 false。</summary>
        public bool destroyOnGround = true;

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
            life -= Time.deltaTime;
            if (life <= 0f) Destroy(gameObject);
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
            if (other.CompareTag("Ground"))
            {
                if (destroyOnGround) Destroy(gameObject);
                return;
            }
            var stats = other.GetComponent<PlayerStats>()
                     ?? other.GetComponentInParent<PlayerStats>();
            if (stats == null) return;
            _hit = true;
            stats.TakeDamage(damage, gameObject);
            Destroy(gameObject);
        }
    }
}
