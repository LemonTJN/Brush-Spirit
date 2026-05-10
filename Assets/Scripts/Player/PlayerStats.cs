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
        public int baseAttack = 10;

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
        Coroutine _hitFlashRoutine;

        void Awake()
        {
            OnHealthChanged ??= new UnityEvent<float, float>();
            OnXpChanged ??= new UnityEvent<int, int>();
            OnLevelChanged ??= new UnityEvent<int>();
            _equipment = GetComponent<EquipmentHolder>();
            _bodySr = GetComponent<SpriteRenderer>();
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

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            if (amount <= 0f) return;
            CurrentHp -= amount;
            if (CurrentHp < 0f) CurrentHp = 0f;
            NotifyHealthChanged();
            if (_bodySr != null)
            {
                if (_hitFlashRoutine != null)
                    StopCoroutine(_hitFlashRoutine);
                _hitFlashRoutine = StartCoroutine(HitFlashRoutine());
            }

            if (CurrentHp <= 0f)
            {
                PlayerRunCarry.ClearRun();
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        IEnumerator HitFlashRoutine()
        {
            Color original = _bodySr.color;
            _bodySr.color = new Color(1f, 0.42f, 0.42f);
            yield return new WaitForSeconds(0.07f);
            _bodySr.color = original;
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
