using System.Collections;
using BrushSpirit.Core;
using BrushSpirit.LevelFlow;
using BrushSpirit.UI;
using UnityEngine;

namespace BrushSpirit.Player
{
    public class PlayerCombat : MonoBehaviour
    {
        public LayerMask hurtboxMask;
        public Vector2 attackBoxSize = new Vector2(1.35f, 0.85f);
        public float attackOffset = 0.75f;
        public float kSkillRadius = 1.75f;
        public float kCooldown = 5f;
        public float lCooldown = 8f;

        [Tooltip("在角色左右两侧同时判定，避免面朝与怪相反时完全打空")]
        public float meleeWidthExtra = 0.55f;

        [Header("击退（水平冲量，越大飞越远）")]
        [Tooltip("普攻 J、三连 L（水平击退冲量，越小退得越近）")]
        public float knockbackJL = 5.5f;
        [Tooltip("墨爆 K")]
        public float knockbackK = 22f;

        PlayerStats _stats;
        PlayerMovement _move;
        BoxCollider2D _body;
        Animator _anim;
        static readonly int kAttack = Animator.StringToHash("Attack");
        static readonly int kCombo = Animator.StringToHash("Combo");
        int _comboIndex;
        bool _busy;

        public enum WeaponMode { Bare = 1, Sword = 2, Pistol = 3 }
        public WeaponMode CurrentWeapon { get; private set; } = WeaponMode.Bare;

        // ──────────────────────────────────────────────
        // 武器伤害系数：参考 Devil May Cry / Hollow Knight 的「武器基础倍率 × 动作倍率」体系。
        //   - 拳偏轻，连击曲线温和；剑标准，连击末段大幅加伤；枪 J 走子弹独立伤害。
        //   - K（墨爆）、L（三连）是「水墨技能」，仍受武器倍率影响（拿剑后清场更强）。
        // ──────────────────────────────────────────────
        [Header("武器伤害倍率（武器系数）")]
        [Tooltip("赤手空拳 — 攻击力倍率")]
        public float bareDamageMul = 0.65f;
        [Tooltip("剑 — 攻击力倍率")]
        public float swordDamageMul = 1.15f;
        [Tooltip("枪 — 近战 K/L 技能时的倍率（J 走子弹独立伤害）")]
        public float pistolMeleeMul = 0.95f;
        [Tooltip("枪 — 子弹伤害额外系数（与 pistolDamage 相乘）。1.20 让远程子弹略胜近战速点，体现「精准换风险」")]
        public float pistolBulletMul = 1.20f;

        [Header("J 连击曲线（三段循环）")]
        public float[] bareComboMuls  = { 1.00f, 1.05f, 1.15f };
        public float[] swordComboMuls = { 1.00f, 1.18f, 1.42f };

        [Header("普攻 J 节奏锁（每段攻击间最短间隔）")]
        [Tooltip("赤手空拳的普攻间隔，越小越快但伤害低")]
        public float meleeCooldownBare  = 0.30f;
        [Tooltip("剑的普攻间隔，剑要慢但伤害高，差异化武器手感")]
        public float meleeCooldownSword = 0.42f;
        [Tooltip("连击 1.2 秒内未继续按 J 则归零，避免一连按到底")]
        public float comboResetTime = 1.2f;
        float _jCdRemaining;
        float _comboGraceT;

        [Header("技能动作倍率（再乘武器倍率）")]
        [Tooltip("K 墨爆 AOE 的动作倍率（剑形态实际伤害 ≈ 1.15 × 1.50 = 1.7×攻击力）")]
        public float kSkillActionMul = 1.50f;
        [Tooltip("L 三连每段的动作倍率（每段独立结算）。每段都比 J 末段(1.42)还高，三段总伤约 J 速点 1 秒的 1.8 倍。")]
        public float lSkillActionMul = 1.70f;
        [Tooltip("L 三连每段之间的间隔（秒），越小越紧凑")]
        public float lSlashInterval = 0.16f;

        [Header("武器范围倍率（影响 J / L 的命中盒尺寸）")]
        public float bareRangeMul = 0.85f;
        public float swordRangeMul = 1.15f;

        /// <summary>本次 Run 内是否已通过拾取解锁「剑」形态。</summary>
        public static bool HasSword;

