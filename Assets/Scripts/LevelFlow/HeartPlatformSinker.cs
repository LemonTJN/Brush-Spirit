using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>悬枢阶：平台周期性「沉入纸背」（碰撞关闭、半透明）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartPlatformSinker : MonoBehaviour
    {
        public float solidSeconds = 2.8f;
        public float sunkSeconds = 1.35f;

        Collider2D _col;
        SpriteRenderer _sr;
        float _phase;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            _phase += Time.deltaTime;
            float cycle = solidSeconds + sunkSeconds;
            float t = _phase % cycle;
            bool sunk = t >= solidSeconds;
            if (_col != null)
                _col.enabled = !sunk;
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = sunk ? 0.28f : 1f;
                _sr.color = c;
            }
        }
    }
}
