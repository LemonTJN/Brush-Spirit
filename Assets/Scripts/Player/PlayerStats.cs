using System.Collections;
using BrushSpirit.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace BrushSpirit.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        /// <summary>当前场景中的玩家属性（用于敌人结算伤害，避免 Tag 指错物体导致 UI 不更新）。</summary>
        public static PlayerStats Active { get; private set; }

        [Header("基础成长（随等级）")]
        public int baseMaxHp = 100;
        // 14 是相对 10 提升 40%，配合 boss 同比 +45% HP，让杀小怪变快、boss 节奏不变。
        public int baseAttack = 14;

        [Header("完美闪避 / 专注时间（Perfect Dodge）")]
        [Tooltip("dash 期间挡下一次攻击触发：本段时间内玩家伤害倍率")]
        public float focusDamageMul = 1.5f;
        [Tooltip("专注时间持续秒数，参考《Returnal》的 Adrenaline / 尼尔的 Perfect Evade")]
        public float focusDuration = 1.5f;

        float _focusT;
        Coroutine _focusVisualRoutine;
        public bool IsInFocus => _focusT > 0f;

        /// <summary>外部攻击者拿这个倍率乘到伤害上（PlayerCombat / Bullet 会读）。</summary>
        public static float OutgoingDamageMul()
        {
            return (Active != null && Active.IsInFocus) ? Active.focusDamageMul : 1f;
        }

        [Header("经验表（累计击杀或固定值）")]
        public int[] xpPerLevel = { 50, 80, 120, 180, 260 };

        public int Level { get; private set; } = 1;
        public int CurrentXp { get; private set; }
        public float CurrentHp { get; private set; }
        public float MaxHp { get; private set; }

        public UnityEvent<float, float> OnHealthChanged;
        public UnityEvent<int, int> OnXpChanged;
        public UnityEvent<int> OnLevelChanged;

        EquipmentHolder _equipment;
        SpriteRenderer _bodySr;
        Animator _anim;
        PlayerMovement _move;
        Color _baseColor = Color.white;
        static readonly int kHit = Animator.StringToHash("Hit");
        static readonly int kDeath = Animator.StringToHash("Death");
        Coroutine _hitFlashRoutine;
        bool _deathSequenceStarted;

        void Awake()
        {
            OnHealthChanged ??= new UnityEvent<float, float>();
            OnXpChanged ??= new UnityEvent<int, int>();
            OnLevelChanged ??= new UnityEvent<int>();
            _equipment = GetComponent<EquipmentHolder>();
            _bodySr = GetComponent<SpriteRenderer>();
            if (_bodySr != null) _baseColor = _bodySr.color;
            _anim = GetComponent<Animator>();
            _move = GetComponent<PlayerMovement>();
            RecomputeFromEquipment();
        }

        void OnEnable()
        {
            Active = this;
        }

        void OnDisable()
        {
            if (Active == this) Active = null;
        }

        public void RecomputeFromEquipment()
        {
            int bonusHp = _equipment != null ? _equipment.GetHpBonus() : 0;
            MaxHp = baseMaxHp + bonusHp + (Level - 1) * 8;
            if (CurrentHp <= 0f || CurrentHp > MaxHp)
                CurrentHp = MaxHp;
            NotifyHealthChanged();
        }

        public float GetAttackPower()
        {
            int bonusAtk = _equipment != null ? _equipment.GetAttackBonus() : 0;
            return baseAttack + bonusAtk + (Level - 1) * 2;
        }

        public void AddXp(int amount)
        {
            if (amount <= 0) return;
            CurrentXp += amount;
            TryLevelUp();
            OnXpChanged?.Invoke(CurrentXp, XpToNext());
        }

        int XpToNext()
        {
            if (Level - 1 >= xpPerLevel.Length) return int.MaxValue;
            return xpPerLevel[Level - 1];
        }

        void TryLevelUp()
        {
            while (Level - 1 < xpPerLevel.Length)
            {
                int need = xpPerLevel[Level - 1];
                if (CurrentXp < need) break;
                CurrentXp -= need;
                Level++;
                baseMaxHp += 8;
                baseAttack += 2;
                OnLevelChanged?.Invoke(Level);
                RecomputeFromEquipment();
                CurrentHp = MaxHp;
                NotifyHealthChanged();
            }
        }

        void Update()
        {
            if (_focusT > 0f)
            {
                _focusT -= Time.deltaTime;
                if (_focusT <= 0f && _focusVisualRoutine == null && _bodySr != null)
                    _bodySr.color = _baseColor;
            }
        }

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            if (amount <= 0f) return;

            // Dash 无敌帧：冲刺期间不仅免伤，还触发「完美闪避 → 专注时间」（Returnal / 尼尔自动人形）
            if (_move == null) _move = GetComponent<PlayerMovement>();
            if (_move != null && _move.IsDashing)
            {
                TriggerPerfectDodge();
                return;
            }

            CurrentHp -= amount;
            if (CurrentHp < 0f) CurrentHp = 0f;
            NotifyHealthChanged();
            if (_bodySr != null)
            {
                if (_hitFlashRoutine != null)
                    StopCoroutine(_hitFlashRoutine);
                _hitFlashRoutine = StartCoroutine(HitFlashRoutine());
            }

            // 玩家挨打反馈：屏幕轻晃 + 极短顿帧，让"被命中"也有重量感
            BrushSpirit.Core.HitFeedback.Medium();

            if (CurrentHp <= 0f && !_deathSequenceStarted)
            {
                _deathSequenceStarted = true;
                if (_anim != null) _anim.SetTrigger(kDeath);
                var mv = GetComponent<PlayerMovement>();
                if (mv != null) mv.enabled = false;
                var combat = GetComponent<PlayerCombat>();
                if (combat != null) combat.enabled = false;
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.velocity = Vector2.zero;
                StartCoroutine(PlayerDeathPresenter.PlayThenReload(SceneManager.GetActiveScene().name));
            }
            else if (_anim != null && CurrentHp > 0f)
            {
                _anim.SetTrigger(kHit);
            }
        }

        /// <summary>
        /// 完美闪避：在 dash 期间承受伤害 → 进入专注时间。
        ///   - 玩家全部攻击 ×focusDamageMul 持续 focusDuration 秒
        ///   - 命中反馈 Heavy（屏幕闪 + 短暂顿帧），有「绝技触发」的仪式感
        ///   - 玩家身体金色脉冲 + 一圈外扩光环 + Toast「完美闪避」
        /// 设计参考《Returnal》Adrenaline、《尼尔自动人形》Perfect Evade。
        /// </summary>
        void TriggerPerfectDodge()
        {
            _focusT = focusDuration;
            BrushSpirit.Core.HitFeedback.Heavy();
            BrushSpirit.UI.GameplayHudToast.Show(this, "完美闪避！  伤害 ×" + focusDamageMul.ToString("0.0"), 1.3f, 220);
            SpawnFocusRing();

            if (_focusVisualRoutine != null) StopCoroutine(_focusVisualRoutine);
            _focusVisualRoutine = StartCoroutine(FocusBodyTintRoutine());
        }

        // 玩家身体金色脉冲：从 dash 解锁瞬间到 focus 结束，提示「这段时间你的剑会更利」
        IEnumerator FocusBodyTintRoutine()
        {
            if (_bodySr == null) { _focusVisualRoutine = null; yield break; }
            Color hot = new Color(1.0f, 0.85f, 0.30f);
            while (_focusT > 0f)
            {
                // 受伤红闪期间让位，闪完后继续脉冲
                if (_hitFlashRoutine == null)
                {
                    float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 14f);
                    _bodySr.color = Color.Lerp(_baseColor, hot, 0.45f + 0.45f * pulse);
                }
                yield return null;
            }
            if (_bodySr != null && _hitFlashRoutine == null) _bodySr.color = _baseColor;
            _focusVisualRoutine = null;
        }

        // 触发瞬间的金色光环：从玩家中心向外扩散并淡出，相当于「时间凝固」的视觉锚点
        void SpawnFocusRing()
        {
            var go = new GameObject("FocusRing");
            go.transform.position = transform.position;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetFocusRingSprite();
            sr.color = new Color(1f, 0.85f, 0.30f, 0.85f);
            sr.sortingOrder = 70;
            go.transform.localScale = Vector3.one * 0.6f;
            StartCoroutine(AnimateFocusRing(go.transform, sr));
        }

        IEnumerator AnimateFocusRing(Transform t, SpriteRenderer sr)
        {
            float dur = 0.42f, time = 0f;
            Vector3 start = Vector3.one * 0.6f;
            Vector3 end   = Vector3.one * 3.2f;
            while (time < dur && t != null)
            {
                time += Time.deltaTime;
                float u = Mathf.Clamp01(time / dur);
                t.localScale = Vector3.Lerp(start, end, u * u); // ease-out 感
                Color c = sr.color;
                c.a = Mathf.Lerp(0.85f, 0f, u);
                sr.color = c;
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // 程序化生成圆环纹理，避免依赖外部美术资源
        static Sprite s_focusRingSprite;
        static Sprite GetFocusRingSprite()
        {
            if (s_focusRingSprite != null) return s_focusRingSprite;
            const int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color[size * size];
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outer = size * 0.48f;
            float inner = size * 0.38f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                float a = 0f;
                if (d > inner && d < outer)
                {
                    // 圈内外做软边
                    float t = (d - inner) / (outer - inner); // 0..1
                    a = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(t * 2f - 1f));
                }
                pixels[y * size + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(pixels);
            tex.Apply();
            s_focusRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / 2.6f);
            return s_focusRingSprite;
        }

        IEnumerator HitFlashRoutine()
        {
            _bodySr.color = new Color(1f, 0.42f, 0.42f);
            yield return new WaitForSeconds(0.07f);
            _bodySr.color = _baseColor;
            _hitFlashRoutine = null;
        }

        public void HealFull()
        {
            CurrentHp = MaxHp;
            NotifyHealthChanged();
        }

        void NotifyHealthChanged()
        {
            if (OnHealthChanged != null)
                OnHealthChanged.Invoke(CurrentHp, MaxHp);
        }
    }
}
