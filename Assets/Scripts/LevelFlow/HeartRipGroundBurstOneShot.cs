using System.Collections;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 绘心「裂帛」：带状预警 → 横向范围伤害 + 击退。
    /// </summary>
    public class HeartRipGroundBurstOneShot : MonoBehaviour
    {
        SpriteRenderer _sr;
        float _warn = 1f;
        float _halfWidth = 5.5f;
        float _halfHeight = 0.45f;
        float _damage = 9f;
        float _knock = 7f;

        public static HeartRipGroundBurstOneShot Create(Vector3 center, Sprite spr, float warnDuration, float halfWidth,
            float halfHeight, float damage, float knockImpulse)
        {
            var go = new GameObject("HeartRipBurst");
            go.transform.position = center;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.55f, 0.5f, 0.62f, 0.35f);
            sr.sortingOrder = 4;
            go.transform.localScale = new Vector3(0.4f, 0.25f, 1f);
            var b = go.AddComponent<HeartRipGroundBurstOneShot>();
            b._sr = sr;
            b._warn = warnDuration;
            b._halfWidth = halfWidth;
            b._halfHeight = halfHeight;
            b._damage = damage;
            b._knock = knockImpulse;
            b.StartCoroutine(b.Run());
            return b;
        }

        IEnumerator Run()
        {
            float t = 0f;
            while (t < _warn)
            {
                t += Time.deltaTime;
                float u = t / _warn;
                float sx = Mathf.Lerp(0.5f, _halfWidth * 2.05f, u);
                float sy = Mathf.Lerp(0.2f, _halfHeight * 2.2f, u);
                transform.localScale = new Vector3(sx, sy, 1f);
                _sr.color = new Color(0.62f, 0.35f, 0.45f, 0.28f + 0.45f * u);
                yield return null;
            }

            DoBlast();
            _sr.color = new Color(0.35f, 0.32f, 0.38f, 0.6f);
            yield return new WaitForSeconds(0.06f);
            Destroy(gameObject);
        }

        void DoBlast()
        {
            GameRuntimeBootstrap.ShowAttackSlashFx(
                transform.position,
                0f,
                _halfWidth,
                0.14f,
                new Color(0.45f, 0.4f, 0.52f, 0.55f),
                _halfHeight * 2.2f);

            var hits = Physics2D.OverlapBoxAll(transform.position, new Vector2(_halfWidth * 1.9f, _halfHeight * 2f), 0f);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null || !h.CompareTag("Player")) continue;
                var stats = h.GetComponentInParent<PlayerStats>() ?? h.GetComponent<PlayerStats>();
                stats?.TakeDamage(_damage, gameObject);
                var rb = h.GetComponentInParent<Rigidbody2D>() ?? h.GetComponent<Rigidbody2D>();
                if (rb == null) continue;
                Vector2 d = rb.position - (Vector2)transform.position;
                if (d.sqrMagnitude < 0.0001f)
                    d = Vector2.up;
                d.Normalize();
                rb.velocity += d * _knock + Vector2.up * 2.6f;
            }
        }
    }
}