        /// <summary>本次 Run 内是否已通过拾取解锁「枪」形态。</summary>
        public static bool HasPistol;

        /// <summary>玩家死亡 / 返回菜单时清空解锁状态，由 PlayerRunCarry.ClearRun 调用。</summary>
        public static void ResetUnlocks()
        {
            HasSword = false;
            HasPistol = false;
        }

        bool _initialVisualApplied;
        float _lockedToastCd;

        [Header("枪 终极弹 L（墨痕巨弹 / Ink Slug）")]
        [Tooltip("主弹相对 pistolDamage×pistolBulletMul 的额外倍率。3.5 让单体伤害与剑 L 三连 burst 持平。")]
        public float pistolUltMul = 3.5f;
        [Tooltip("巨弹的视觉/碰撞放大倍数")]
        public float pistolUltScaleMul = 3.0f;
        [Tooltip("最多穿透敌人数（不算 AOE 命中）")]
        public int pistolUltPierces = 3;
        [Tooltip("命中点 AOE 爆开的半径")]
        public float pistolUltAoeRadius = 1.5f;
        [Tooltip("AOE 伤害是主弹伤害的多少倍")]
        public float pistolUltAoeMul = 0.6f;
        [Tooltip("巨弹飞行速度（比普通子弹慢，呈现「沉重感」）")]
        public float pistolUltSpeed = 12f;
        public float pistolUltLife = 1.6f;
        public float pistolUltKnockback = 14f;

        [Header("枪 L 蓄力（参考 DMC5 Nero 的 Charge Shot）")]
        [Tooltip("低于此时长释放 → 取消蓄力，不消耗 CD。给玩家「手滑」的容错。")]
        public float chargeMinTime = 0.18f;
        public float chargeLv2Time = 0.55f;
        public float chargeLv3Time = 1.05f;
        [Tooltip("达到此时长自动释放，避免玩家死按不松")]
        public float chargeMaxTime = 1.35f;
        [Tooltip("各级伤害倍率（Lv1/2/3），在 pistolUltMul 基础上再叠加")]
        public float[] chargeDamageMuls   = { 1.00f, 1.45f, 1.95f };
        [Tooltip("各级穿透数额外加成")]
        public int[]   chargePiercesBonus = { 0,     1,     2     };
        [Tooltip("各级巨弹尺寸倍率")]
        public float[] chargeScaleMuls    = { 1.00f, 1.18f, 1.40f };
        [Tooltip("各级 AOE 范围倍率")]
        public float[] chargeAoeRadiusMuls= { 1.00f, 1.22f, 1.50f };

        bool _charging;
        float _chargeT;
        int _lastChargeLv;
        GameObject _chargeMuzzleFx;
        SpriteRenderer _chargeMuzzleSr;

        [Header("手枪")]
        public float pistolFireCooldown = 0.35f;
        // 子弹基础值：从 10 提到 16，跟玩家 baseAttack 10→14 的同步上调；再乘 pistolBulletMul = 1.20。
        public float pistolDamage = 16f;
        public float pistolMuzzleOffsetX = 0.45f;
        public float pistolMuzzleOffsetY = -0.1f;
        float _pistolCdRemaining;

        /// <summary>身体几何中心；用 BoxCollider2D.offset 补偿 Sprite Pivot 不在视觉中心的情况。</summary>
        Vector2 BodyCenter
        {
            get
            {
                if (_body == null) _body = GetComponent<BoxCollider2D>();
                Vector2 off = _body != null ? _body.offset : Vector2.zero;
                return (Vector2)transform.position + off;
            }
        }

        public float KCdRemaining { get; private set; }
        public float LCdRemaining { get; private set; }

        void Awake()
        {
            CacheRefs();
            if (hurtboxMask.value == 0)
                hurtboxMask = ~0;
            Physics2D.queriesHitTriggers = true;
        }

        void Start()
        {
            CacheRefs();
            // 进入游戏默认赤手空拳；显式应用一次形态视觉，覆盖 BuildPlayer 时挂的默认 Animator Controller
            if (!_initialVisualApplied)
            {
                ApplyWeaponVisual(CurrentWeapon);
                _initialVisualApplied = true;
            }
        }

        void CacheRefs()
        {
            if (_stats == null) _stats = GetComponent<PlayerStats>();
            if (_move == null) _move = GetComponent<PlayerMovement>();
            if (_anim == null) _anim = GetComponent<Animator>();
        }

