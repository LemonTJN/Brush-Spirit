using System.Collections;
using BrushSpirit.Core;
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
        int _comboIndex;
        bool _busy;

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
        }

        void Update()
        {
            if (KCdRemaining > 0f) KCdRemaining -= Time.deltaTime;
            if (LCdRemaining > 0f) LCdRemaining -= Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.J))
            {
                float mult = _comboIndex == 0 ? 1f : _comboIndex == 1 ? 1.1f : 1.25f;
                DealMeleeWide(mult, new Color(1f, 0.92f, 0.55f, 0.58f), 0.12f, 1f, knockbackJL);
                _comboIndex = (_comboIndex + 1) % 3;
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

        IEnumerator DoBlast()
        {
            _busy = true;
            KCdRemaining = kCooldown;
            yield return new WaitForSeconds(0.08f);
            SpawnAttackCircleFx(transform.position, kSkillRadius);
            DealCircle(kSkillRadius, 1.5f, knockbackK);
            yield return new WaitForSeconds(0.2f);
            _busy = false;
        }

        IEnumerator DoTripleSlash()
        {
            _busy = true;
            LCdRemaining = lCooldown;
            for (int i = 0; i < 3; i++)
            {
                DealMeleeWide(1f, new Color(1f, 0.45f, 0.15f, 0.72f), 0.24f, 1.08f, knockbackJL);
                yield return new WaitForSeconds(0.22f);
            }

            _busy = false;
        }

        /// <summary>以角色为中心的水平宽盒：同时覆盖左右，解决单向判定打不中问题。</summary>
        void DealMeleeWide(float damageMultiplier, Color fxColor, float fxLife, float fxSizeMul = 1f,
            float knockbackImpulse = 0f)
        {
            CacheRefs();
            if (_stats == null) return;
            Vector2 center = (Vector2)transform.position + new Vector2(0f, 0.12f);
            float w = attackOffset * 2f + attackBoxSize.x + meleeWidthExtra;
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
            var hits = Physics2D.OverlapCircleAll(transform.position, radius, hurtboxMask, Mathf.NegativeInfinity, Mathf.Infinity);
            ApplyDamage(hits, _stats.GetAttackPower() * damageMultiplier, knockbackImpulse);
        }

        void ApplyDamage(Collider2D[] hits, float damage, float knockbackImpulseX)
        {
            if (damage <= 0f || hits == null) return;
            foreach (var h in hits)
            {
                if (h == null) continue;
                if (h.gameObject == gameObject) continue;
                if (h.transform.IsChildOf(transform)) continue;

                var d = h.GetComponent<IDamageable>();
                if (d == null)
                    d = h.GetComponentInParent<IDamageable>();
                d?.TakeDamage(damage, gameObject, knockbackImpulseX);
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
            var go = new GameObject("AttackCircleFX");
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BrushSpirit.GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.55f, 0.85f, 1f, 0.42f);
            sr.sortingOrder = 54;
            go.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            Object.Destroy(go, 0.14f);
        }
    }
}
