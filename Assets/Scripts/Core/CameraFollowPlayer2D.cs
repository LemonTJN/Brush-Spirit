using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 正交相机跟随目标（含纵向），并以世界坐标限制相机中心，避免露出未布置区域。
    /// </summary>
    public class CameraFollowPlayer2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 focusOffset = new Vector2(2.5f, 1.2f);
        [Tooltip("位置平滑时间；≤0 则每帧直接贴合目标。")]
        public float smoothTime = 0.06f;

        public float minX = -50f;
        public float maxX = 50f;
        public float minY = -6f;
        public float maxY = 10f;

        Vector3 _smoothVel;

        // ── 震屏：振幅 + 剩余时长，外部通过 Shake() 触发 ──
        float _shakeAmp;
        float _shakeRemain;
        float _shakeTotal;

        public static CameraFollowPlayer2D Active { get; private set; }

        void OnEnable() { Active = this; }
        void OnDisable() { if (Active == this) Active = null; }

        /// <summary>
        /// 给相机叠加一段震屏。同时调用时取较强的那一次（不累加，避免叠到飞出屏幕）。
        /// </summary>
        /// <param name="amplitude">最大偏移（世界单位），0.06~0.25 比较合适。</param>
        /// <param name="duration">持续时间（秒），通常 0.08~0.20。</param>
        public void Shake(float amplitude, float duration)
        {
            if (amplitude <= 0f || duration <= 0f) return;
            // 取较强的那一次，并刷新时长
            if (amplitude > _shakeAmp) _shakeAmp = amplitude;
            if (duration > _shakeRemain) _shakeRemain = duration;
            _shakeTotal = Mathf.Max(_shakeTotal, duration);
        }

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = target.position + (Vector3)focusOffset;
            goal.z = transform.position.z;

            Vector3 p;
            if (smoothTime <= 0.0001f)
                p = goal;
            else
                p = Vector3.SmoothDamp(transform.position, goal, ref _smoothVel, smoothTime);

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            p.z = transform.position.z;

            // 震屏：用未缩放时间（hit-stop 期间相机仍然抖）+ 随时间衰减的高频偏移
            if (_shakeRemain > 0f && _shakeTotal > 0f)
            {
                _shakeRemain -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_shakeRemain / _shakeTotal); // 1→0 衰减
                float amp = _shakeAmp * k * k; // 二次衰减，尾巴更短
                // 用噪声而不是 Random，避免每帧完全独立——更接近真实手感
                float t = Time.unscaledTime * 38f;
                float ox = (Mathf.PerlinNoise(t, 0.13f) - 0.5f) * 2f * amp;
                float oy = (Mathf.PerlinNoise(0.71f, t) - 0.5f) * 2f * amp;
                p.x += ox;
                p.y += oy;

                if (_shakeRemain <= 0f) { _shakeAmp = 0f; _shakeTotal = 0f; }
            }

            transform.position = p;
        }
    }
}
