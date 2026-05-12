using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>墨镜地面：略增水平惯性（由 PlayerMovement 查询）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartInkMirrorZone : MonoBehaviour
    {
        void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null)
                col.isTrigger = true;
        }
    }
}
