using System.Collections;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 焚道「爆灰」：预警圈扩大 → 短瞬间圆形范围伤害 + 击退。
    /// </summary>
    public class AshGroundBurstOneShot : MonoBehaviour
    {
        SpriteRenderer _sr;
        float _warn = 1f;
        float _radius = 2.3f;
        float _damage = 7.5f;
        float _knock = 6f;

        public static AshGroundBurstOneShot Create(Vector3 pos, Sprite spr, float warnDuration, float blastRadius,
            float damage, float knockImpulse)
        {
            var go = new GameObject("AshGroundBurst");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr;
            sr.color = new Color(0.92f, 0.38f, 0.14f, 0.32f);
            sr.sortingOrder = 4;
            go.transform.localScale = Vector3.one * 0.2f;
            var b = go.AddComponent<AshGroundBurstOneShot>();
            b._sr = sr;
            b._warn = warnDuration;
            b._radius = blastRadius;
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
                float scale = Mathf.Lerp(0.22f, _radius * 2.1f, u);
                transform.localScale = new Vector3(scale, scale, 1f);
                _sr.color = new Color(0.95f, 0.42f, 0.16f, 0.28f + 0.5f * u);
                yield return null;
            }

            DoBlast();
            _sr.color = new Color(0.75f, 0.75f, 0.8f, 0.55f);
            yield return new WaitForSeconds(0.06f);
            Destroy(gameObject);
        }

        void DoBlast()
        {
            BrushSpirit.GameRuntimeBootstrap.ShowAttackSlashFx(
                transform.position,
                0f,
                _radius,
                0.12f,
                new Color(0.35f, 0.35f, 0.4f, 0.55f),
                _radius * 1.15f);

            var hits = Physics2D.OverlapCircleAll(transform.position, _radius * 0.48f);
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
                rb.velocity += d * _knock + Vector2.up * 2.4f;
            }
        }
    }
}
