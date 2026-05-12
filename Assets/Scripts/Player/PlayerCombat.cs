using System.Collections;
using BrushSpirit.Core;
using BrushSpirit.LevelFlow;
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
        public WeaponMode CurrentWeapon { get; private set; } = WeaponMode.Sword;

        [Header("手枪")]
        public float pistolFireCooldown = 0.35f;
        public float pistolDamage = 10f;
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

            // 1/2/3 切换武器
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetWeapon(WeaponMode.Bare);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) SetWeapon(WeaponMode.Sword);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) SetWeapon(WeaponMode.Pistol);

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
                else
                {
                    float mult = _comboIndex == 0 ? 1f : _comboIndex == 1 ? 1.1f : 1.25f;
                    DealMeleeWide(mult, new Color(1f, 0.92f, 0.55f, 0.58f), 0.12f, 1f, knockbackJL);
                    _comboIndex = (_comboIndex + 1) % 3;
                    if (_anim != null) _anim.SetTrigger(kAttack);
                }
            }

            if (_busy) return;

            if (Input.GetKeyDown(KeyCode.K) && KCdRemaining <= 0f)
            {
                StartCoroutine(DoBlast());
                return;
            }

            if (Input.GetKeyDown(KeyCode.L) && LCdRemaining <= 0f)
                StartCoroutine(DoTripleSlash());
        }

        public void SetWeapon(WeaponMode mode)
        {
            CurrentWeapon = mode;
            var gb = BrushSpirit.GameRuntimeBootstrap.Instance;
            if (_anim != null && gb != null)
            {
                RuntimeAnimatorController ctl = null;
                switch (mode)
                {
                    case WeaponMode.Bare:   ctl = gb.bareController; break;
                    case WeaponMode.Sword:  ctl = gb.playerController; break;
                    case WeaponMode.Pistol: ctl = gb.pistolController; break;
                }
                if (ctl != null) _anim.runtimeAnimatorController = ctl;
            }
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
            b.Launch(new Vector2(facingDir, 0f), gameObject, pistolDamage);
        }

        IEnumerator DoBlast()
        {
            _busy = true;
            KCdRemaining = kCooldown;
            yield return new WaitForSeconds(0.08f);
            SpawnAttackCircleFx(BodyCenter, kSkillRadius);
            DealCircle(kSkillRadius, 1.5f, knockbackK);
            yield return new WaitForSeconds(0.2f);
            _busy = false;
        }

        IEnumerator DoTripleSlash()
        {
            _busy = true;
            LCdRemaining = lCooldown;
            if (_anim != null) _anim.SetTrigger(kCombo);
            for (int i = 0; i < 3; i++)
            {
                DealMeleeWide(1f, new Color(1f, 0.45f, 0.15f, 0.72f), 0.24f, 1.08f, knockbackJL);
                yield return new WaitForSeconds(0.22f);
            }

            _busy = false;
        }

        /// <summary>朝向方向的攻击盒：跟随主角面朝方向，只覆盖身前。</summary>
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
            SpawnAttackBoxFx(center, size, fxLife, fxColor);
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
                d.TakeDamage(damage * dmgMul, gameObject, knockbackImpulseX);

                if (gb != null && gb.attackHitPrefab != null)
                {
                    var inst = Object.Instantiate(gb.attackHitPrefab);
                    inst.transform.position = h.bounds.center;
                    var sr = inst.GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.sortingOrder = 60;
                    Object.Destroy(inst, 0.5f);
                }
            }
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

        static void SpawnAttackCircleFx(Vector2 center, float radius)
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
