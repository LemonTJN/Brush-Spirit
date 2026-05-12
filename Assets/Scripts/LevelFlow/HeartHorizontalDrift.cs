using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>褪色域整组水平漂移（绘心 03）。</summary>
    public class HeartHorizontalDrift : MonoBehaviour
    {
        public float speed = 0.65f;
        public float minX = -14f;
        public float maxX = 14f;

        void Update()
        {
            transform.position += Vector3.right * (speed * Time.deltaTime);
            float x = transform.position.x;
            if (x > maxX)
                transform.position = new Vector3(minX, transform.position.y, transform.position.z);
            if (x < minX)
                transform.position = new Vector3(maxX, transform.position.y, transform.position.z);
        }
    }
}
