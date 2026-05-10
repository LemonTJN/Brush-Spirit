using BrushSpirit.Items;
using UnityEngine;

namespace BrushSpirit.Player
{
    public class EquipmentHolder : MonoBehaviour
    {
        public EquipmentData weapon;
        public EquipmentData armor;

        readonly System.Text.StringBuilder _log = new System.Text.StringBuilder();

        public int GetAttackBonus()
        {
            int a = weapon != null ? weapon.attackBonus : 0;
            int b = armor != null ? armor.attackBonus : 0;
            return a + b;
        }

        public int GetHpBonus()
        {
            int a = weapon != null ? weapon.hpBonus : 0;
            int b = armor != null ? armor.hpBonus : 0;
            return a + b;
        }

        /// <summary>仅当新装备在对应槽位上「有效总分」更高时替换。</summary>
        public bool TryEquip(EquipmentData next)
        {
            if (next == null) return false;

            EquipmentData current = next.slot == EquipmentSlot.Weapon ? weapon : armor;

            int curScore = SlotScore(current, next.slot);
            int nextScore = SlotScore(next, next.slot);
            if (nextScore <= curScore) return false;

            if (next.slot == EquipmentSlot.Weapon) weapon = next;
            else armor = next;

            var stats = GetComponent<PlayerStats>();
            stats?.RecomputeFromEquipment();
            Debug.Log($"装备更新: {next.displayName}");
            return true;
        }

        static int SlotScore(EquipmentData e, EquipmentSlot slot)
        {
            if (e == null) return 0;
            return slot == EquipmentSlot.Weapon ? e.attackBonus * 10 + e.hpBonus : e.hpBonus * 10 + e.attackBonus;
        }

        public string GetLoadoutText()
        {
            _log.Clear();
            _log.Append("武器: ").Append(weapon != null ? weapon.displayName : "无");
            _log.Append(" | 衣: ").Append(armor != null ? armor.displayName : "无");
            return _log.ToString();
        }
    }
}
