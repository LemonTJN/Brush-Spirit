using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>
    /// 墨林 Boss：墨树。三阶段，含 5 种攻击模式：
    ///   ① 墨爪冲刺 MeleeDash（贴身高伤、近战必杀技）
    ///   ② 追踪墨弹 RangedBolts（远程扇形 + 阶段越高弹数越多）
    ///   ③ 墨柱地刺 GroundPillars（脚下区域伏击，红圈警示 → 柱体爆发）
    ///   ④ 墨涌震波 SlamShockwave（抛物线跳跃落地 → 双向地震波）
    ///   ⑤ 墨幕坠落 BulletCurtain（终末期专属，玩家上方密集弹幕）
    /// 参考主流 2D 动作游戏：Hollow Knight、Dead Cells、Salt and Sanctuary 的多模式 telegraph 战斗。
    /// </summary>
    public class BossInkTree : MonoBehaviour, IDamageable
    {
        [Header("基础属性")]
        public float maxHp = 580f;
        public float moveSpeed = 3.0f;

        [Header("近战 — 墨爪冲刺")]
        public float slamRadius = 2.4f;
        public float slamDamage = 26f;
        public float meleeWindupTime = 0.60f;   // 延长前摇，给玩家反应空间
        public float dashSpeed = 24f;
        public float dashDuration = 0.95f;

        [Header("远程 — 墨弹（追踪）")]
        public float boltDamage = 16f;
        public float boltSpeed = 9f;
        public float rangedWindupTime = 0.70f;  // 延长瞄准窗口
        public float boltHomingDuration  = 1.0f;
        public float boltHomingTurnSpeed = 150f;

        [Header("墨柱地刺 GroundPillars")]
        public int pillarCount = 3;
        public float pillarSpacing = 2.4f;          // 增大间距以保留躲避空隙
        public float pillarWindupTime = 0.8f;      // 红圈预警时长，便于玩家走位
        public float pillarStrikeDuration = 0.45f;
        public float pillarDamage = 22f;
        public float pillarHalfWidth = 0.55f;
        public float pillarHeight = 2.4f;

        [Header("阶段切换无敌（红膜）")]
        [Tooltip("进入 Phase1 / Phase2 时短暂无敌时长（秒），同时披上红色脉冲。")]
        public float phase1InvulnTime = 1.0f;
        public float phase2InvulnTime = 1.3f;

        [Header("贴脸惩罚（Contact Damage，参考 Hollow Knight）")]
        [Tooltip("玩家距 boss 中心小于此值视为贴脸")]
        public float contactRange = 1.10f;
        [Tooltip("贴脸时每秒造成的伤害（每 0.5s 结算一次）")]
        public float contactDamagePerSec = 14f;
        float _contactTickT;

        [Header("墨涌震波 SlamShockwave")]
        public float slamWindupTime = 0.42f;
        public float slamApexHeight = 1.9f;
        public float slamAirTime = 0.55f;
        public float shockwaveSpeed = 7.5f;
        public float shockwaveDamage = 24f;
        public float shockwaveLife = 1.9f;

        [Header("墨幕坠落 BulletCurtain")]
        public int curtainCount = 9;
        public float curtainSpacing = 1.15f;
        public float curtainHeight = 4.2f;
        public float curtainWindupTime = 0.70f;
        public float curtainFallSpeed = 6.5f;
        public float curtainDamage = 18f;

        [Header("掉落 / 击退")]
        public int xpReward = 80;
        public EquipmentData colorGearDrop;
        public float knockDecayPerSec = 16f;
        public float bossKnockbackMul = 0.55f;

        public Transform player;
        public System.Action OnDefeated;

        // ── 运行时 ──
        enum AttackKind { MeleeDash, RangedBolts, GroundPillars, SlamShockwave, BulletCurtain }
        enum BossState { Chase, Windup, Attack, Recover }

        float _hp;
        float _t;
        int _phase;
        bool _windupFxShown;
        AttackKind _kind;
        BossState _state = BossState.Chase;

        // Melee dash 缓存
        float _dashT;
        float _dashDirX;
        bool _dashHit;

        // Slam arc 缓存
        float _slamT;
        Vector3 _slamOrigin;

        // 阶段切换无敌
        float _invulnT;

        // 墨柱地刺位置缓存（避免「红圈在 A 点但柱体跟着玩家到 B 点」的 bug）
        float[] _pillarTargetXs;

        // 墨幕坠落中心点缓存（同样防止 telegraph 与 execute 位置不一致）
        float _curtainCenterX;
        bool _curtainCenterValid;

        SpriteRenderer _sr;
        WorldHealthBar _bar;
        float _knockVelX;
        float _baseY; // 立足平面 Y（落地后用于校正高度）

        float AttackInterval => _phase == 0 ? 2.3f : _phase == 1 ? 1.55f : 1.05f;
        float RecoverTime    => _phase == 0 ? 0.75f : _phase == 1 ? 0.48f : 0.30f;

        void Start()
        {
            _hp = maxHp;
            _sr = GetComponent<SpriteRenderer>();
            EnsureGroundFooting();
            _baseY = transform.position.y;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 1.85f);
            ResolvePlayerTransform();
        }

        /// <summary>
        /// 把 boss 放到主地面上。注意：平台 / 侧墙 也用 tag="Ground"，简单向下射线会落到平台上。
        /// 做法：找场景中所有 Ground 碰撞体，选「最宽的那个」（= 主地面，平台/侧墙都很窄），
        /// 把脚放到它的顶部。
        /// </summary>
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
                // 平台/侧墙通常 < 5 单位；主地面横跨整个场地，宽度远大于 5
                if (w < 5f) continue;
                if (w > bestWidth)
                {
                    bestWidth = w;
                    best = col;
                }
            }

            if (best == null) return;
            Vector3 p = transform.position;
            p.y = best.bounds.max.y + halfHeight + 0.05f;
            transform.position = p;
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
            // 阶段切换时短暂无敌：仅做受击闪烁，不扣血、不触发击退
            if (_invulnT > 0f)
            {
                if (_sr != null) _sr.color = new Color(1f, 0.55f, 0.55f);
                return;
            }

            _hp -= amount;
            _bar?.SetHp(_hp);
            if (knockbackImpulseX > 0f)
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker)
                              * knockbackImpulseX * bossKnockbackMul;
            if (_sr != null) { _sr.color = Color.white; Invoke(nameof(ResetColor), 0.08f); }

            if (_hp <= maxHp * 0.60f && _phase == 0) EnterPhase(1);
            if (_hp <= maxHp * 0.30f && _phase == 1) EnterPhase(2);
            if (_hp <= 0f) Die(attacker);
        }

        /// <summary>
        /// 阶段切换：进入无敌帧 + 强制立刻发起该阶段的招牌技，迫使玩家躲避，避免被秒杀。
        /// </summary>
        void EnterPhase(int p)
        {
            _phase = p;
            _invulnT = p == 1 ? phase1InvulnTime : phase2InvulnTime;
            _t = 0f;
            _windupFxShown = false;
            _knockVelX = 0f;
            // 立刻进入新阶段的招牌技：Phase1 → 震波，Phase2 → 弹幕雨
            _kind = p == 1 ? AttackKind.SlamShockwave : AttackKind.BulletCurtain;
            _state = BossState.Windup;
        }

        void ResetColor()
        {
            if (_sr == null) return;
            // 阶段越高，颜色越暗紫，强化「狂暴」视觉
            _sr.color = _phase == 0
                ? new Color(0.10f, 0.10f, 0.12f)
                : _phase == 1
                    ? new Color(0.16f, 0.06f, 0.18f)
                    : new Color(0.26f, 0.04f, 0.22f);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);
            // 取消 boss 装备掉落（颜色画笔等占位拾取物视觉不佳，进度由武器形态拾取 + 通关流程承担）
            WeaponDropDirector.OnEnemyKilled(transform.position);
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

            // 阶段无敌：身体显示红色脉冲，但 boss 仍正常推进状态机（继续放新阶段招式）
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

            if (player == null) ResolvePlayerTransform();
            if (player == null) return;
            _t += dt;

            TickContactDamage(dt);

            switch (_state)
            {
                case BossState.Chase:   TickChase(dt);   break;
                case BossState.Windup:  TickWindup();    break;
                case BossState.Attack:  TickAttack(dt);  break;
                case BossState.Recover: TickRecover();   break;
            }
        }

        /// <summary>
        /// 贴脸惩罚：玩家距 boss 在 contactRange 内时持续掉血（每 0.5s 一次）。
        /// 这就是 Hollow Knight 大部分 boss 用的「身体接触判定」——它把战斗节奏从
        /// 「贴脸站桩平A」改造成「dash 进 → 打两下 → dash 出」。
        /// 玩家 dash 期间无敌（PlayerStats 已实现 i-frames），所以是公平的。
        /// </summary>
        void TickContactDamage(float dt)
        {
            if (player == null) return;
            if (contactDamagePerSec <= 0f) return;
            float dist = Vector2.Distance(player.position, transform.position);
            if (dist > contactRange) { _contactTickT = 0f; return; }

            _contactTickT -= dt;
            if (_contactTickT > 0f) return;
            _contactTickT = 0.5f; // 每 0.5s 一次结算

            var stats = PlayerStats.Active != null
                ? PlayerStats.Active
                : player.GetComponent<PlayerStats>();
            stats?.TakeDamage(contactDamagePerSec * 0.5f, gameObject);
        }

        // ── 追击 ──
        void TickChase(float dt)
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * dt, 0f, 0f);

            if (_t < AttackInterval) return;
            _t = 0f;
            _windupFxShown = false;
            _kind = ChooseAttack(Mathf.Abs(dx));
            _state = BossState.Windup;
        }

        AttackKind ChooseAttack(float dist)
        {
            float r = Random.value;
            if (_phase == 0)
            {
                if (dist > 5.5f) return AttackKind.RangedBolts;
                return r < 0.55f ? AttackKind.MeleeDash : AttackKind.RangedBolts;
            }

            if (_phase == 1)
            {
                if (dist > 6.5f) return r < 0.6f ? AttackKind.RangedBolts : AttackKind.GroundPillars;
                if (r < 0.28f) return AttackKind.MeleeDash;
                if (r < 0.52f) return AttackKind.GroundPillars;
                if (r < 0.78f) return AttackKind.SlamShockwave;
                return AttackKind.RangedBolts;
            }

            // Phase 2 — 狂暴：所有招式可用，含弹幕墨幕
            if (r < 0.18f) return AttackKind.MeleeDash;
            if (r < 0.38f) return AttackKind.RangedBolts;
            if (r < 0.58f) return AttackKind.GroundPillars;
            if (r < 0.78f) return AttackKind.SlamShockwave;
            return AttackKind.BulletCurtain;
        }

        // ── 前摇：展示 telegraph + 缓存攻击数据 ──
        void TickWindup()
        {
            if (!_windupFxShown)
            {
                _windupFxShown = true;
                PrepareAttackData();
                ShowWindupTelegraph();
            }

            float windupDone = WindupTimeFor(_kind);
            if (_t < windupDone) return;
            _t = 0f;

            // 由攻击类型决定下一阶段
            switch (_kind)
            {
                case AttackKind.MeleeDash:
                    _state = BossState.Attack;
                    break;
                case AttackKind.SlamShockwave:
                    _state = BossState.Attack;
                    break;
                case AttackKind.RangedBolts:
                    ExecuteShoot();
                    _state = BossState.Recover;
                    break;
                case AttackKind.GroundPillars:
                    ExecutePillars();
                    _state = BossState.Recover;
                    break;
                case AttackKind.BulletCurtain:
                    ExecuteBulletCurtain();
                    _state = BossState.Recover;
                    break;
            }
        }

        void PrepareAttackData()
        {
            switch (_kind)
            {
                case AttackKind.MeleeDash:
                    _dashDirX = Mathf.Sign(player.position.x - transform.position.x);
                    _dashT = dashDuration;
                    _dashHit = false;
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
                case AttackKind.MeleeDash:      return meleeWindupTime;
                case AttackKind.RangedBolts:    return rangedWindupTime;
                case AttackKind.GroundPillars:  return pillarWindupTime;
                case AttackKind.SlamShockwave:  return slamWindupTime;
                case AttackKind.BulletCurtain:  return curtainWindupTime;
            }
            return 0.4f;
        }

        void ShowWindupTelegraph()
        {
            float dir = Mathf.Sign(player.position.x - transform.position.x);
            switch (_kind)
            {
                case AttackKind.MeleeDash:
                    // 红色宽长条 — 注意冲刺路径！
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, slamRadius + dashSpeed * dashDuration * 0.4f,
                        meleeWindupTime, new Color(0.95f, 0.18f, 0.08f, 0.68f), slashHeight: 1.1f);
                    break;
                case AttackKind.RangedBolts:
                    // 紫色细线 — 瞄准
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position, dir, 13f,
                        rangedWindupTime, new Color(0.4f, 0.05f, 0.9f, 0.55f), slashHeight: 0.35f);
                    break;
                case AttackKind.GroundPillars:
                    SpawnPillarTelegraphs();
                    break;
                case AttackKind.SlamShockwave:
                    // 脚下大圆 + 头顶警示符
                    GameRuntimeBootstrap.ShowAttackSlashFx(
                        transform.position + Vector3.down * 1.2f, 0f, 2.6f,
                        slamWindupTime, new Color(0.6f, 0.18f, 0.08f, 0.55f), slashHeight: 0.55f);
                    break;
                case AttackKind.BulletCurtain:
                    // 在玩家头顶宽幅紫条 —— 锁定本次弹幕的中心 X，execute 时按此点而非「之后的玩家位置」生成
                    if (player != null)
                    {
                        _curtainCenterX = player.position.x;
                        _curtainCenterValid = true;
                        Vector3 pos = new Vector3(_curtainCenterX, transform.position.y + 2.4f, 0f);
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
                default:                       _state = BossState.Recover; break;
            }
        }

        void TickDash(float dt)
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

        /// <summary>抛物线跳起再落地，落地后向左右各发一道地震波。</summary>
        void TickSlamArc(float dt)
        {
            _slamT += dt;
            float u = Mathf.Clamp01(_slamT / slamAirTime);
            // 抛物线：4*h*u*(1-u) 在 u=0.5 处达到峰值 h
            float yOff = 4f * slamApexHeight * u * (1f - u);
            float lateralDir = player != null ? Mathf.Sign(player.position.x - _slamOrigin.x) : 0f;
            float lateral = u * 1.4f * lateralDir;

            transform.position = _slamOrigin + new Vector3(lateral, yOff, 0f);

            if (u >= 1f)
            {
                // 落地
                Vector3 landPos = _slamOrigin + new Vector3(lateral, 0f, 0f);
                landPos.y = _baseY;
                transform.position = landPos;

                SpawnShockwave(-1f);
                SpawnShockwave(+1f);
                _t = 0f;
                _state = BossState.Recover;
            }
        }

        // ── 远程：扇形墨弹（阶段越高扇形越宽、弹数越多；始终追踪以维持原手感）──
        void ExecuteShoot()
        {
            if (player == null) return;
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            Vector2 baseDir = new Vector2(dirX, 0f);

            float[] angles = _phase == 0
                ? new[] { -25f, -10f, 10f, 25f }
                : _phase == 1
                    ? new[] { -40f, -20f, -5f, 5f, 20f, 40f }
                    : new[] { -55f, -38f, -20f, -7f, 7f, 20f, 38f, 55f };

            foreach (float a in angles)
            {
                Vector2 vel = InkBolt.Rotate(baseDir * boltSpeed, a);
                SpawnBolt(transform.position, vel, boltDamage, /*homing*/ true, 5f);
            }
        }

        // ── 墨柱地刺：在前摇时锁定红圈 X 坐标，柱体一定在红圈位置爆发，可走位躲开 ──
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
                    pillarWindupTime, new Color(0.95f, 0.18f, 0.08f, 0.62f),
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
            sr.color = new Color(0.16f, 0.06f, 0.34f, 0.94f);
            sr.sortingOrder = 9;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;

            var hit = go.AddComponent<InkAreaHit>();
            hit.damage = pillarDamage;
            hit.life = pillarStrikeDuration;
        }

        // ── 墨涌震波：左右两道沿地面飞行的弹体 ──
        void SpawnShockwave(float dirX)
        {
            var go = new GameObject("InkShockwave");
            Vector3 origin = transform.position + new Vector3(dirX * 1.0f, -1.55f, 0f);
            go.transform.position = origin;
            go.transform.localScale = new Vector3(1.0f, 0.55f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.18f, 0.05f, 0.42f, 0.88f);
            sr.sortingOrder = 6;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.45f;

            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity = new Vector2(dirX * shockwaveSpeed, 0f);
            bolt.damage = shockwaveDamage;
            bolt.life = shockwaveLife;
            bolt.destroyOnGround = false; // 沿地面飞行，不被 Ground 一触即消
            bolt.isHoming = false;
        }

        // ── 墨幕坠落：用前摇时锁定的中心 X 生成（不再跟随玩家移动） ──
        void ExecuteBulletCurtain()
        {
            // 若 telegraph 没跑（极端情况），退回玩家位置
            float centerX = _curtainCenterValid
                ? _curtainCenterX
                : (player != null ? player.position.x : transform.position.x);
            _curtainCenterValid = false;

            float topY = transform.position.y + curtainHeight;
            for (int i = 0; i < curtainCount; i++)
            {
                float x = centerX + (i - (curtainCount - 1) * 0.5f) * curtainSpacing;
                Vector3 pos = new Vector3(x, topY, 0f);

                // 加微小水平扰动，制造「不规则雨幕」感
                float lateral = (Random.value - 0.5f) * 0.6f;
                Vector2 vel = new Vector2(lateral, -1f).normalized * curtainFallSpeed;
                SpawnBolt(pos, vel, curtainDamage, false, 3.5f);
            }
        }

        // ── 通用墨弹生成 ──
        void SpawnBolt(Vector3 origin, Vector2 vel, float dmg, bool homing, float life)
        {
            var go = new GameObject("InkBolt");
            go.transform.position = origin + new Vector3(vel.x > 0 ? 0.7f : (vel.x < 0 ? -0.7f : 0f), 0.1f, 0f);
            go.transform.localScale = new Vector3(0.5f, 0.22f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.22f, 0.04f, 0.48f, 0.92f);
            sr.sortingOrder = 7;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.2f;

            var bolt = go.AddComponent<InkBolt>();
            bolt.velocity = vel;
            bolt.damage = dmg;
            bolt.isHoming = homing;
            bolt.homingDuration = boltHomingDuration;
            bolt.homingTurnSpeed = boltHomingTurnSpeed;
            bolt.life = life;
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

    /// <summary>固定位置的伤害区域（如墨柱地刺），停留期间持续判定。</summary>
    public class InkAreaHit : MonoBehaviour
    {
        public float damage = 20f;
        public float life = 0.4f;
        bool _hit;
        float _t;

        void Update()
        {
            _t += Time.deltaTime;
            if (_t >= life) Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
        void OnTriggerStay2D(Collider2D other)  { TryHit(other); }

        void TryHit(Collider2D other)
        {
            if (_hit) return;
            var stats = other.GetComponent<PlayerStats>() ?? other.GetComponentInParent<PlayerStats>();
            if (stats == null) return;
            _hit = true;
            stats.TakeDamage(damage, gameObject);
        }
    }
}