        void Update()
        {
            if (KCdRemaining > 0f) KCdRemaining -= Time.deltaTime;
            if (LCdRemaining > 0f) LCdRemaining -= Time.deltaTime;
            if (_pistolCdRemaining > 0f) _pistolCdRemaining -= Time.deltaTime;
            if (_jCdRemaining > 0f) _jCdRemaining -= Time.deltaTime;
            if (_lockedToastCd > 0f) _lockedToastCd -= Time.deltaTime;

            // 蓄力中：吃掉所有其他输入，只关心继续蓄 or 释放
            if (_charging)
            {
                TickCharge(Time.deltaTime);
                if (Input.GetKeyUp(KeyCode.L)) ReleaseCharge();
                return;
            }
            if (_comboGraceT > 0f)
            {
                _comboGraceT -= Time.deltaTime;
                if (_comboGraceT <= 0f) _comboIndex = 0;
            }

            // 1/2/3 切换武器
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeapon(WeaponMode.Bare);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeapon(WeaponMode.Sword);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetWeapon(WeaponMode.Pistol);

            // 修复原 bug：J 此前被放在 _busy 之前，导致放 K/L 期间还能继续按 J 输出。
            // 现在 J 也受 _busy 锁约束，并且加了节奏 cooldown。
            if (_busy) return;

            if (Input.GetKeyDown(KeyCode.J))
            {
                if (CurrentWeapon == WeaponMode.Pistol)
                {
                    if (_pistolCdRemaining <= 0f)
                    {
                        FirePistol();
                        _pistolCdRemaining = pistolFireCooldown;
                        if (_anim != null) _anim.SetTrigger(kAttack);
                    }
                }
                else if (_jCdRemaining <= 0f)
                {
                    // 普攻节奏锁：拳快剑慢，差异化武器手感。
                    float cd = (CurrentWeapon == WeaponMode.Sword) ? meleeCooldownSword : meleeCooldownBare;
                    _jCdRemaining = cd;
                    _comboGraceT = comboResetTime;

                    float comboMul = GetComboStepMul(_comboIndex);
                    float weaponMul = GetWeaponBaseMul();
                    DealMeleeWide(comboMul * weaponMul, new Color(1f, 0.92f, 0.55f, 0.58f),
                        0.12f, GetWeaponRangeMul(), knockbackJL);
                    _comboIndex = (_comboIndex + 1) % 3;
                    if (_anim != null) _anim.SetTrigger(kAttack);
                }
            }

            if (Input.GetKeyDown(KeyCode.K) && KCdRemaining <= 0f)
            {
                StartCoroutine(DoBlast());
                return;
            }

            if (Input.GetKeyDown(KeyCode.L) && LCdRemaining <= 0f)
            {
                if (CurrentWeapon == WeaponMode.Pistol)
                    StartCharge();
                else
                    StartCoroutine(DoTripleSlash());
            }
        }

        public void SetWeapon(WeaponMode mode)
        {
            if (mode == WeaponMode.Sword && !HasSword)
            {
                ShowLockedToast("尚未获得 剑，击败敌人会有掉落。");
                return;
            }
            if (mode == WeaponMode.Pistol && !HasPistol)
            {
                ShowLockedToast("尚未获得 枪，击败更多敌人后掉落。");
                return;
            }

            if (_charging) CancelCharge(); // 切武器立即打断蓄力，避免拿剑后还在攒能量球
            CurrentWeapon = mode;
            ApplyWeaponVisual(mode);
        }

        void ApplyWeaponVisual(WeaponMode mode)
        {
            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;
            if (_anim == null || gb == null) return;
            RuntimeAnimatorController ctl = null;
            switch (mode)
            {
                case WeaponMode.Bare:   ctl = gb.bareController; break;
                case WeaponMode.Sword:  ctl = gb.playerController; break;
                case WeaponMode.Pistol: ctl = gb.pistolController; break;
            }
            if (ctl != null) _anim.runtimeAnimatorController = ctl;
        }

        void ShowLockedToast(string message)
        {
            if (_lockedToastCd > 0f) return;
            _lockedToastCd = 1.6f;
            GameplayHudToast.Show(this, message, 1.8f, 190);
        }

