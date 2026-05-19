using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 爆灰溅射：从爆炸中心沿单一方向外飞，玩家可通过走位躲避。
    /// </summary>
    public class AshSplashEmber : MonoBehaviour
    {
        Vector2 _dir;
        Vector2 _start;
        float _speed;
        float _maxRange;
        float _damage;
        float _knock;
        GameObject _owner;
        bool _hitPlayer;
        float _traveled;

        public static void Launch(Vector2 origin, Vector2 direction, Sprite sprite, float speed, float maxRange,
            float hitRadius, float damage, float knock, GameObject owner)
        {
            var go = new GameObject("AshSplashEmber");
            go.transform.position = origin;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite != null ? sprite : GameRuntimeBootstrap.CreatePlaceholderSprite();
            sr.color = new Color(0.98f, 0.52f, 0.16f, 0.78f);
            sr.sortingOrder = 7;

            float len = Mathf.Max(hitRadius * 1.35f, 0.55f);
            go.transform.localScale = new Vector3(len * 1.6f, len * 0.55f, 1f);
            float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            go.transform.rotation = Quaternion.Euler(0f, 0f, ang);

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = hitRadius;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var ember = go.AddComponent<AshSplashEmber>();
            ember._dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
            ember._start = origin;
            ember._speed = speed;
            ember._maxRange = maxRange;
            ember._damage = damage;
            ember._knock = knock;
            ember._owner = owner;
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_dir * step);
            _traveled += step;

            float u = Mathf.Clamp01(_traveled / Mathf.Max(_maxRange, 0.01f));
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = new Color(0.98f, 0.52f, 0.16f, Mathf.Lerp(0.82f, 0.22f, u));

            if (_traveled >= _maxRange)
                Destroy(gameObject);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || _hitPlayer) return;
            if (other.CompareTag("Ground"))
            {
                Destroy(gameObject);
                return;
            }

            if (!other.CompareTag("Player")) return;
            _hitPlayer = true;

            var stats = other.GetComponentInParent<PlayerStats>() ?? other.GetComponent<PlayerStats>();
            stats?.TakeDamage(_damage, _owner != null ? _owner : gameObject);

            var prb = other.GetComponentInParent<Rigidbody2D>() ?? other.GetComponent<Rigidbody2D>();
            if (prb != null)
            {
                Vector2 push = _dir;
                if (push.sqrMagnitude < 0.0001f)
                    push = Vector2.up;
                prb.velocity += push.normalized * _knock + Vector2.up * (_knock * 0.35f);
            }

            Destroy(gameObject);
        }
    }
}
