using UnityEngine;

namespace BrushSpirit.Core
{
    public interface IDamageable
    {
        /// <param name="knockbackImpulseX">水平击退冲量（世界单位·秒量级，由受击方按衰减施加位移）。0 表示无击退。</param>
        void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f);
    }
}