        void FirePistol()
        {
            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;
            if (gb == null || gb.bulletPrefab == null) return;
            float facingDir = (_move != null && _move.IsFacingRight) ? 1f : -1f;
            Vector2 muzzle = BodyCenter + new Vector2(facingDir * pistolMuzzleOffsetX, pistolMuzzleOffsetY);
            var inst = Object.Instantiate(gb.bulletPrefab, muzzle, Quaternion.identity);
            var b = inst.GetComponent<Bullet>();
            if (b == null) b = inst.AddComponent<Bullet>();
            b.Launch(new Vector2(facingDir, 0f), gameObject, pistolDamage * pistolBulletMul);
        }

        IEnumerator DoBlast()
        {
            _busy = true;
            KCdRemaining = kCooldown;
            yield return new WaitForSeconds(0.08f);
            SpawnAttackCircleFx(BodyCenter, kSkillRadius);
            // K 墨爆 = baseAttack × 武器倍率 × 动作倍率
            DealCircle(kSkillRadius, kSkillActionMul * GetWeaponBaseMul(), knockbackK);
            yield return new WaitForSeconds(0.2f);
            _busy = false;
        }

        // ───────────────── 枪 L 蓄力系统 ─────────────────

        void StartCharge()
        {
            _charging = true;
            _chargeT = 0f;
            _lastChargeLv = 0;
            EnsureMuzzleFx();
            // 注意：CD 在 ReleaseCharge 成功发射时才设置；这样手滑取消不消耗 8s 大招
        }

        void TickCharge(float dt)
        {
            _chargeT += dt;
            int lv = GetChargeLevel(_chargeT);

            // 跨等级瞬间：能量球闪一下 + 屏幕轻震，体现「咚！咚！」节奏感
            if (lv > _lastChargeLv && lv > 0)
            {
                _lastChargeLv = lv;
                BrushSpirit.Core.HitFeedback.Light();
            }

            // 持续 shake：amp 随等级阶梯式提升，Lv3 叠正弦"颤"，做出「过载」感
            //   Lv0 — 起势微震   Lv1 ≈ Medium   Lv2 ≈ Heavy   Lv3 ≈ Huge + 颤动
            // 用 SetContinuousShake 避免被 max-merge 卡住，sin 部分才能真正显现
            if (BrushSpirit.Core.CameraFollowPlayer2D.Active != null)
            {
                float amp;
                if (lv >= 3)
                    amp = 0.105f + 0.045f * Mathf.Abs(Mathf.Sin(Time.time * 26f));
                else if (lv == 2)
                    amp = 0.075f;
                else if (lv == 1)
                    amp = 0.045f;
                else
                    amp = 0.020f;
                BrushSpirit.Core.CameraFollowPlayer2D.Active.SetContinuousShake(amp);
            }

            UpdateMuzzleFx(lv);

            if (_chargeT >= chargeMaxTime) ReleaseCharge();
        }

        void ReleaseCharge()
        {
            if (!_charging) return;
            int lv = GetChargeLevel(_chargeT);
            _charging = false;
            _chargeT = 0f;
            DestroyMuzzleFx();

            if (lv <= 0) return; // 没蓄够就松开 → 取消，不消耗 CD
            StartCoroutine(DoPistolUltimateAtLevel(lv));
        }

        void CancelCharge()
        {
            _charging = false;
            _chargeT = 0f;
            _lastChargeLv = 0;
            DestroyMuzzleFx();
        }

        int GetChargeLevel(float t)
        {
            if (t >= chargeLv3Time) return 3;
            if (t >= chargeLv2Time) return 2;
            if (t >= chargeMinTime) return 1;
            return 0;
        }

