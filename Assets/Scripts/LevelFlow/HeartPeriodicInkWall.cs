using System.Collections;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>墨墙起落：周期性阻挡通道。</summary>
    public class HeartPeriodicInkWall : MonoBehaviour
    {
        public float startDelay = 2.5f;
        public float interval = 6f;
        public float wallUpDuration = 2.2f;

        BoxCollider2D _wallCol;
        SpriteRenderer _sr;

        void Awake()
        {
            _wallCol = GetComponent<BoxCollider2D>();
            _sr = GetComponent<SpriteRenderer>();
            if (_wallCol != null) _wallCol.enabled = false;
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 0f;
                _sr.color = c;
            }
        }

        void Start()
        {
            StartCoroutine(Loop());
        }

        IEnumerator Loop()
        {
            yield return new WaitForSeconds(startDelay);
            while (true)
            {
                yield return new WaitForSeconds(interval);
                if (_wallCol != null) _wallCol.enabled = true;
                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = 0.55f;
                    _sr.color = c;
                }

                yield return new WaitForSeconds(wallUpDuration);
                if (_wallCol != null) _wallCol.enabled = false;
                if (_sr != null)
                {
                    var c = _sr.color;
                    c.a = 0f;
                    _sr.color = c;
                }
            }
        }
    }
}
