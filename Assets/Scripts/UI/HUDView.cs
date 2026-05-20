using BrushSpirit.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BrushSpirit.UI
{
    public class HUDView : MonoBehaviour
    {
        [Tooltip("优先使用 Slider；部分引擎上动态创建的 Filled Image 的 fillAmount 可能不刷新")]
        public Slider healthSlider;
        public Image healthFill;
        public Text levelText;
        public Text jAttackText;
        public Text kCooldownText;
        public Text lCooldownText;
        public Text dashCooldownText;

        PlayerStats _stats;
        PlayerCombat _combat;
        PlayerMovement _move;

        public void Bind(PlayerStats stats, PlayerCombat combat, PlayerMovement move = null)
        {
            _stats = stats;
            _combat = combat;
            _move = move;
            if (_stats == null) return;
            if (_stats.OnHealthChanged == null)
                _stats.OnHealthChanged = new UnityEngine.Events.UnityEvent<float, float>();
            if (_stats.OnLevelChanged == null)
                _stats.OnLevelChanged = new UnityEngine.Events.UnityEvent<int>();
            _stats.OnHealthChanged.AddListener(OnHp);
            _stats.OnLevelChanged.AddListener(OnLv);
            OnHp(_stats.CurrentHp, _stats.MaxHp);
            OnLv(_stats.Level);
        }

        void OnDestroy()
        {
            if (_stats == null) return;
            if (_stats.OnHealthChanged != null)
                _stats.OnHealthChanged.RemoveListener(OnHp);
            if (_stats.OnLevelChanged != null)
                _stats.OnLevelChanged.RemoveListener(OnLv);
        }

        void OnHp(float cur, float max)
        {
            ApplyHealthVisual(cur, max);
        }

        void ApplyHealthVisual(float cur, float max)
        {
            if (max <= 0f) return;
            float r = Mathf.Clamp01(cur / max);
            if (healthSlider != null)
                healthSlider.value = r;
            if (healthFill != null)
                healthFill.fillAmount = r;
        }

        void OnLv(int lv)
        {
            if (levelText != null)
                levelText.text = "Lv." + lv;
        }

        void LateUpdate()
        {
            var st = PlayerStats.Active != null ? PlayerStats.Active : _stats;
            if (st != null && st.MaxHp > 0f)
                ApplyHealthVisual(st.CurrentHp, st.MaxHp);
        }

        void Update()
        {
            if (jAttackText != null)
                jAttackText.text = BuildJAttackLine();
            if (_combat == null) return;
            if (kCooldownText != null)
                kCooldownText.text = _combat.KCdRemaining > 0f ? $"墨爆 K: {_combat.KCdRemaining:0.0}s" : "墨爆 K: 就绪";
            if (lCooldownText != null)
                lCooldownText.text = _combat.LCdRemaining > 0f ? $"三连 L: {_combat.LCdRemaining:0.0}s" : "三连 L: 就绪";
            if (dashCooldownText != null && _move != null)
                dashCooldownText.text = _move.DashCdRemaining > 0f ? $"冲刺 双击←/→: {_move.DashCdRemaining:0.0}s" : "冲刺 双击←/→: 就绪";
        }

        string BuildJAttackLine()
        {
            string formTag;
            string action;
            string mulTag;
            if (_combat == null)
            {
                formTag = "拳";
                action = "无冷却（三段循环）";
                mulTag = "";
            }
            else
            {
                switch (_combat.CurrentWeapon)
                {
                    case PlayerCombat.WeaponMode.Pistol:
                        formTag = "枪";
                        action = "J 射击";
                        break;
                    case PlayerCombat.WeaponMode.Sword:
                        formTag = "剑";
                        action = "无冷却（三段循环）";
                        break;
                    default:
                        formTag = "拳";
                        action = "无冷却（三段循环）";
                        break;
                }
                mulTag = $" ×{_combat.GetWeaponBaseMul():0.00}";
            }

            string swordTag = PlayerCombat.HasSword ? "2 剑✓" : "2 剑✗";
            string pistolTag = PlayerCombat.HasPistol ? "3 枪✓" : "3 枪✗";
            return $"{formTag}·普攻 J{mulTag}：{action}  [1 拳  {swordTag}  {pistolTag}]";
        }
    }
}
