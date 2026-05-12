using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>悬枢阶：整块枢轴慢旋，子级为可站立结构。</summary>
    public class HeartRotatingArenaRoot : MonoBehaviour
    {
        public float degreesPerSecond = 8.5f;

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}
