using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>钟摆枢轴：摆动子物体（子级挂 <see cref="HeartPendulumBladeHit"/>）。</summary>
    public class HeartPendulumPivot : MonoBehaviour
    {
        public float swingAmplitudeDeg = 52f;
        public float swingPeriod = 3.2f;

        [Tooltip("秒；两摆错相，扫过走廊口时间错开。")]
        public float phaseTimeOffset;

        void Update()
        {
            float t = (Time.time + phaseTimeOffset) * (Mathf.PI * 2f / swingPeriod);
            float ang = Mathf.Sin(t) * swingAmplitudeDeg;
            transform.localRotation = Quaternion.Euler(0f, 0f, ang);
        }
    }
}
