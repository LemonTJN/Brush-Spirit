using UnityEngine;

namespace BrushSpirit.Enemies
{
    /// <summary>
    /// 挂在平台刷怪点上，约束该平台小怪的水平活动范围（世界坐标）。
    /// </summary>
    public class PlatformEnemySpawn : MonoBehaviour
    {
        public float patrolMinX;
        public float patrolMaxX;
    }
}