        /// <summary>
        /// 蓄力释放：朝面朝方向打出对应等级的墨痕巨弹。
        /// Lv1 = 基础 ult（与剑 L burst 持平）；Lv3 = 近乎翻倍 + 穿透 +2 + AOE ×1.5。
        /// </summary>
        IEnumerator DoPistolUltimateAtLevel(int lv)
        {
            _busy = true;
            LCdRemaining = lCooldown;

            int idx = Mathf.Clamp(lv - 1, 0, 2);
            float dmgScale = chargeDamageMuls[idx];
            int piercesBonus = chargePiercesBonus[idx];
            float scaleByLv = chargeScaleMuls[idx];
            float aoeByLv = chargeAoeRadiusMuls[idx];

            // 蓄力已经吃掉了大部分前摇，发射几乎瞬发
            yield return new WaitForSeconds(0.06f);
            if (_anim != null) _anim.SetTrigger(kAttack);

            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;
            if (gb != null && gb.bulletPrefab != null)
            {
                float facingDir = (_move != null && _move.IsFacingRight) ? 1f : -1f;
                Vector2 muzzle = BodyCenter + new Vector2(facingDir * pistolMuzzleOffsetX, pistolMuzzleOffsetY);
                var inst = Object.Instantiate(gb.bulletPrefab, muzzle, Quaternion.identity);
                inst.transform.localScale = Vector3.one * (pistolUltScaleMul * scaleByLv);

                var b = inst.GetComponent<Bullet>();
                if (b == null) b = inst.AddComponent<Bullet>();
                b.maxPierces = pistolUltPierces + piercesBonus;
                b.aoeRadiusOnHit = pistolUltAoeRadius * aoeByLv;
                b.aoeDamageMul = pistolUltAoeMul;
                b.hurtboxMaskForAoe = hurtboxMask;
                b.speed = pistolUltSpeed;
                b.lifetime = pistolUltLife;
                b.knockbackImpulse = pistolUltKnockback;
                b.hitFeedbackLevel = 2;
                b.ignoreTerrain = true; // 巨弹穿山贯石，不被地形吃掉

                // 蓄力等级越高，颜色越亮：暗紫 → 紫粉 → 暖金
                var sr = inst.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (lv >= 3)      sr.color = new Color(1.00f, 0.85f, 0.35f);
                    else if (lv == 2) sr.color = new Color(0.85f, 0.45f, 0.95f);
                    else              sr.color = new Color(0.22f, 0.10f, 0.32f);
                }

                float dmg = pistolDamage * pistolBulletMul * pistolUltMul * dmgScale;
                b.Launch(new Vector2(facingDir, 0f), gameObject, dmg);

                // 发射反馈分级：Lv1=Medium，Lv2=Heavy，Lv3=Huge
                if (lv >= 3)      BrushSpirit.Core.HitFeedback.Huge();
                else if (lv == 2) BrushSpirit.Core.HitFeedback.Heavy();
                else              BrushSpirit.Core.HitFeedback.Medium();
            }

            yield return new WaitForSeconds(0.20f);
            _busy = false;
        }

        // 枪口能量球：程序化贴图，跟随面朝方向，蓄力时尺寸/颜色随等级变化
        void EnsureMuzzleFx()
        {
            if (_chargeMuzzleFx != null) return;
            _chargeMuzzleFx = new GameObject("PistolChargeOrb");
            _chargeMuzzleFx.transform.SetParent(transform, false);
            _chargeMuzzleFx.transform.localPosition = new Vector3(pistolMuzzleOffsetX, pistolMuzzleOffsetY, 0f);
            _chargeMuzzleSr = _chargeMuzzleFx.AddComponent<SpriteRenderer>();
            _chargeMuzzleSr.sprite = GetChargeOrbSprite();
            _chargeMuzzleSr.sortingOrder = 70;
            _chargeMuzzleFx.transform.localScale = Vector3.one * 0.1f;
        }

        void UpdateMuzzleFx(int lv)
        {
            if (_chargeMuzzleFx == null || _chargeMuzzleSr == null) return;
            float facingDir = (_move != null && _move.IsFacingRight) ? 1f : -1f;
            _chargeMuzzleFx.transform.localPosition = new Vector3(facingDir * pistolMuzzleOffsetX, pistolMuzzleOffsetY, 0f);

            float t = Mathf.Clamp01(_chargeT / chargeLv3Time);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
            float scale = Mathf.Lerp(0.18f, 0.62f, t) * (1f + 0.10f * pulse);
            _chargeMuzzleFx.transform.localScale = Vector3.one * scale;

            Color baseCol;
            if (lv >= 3)      baseCol = new Color(1.00f, 0.92f, 0.35f); // 暖金
            else if (lv == 2) baseCol = new Color(0.95f, 0.50f, 1.00f); // 紫粉
            else if (lv == 1) baseCol = new Color(0.55f, 0.30f, 0.92f); // 暗紫
            else              baseCol = new Color(0.30f, 0.18f, 0.55f); // 未到 Lv1，更暗
            float alpha = Mathf.Lerp(0.55f, 1.0f, t) * (0.85f + 0.18f * pulse);
            _chargeMuzzleSr.color = new Color(baseCol.r, baseCol.g, baseCol.b, alpha);
        }

