using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>漂浮画框等地形：水平往返漂移（子物体带碰撞）。</summary>
    public class HeartDriftingTerrain : MonoBehaviour
    {
        public float speed = 0.38f;
        public float minX = -8f;
        public float maxX = 8f;

        void Update()
        {
            transform.position += Vector3.right * (speed * Time.deltaTime);
            float x = transform.position.x;
            if (x > maxX || x < minX)
                speed = -speed;
        }
    }
}
