using BrushSpirit.Core;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>子物体触发器，把伤害转发给根上的 IDamageable。</summary>
    public class Hurtbox : MonoBehaviour, IDamageable
    {
        [SerializeField] MonoBehaviour damageReceiver;

        public void Configure(MonoBehaviour receiver) => damageReceiver = receiver;

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            if (damageReceiver is IDamageable d)
                d.TakeDamage(amount, attacker, knockbackImpulseX);
        }
    }
}