        void DestroyMuzzleFx()
        {
            if (_chargeMuzzleFx != null) Destroy(_chargeMuzzleFx);
            _chargeMuzzleFx = null;
            _chargeMuzzleSr = null;
        }

        // 程序化生成一颗带软光晕的能量球（无外部美术依赖）
        static Sprite s_chargeOrbSprite;
        static Sprite GetChargeOrbSprite()
        {
            if (s_chargeOrbSprite != null) return s_chargeOrbSprite;
            const int size = 96;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float maxR = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float u = Mathf.Clamp01(d / maxR);
                // 软光晕：中心实白，边缘 Gaussian 衰减
                float a = Mathf.Pow(1f - u, 2.2f);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            s_chargeOrbSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return s_chargeOrbSprite;
        }

        // ───────────────────────────────────────────────────

        IEnumerator DoTripleSlash()
        {
            _busy = true;
            LCdRemaining = lCooldown;
            if (_anim != null) _anim.SetTrigger(kCombo);
            // L 三连：每段独立结算 = baseAttack × 武器倍率 × 动作倍率
            float lDmgMul = lSkillActionMul * GetWeaponBaseMul();
            float lRangeMul = GetWeaponRangeMul() * 1.08f; // 三连略大
            for (int i = 0; i < 3; i++)
            {
                DealMeleeWide(lDmgMul, new Color(1f, 0.45f, 0.15f, 0.72f), 0.24f, lRangeMul, knockbackJL);
                yield return new WaitForSeconds(lSlashInterval);
            }

            _busy = false;
        }

        // ── 武器系数辅助方法 ──
        public float GetWeaponBaseMul()
        {
            switch (CurrentWeapon)
            {
                case WeaponMode.Bare:   return bareDamageMul;
                case WeaponMode.Sword:  return swordDamageMul;
                case WeaponMode.Pistol: return pistolMeleeMul;
            }
            return 1f;
        }

        float GetWeaponRangeMul()
        {
            switch (CurrentWeapon)
            {
                case WeaponMode.Bare:  return bareRangeMul;
                case WeaponMode.Sword: return swordRangeMul;
            }
            return 1f;
        }

        float GetComboStepMul(int idx)
        {
            float[] muls = CurrentWeapon == WeaponMode.Sword ? swordComboMuls : bareComboMuls;
            if (muls == null || muls.Length == 0) return 1f;
            return muls[Mathf.Clamp(idx, 0, muls.Length - 1)];
        }

        /// <summary>
        /// 朝向方向的攻击盒：跟随主角面朝方向，只覆盖身前。
        /// 取消了原先的彩色 hitbox 可视化（人物本身已有挥击动画），仅保留命中判定。
        /// fxColor / fxLife 参数保留以兼容旧调用签名。
        /// </summary>
        void DealMeleeWide(float damageMultiplier, Color fxColor, float fxLife, float fxSizeMul = 1f,
            float knockbackImpulse = 0f)
        {
            CacheRefs();
            if (_stats == null) return;
            float facingDir = (_move != null && _move.IsFacingRight) ? 1f : -1f;
            Vector2 center = BodyCenter + new Vector2(facingDir * attackOffset, 0.12f);
            float w = attackBoxSize.x + meleeWidthExtra;
            float h = attackBoxSize.y + 0.25f;
            Vector2 size = new Vector2(w, h) * fxSizeMul;
            var hits = Physics2D.OverlapBoxAll(center, size, 0f, hurtboxMask, Mathf.NegativeInfinity, Mathf.Infinity);
            ApplyDamage(hits, _stats.GetAttackPower() * damageMultiplier, knockbackImpulse);
        }

        void DealCircle(float radius, float damageMultiplier, float knockbackImpulse)
        {
            CacheRefs();
            if (_stats == null) return;
            var hits = Physics2D.OverlapCircleAll(BodyCenter, radius, hurtboxMask, Mathf.NegativeInfinity, Mathf.Infinity);
            ApplyDamage(hits, _stats.GetAttackPower() * damageMultiplier, knockbackImpulse);
        }

