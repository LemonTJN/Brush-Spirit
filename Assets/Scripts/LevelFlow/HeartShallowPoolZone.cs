using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>月牙浅池：区内水平目标速度按系数衰减（由 PlayerMovement 查询）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartShallowPoolZone : MonoBehaviour
    {
        [Range(0.4f, 1f)] public float horizontalVelocityRetain = 0.78f;

        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }
    }
}
