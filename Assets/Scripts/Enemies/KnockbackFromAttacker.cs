using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    public static class KnockbackFromAttacker
    {
        /// <summary>击退方向：远离攻击者；若几乎重合则用玩家朝向决定左右。</summary>
        public static float SignedHorizontalDir(Transform target, GameObject attacker)
        {
            if (attacker == null || target == null) return 0f;
            float dx = target.position.x - attacker.transform.position.x;
            if (Mathf.Abs(dx) < 0.08f)
            {
                var pm = attacker.GetComponent<PlayerMovement>();
                if (pm != null)
                    return pm.IsFacingRight ? 1f : -1f;
            }

            return Mathf.Sign(dx);
        }
    }
}