        void ApplyDamage(Collider2D[] hits, float damage, float knockbackImpulseX)
        {
            if (damage <= 0f || hits == null) return;
            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;
            bool anyHit = false;
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.gameObject == gameObject) continue;
                if (h.transform.IsChildOf(transform)) continue;

                var d = h.GetComponent<IDamageable>();
                if (d == null)
                    d = h.GetComponentInParent<IDamageable>();
                if (d == null) continue;
                float dmgMul = HeartSceneEnvironment.GetOutgoingDamageMultiplierOnTarget(h.bounds.center);
                float focusMul = PlayerStats.OutgoingDamageMul(); // 完美闪避奖励
                d.TakeDamage(damage * dmgMul * focusMul, gameObject, knockbackImpulseX);
                anyHit = true;

                if (gb != null && gb.attackHitPrefab != null)
                {
                    var inst = Object.Instantiate(gb.attackHitPrefab);
                    inst.transform.position = h.bounds.center;
                    var sr = inst.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 60;
                    Object.Destroy(inst, 0.5f);
                }
            }

            // 命中反馈：根据本次伤害量级决定 hit-stop + 屏幕震动强度
            //   连击末段 / 剑伤害 / 技能 → Heavy；普通普攻 → Light；范围技 K → Huge
            if (anyHit) TriggerHitFeedbackFor(damage);
        }

        /// <summary>
        /// 根据本次伤害大小映射到命中反馈强度。阈值用「主属性 × 期望倍率」估，调起来直观。
        /// </summary>
        void TriggerHitFeedbackFor(float damage)
        {
            if (_stats == null) { BrushSpirit.Core.HitFeedback.Light(); return; }
            float baseAtk = Mathf.Max(1f, _stats.GetAttackPower());
            float r = damage / baseAtk; // ≈ 武器倍率 × 动作倍率
            if      (r >= 2.2f) BrushSpirit.Core.HitFeedback.Huge();   // K 墨爆等
            else if (r >= 1.3f) BrushSpirit.Core.HitFeedback.Heavy();  // 剑末段 / L 三连
            else if (r >= 1.0f) BrushSpirit.Core.HitFeedback.Medium(); // 剑前段 / 拳末段
            else                BrushSpirit.Core.HitFeedback.Light();  // 拳前段
        }

        static void SpawnAttackBoxFx(Vector2 center, Vector2 worldSize, float life, Color color)
        {
            var go = new GameObject("AttackBoxFX");
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BrushSpirit.GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = color;
            sr.sortingOrder = 58;
            go.transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
            Object.Destroy(go, life);
        }

        public static void SpawnAttackCircleFx(Vector2 center, float radius)
        {
            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;

            // 优先：实例化带动画的预制体（含 Animator + AutoDestroy）
            if (gb != null && gb.attackCirclePrefab != null)
            {
                var inst = Object.Instantiate(gb.attackCirclePrefab);
                inst.transform.position = center;
                var sr = inst.GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    Vector2 natural = sr.sprite.bounds.size;
                    float sx = (radius * 2f) / Mathf.Max(0.01f, natural.x);
                    float sy = (radius * 2f) / Mathf.Max(0.01f, natural.y);
                    inst.transform.localScale = new Vector3(sx, sy, 1f);
                    sr.sortingOrder = 54;
                }
                else
                {
                    inst.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
                }
                Object.Destroy(inst, 1.0f); // 即使 Prefab 没挂 AutoDestroy 也能消失
                return;
            }

            // 次选：用单张 Sprite 静态显示
            var go = new GameObject("AttackCircleFX");
            go.transform.position = center;
            var fallbackSr = go.AddComponent<SpriteRenderer>();
            if (gb != null && gb.attackCircleSprite != null)
            {
                fallbackSr.sprite = gb.attackCircleSprite;
                fallbackSr.color = Color.white;
            }
            else
            {
                fallbackSr.sprite = BrushSpirit.GameRuntimeBootstrap.CreatePlaceholderSprite();
                fallbackSr.color = new Color(0.55f, 0.85f, 1f, 0.42f);
            }
            fallbackSr.sortingOrder = 54;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            Object.Destroy(go, 0.4f);
        }
    }
}
