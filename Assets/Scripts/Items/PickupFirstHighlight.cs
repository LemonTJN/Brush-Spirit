using UnityEngine;

namespace BrushSpirit.Items
{
    /// <summary>首次掉落物高亮：轻微缩放脉冲，拾取或销毁时结束。</summary>
    public class PickupFirstHighlight : MonoBehaviour
    {
        SpriteRenderer _sr;
        Vector3 _baseScale;
        float _phase;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _baseScale = transform.localScale;
        }

        void Update()
        {
            _phase += Time.deltaTime * 4.2f;
            float m = 1f + 0.12f * Mathf.Sin(_phase);
            transform.localScale = _baseScale * m;
            if (_sr != null)
            {
                float e = 0.5f + 0.5f * Mathf.Sin(_phase * 1.1f);
                _sr.color = Color.Lerp(_sr.color, new Color(1f, 1f, 0.92f, 1f), 0.08f * e);
            }
        }
    }
}
