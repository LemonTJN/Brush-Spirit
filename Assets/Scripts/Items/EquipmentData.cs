using UnityEngine;

namespace BrushSpirit.Items
{
    public enum EquipmentSlot
    {
        Weapon,
        Armor
    }

    [CreateAssetMenu(fileName = "Equipment", menuName = "Brush Spirit/Equipment")]
    public class EquipmentData : ScriptableObject
    {
        public string displayName = "装备";
        public EquipmentSlot slot = EquipmentSlot.Weapon;
        public int attackBonus;
        public int hpBonus;
        public bool isColorGear;
        public Color visualTint = Color.white;
    }
}
