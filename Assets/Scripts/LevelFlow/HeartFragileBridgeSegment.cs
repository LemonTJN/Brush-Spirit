using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>链桥单节：站立过久后坍塌（碰撞关闭）。</summary>
    [RequireComponent(typeof(Collider2D))]
    public class HeartFragileBridgeSegment : MonoBehaviour
    {
        public float breakAfterStandSeconds = 1.65f;

        [Tooltip(">1 时站立计时乘该系数（承重裂纹：站久更快塌）。")]
        public float standTimeMultiplier = 1f;

        [Tooltip("true 时裂纹段略偏红的视觉提示。")]
        public bool crackVisual;

        Collider2D _col;
        SpriteRenderer _sr;
        float _stand;
        bool _broken;

        void Awake()
        {
            _col = GetComponent<Collider2D>();
            _sr = GetComponent<SpriteRenderer>();
            if (_col != null) _col.isTrigger = false;
            if (crackVisual && _sr != null)
                _sr.color = new Color(0.52f, 0.36f, 0.34f, 0.92f);
        }

        void OnCollisionStay2D(Collision2D collision)
        {
            if (_broken || !collision.collider.CompareTag("Player")) return;
            _stand += Time.deltaTime * Mathf.Max(0.5f, standTimeMultiplier);
            if (_stand < breakAfterStandSeconds) return;
            _broken = true;
            if (_col != null) _col.enabled = false;
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 0.18f;
                _sr.color = c;
            }
        }

        void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.collider.CompareTag("Player"))
                _stand = 0f;
        }
    }
}
