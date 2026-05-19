using System.Collections;
using System.Collections.Generic;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 焚道「爆灰」：预警圈扩大 → 圆形范围伤害 + 击退；可选爆炸后 360° 溅射（烬谷 03）。
    /// </summary>
    public class AshGroundBurstOneShot : MonoBehaviour
    {
        const int SplashSpokes = 14;

        SpriteRenderer _sr;
        Sprite _ringSprite;
        float _warn = 1f;
        float _radius = 2.3f;
        float _damage = 7.5f;
        float _knock = 6f;
        bool _enableRadialSplash;
        float _splashDamageRatio = 0.55f;
        float _splashKnockRatio = 0.7f;
        float _splashRangeMult = 4.2f;
        float _splashSpeed = 11.5f;
        float _splashHitRadius = 0.42f;

        public static AshGroundBurstOneShot Create(Vector3 pos, Sprite spr, float warnDuration, float blastRadius,
            float damage, float knockImpulse, bool enableRadialSplash = false, float splashDamageRatio = 0.55f)
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
            b._ringSprite = spr;
            b._warn = warnDuration;
            b._radius = blastRadius;
            b._damage = damage;
            b._knock = knockImpulse;
            b._enableRadialSplash = enableRadialSplash;
            b._splashDamageRatio = splashDamageRatio;
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
            if (_enableRadialSplash)
                yield return DoRadialSplash();

            _sr.color = new Color(0.75f, 0.75f, 0.8f, 0.55f);
            yield return new WaitForSeconds(0.06f);
            Destroy(gameObject);
        }

        void DoBlast()
        {
            GameRuntimeBootstrap.ShowAttackSlashFx(
                transform.position,
                0f,
                _radius,
                0.12f,
                new Color(0.35f, 0.35f, 0.4f, 0.55f),
                _radius * 1.15f);

            ApplyCircleDamage(transform.position, _radius * 0.52f, _damage, _knock, null);
        }

        IEnumerator DoRadialSplash()
        {
            yield return new WaitForSeconds(0.06f);

            var center = (Vector2)transform.position;
            float maxRange = _radius * _splashRangeMult;
            float splashDmg = _damage * _splashDamageRatio;
            float splashKnock = _knock * _splashKnockRatio;
            float speed = _splashSpeed * Random.Range(0.92f, 1.08f);

            GameRuntimeBootstrap.ShowAttackSlashFx(
                center,
                0f,
                _radius * 0.35f,
                0.1f,
                new Color(0.95f, 0.45f, 0.12f, 0.45f),
                _radius * 0.5f);

            for (int i = 0; i < SplashSpokes; i++)
            {
                float ang = i * (Mathf.PI * 2f / SplashSpokes);
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
                AshSplashEmber.Launch(
                    center,
                    dir,
                    _ringSprite,
                    speed,
                    maxRange,
                    _splashHitRadius,
                    splashDmg,
                    splashKnock,
                    gameObject);
            }

            yield break;
        }

        void ApplyCircleDamage(Vector2 center, float hitRadius, float damage, float knock,
            HashSet<PlayerStats> onlyOnce)
        {
            var hits = Physics2D.OverlapCircleAll(center, hitRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null || !h.CompareTag("Player")) continue;

                var stats = h.GetComponentInParent<PlayerStats>() ?? h.GetComponent<PlayerStats>();
                if (stats == null) continue;
                if (onlyOnce != null && !onlyOnce.Add(stats)) continue;

                stats.TakeDamage(damage, gameObject);
                var rb = h.GetComponentInParent<Rigidbody2D>() ?? h.GetComponent<Rigidbody2D>();
                if (rb == null) continue;

                Vector2 d = rb.position - center;
                if (d.sqrMagnitude < 0.0001f)
                    d = Vector2.up;
                d.Normalize();
                rb.velocity += d * knock + Vector2.up * (knock * 0.38f);
            }
        }
    }
}
