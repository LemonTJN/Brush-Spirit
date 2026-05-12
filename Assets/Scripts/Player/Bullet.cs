using BrushSpirit.Core;
using UnityEngine;

namespace BrushSpirit.Player
{
    /// <summary>玩家手枪发射的子弹：直线飞、撞到 Hurtbox 扣血、超时自销毁。</summary>
    public class Bullet : MonoBehaviour
    {
        public float speed = 18f;
        public float lifetime = 1.2f;
        public float damage = 8f;
        public float knockbackImpulse = 4f;

        Vector2 _dir = Vector2.right;
        GameObject _owner;
        float _aliveT;

        public void Launch(Vector2 direction, GameObject owner, float dmg)
        {
            _dir = direction.normalized;
            if (_dir.sqrMagnitude < 0.01f) _dir = Vector2.right;
            _owner = owner;
            damage = dmg;
            transform.right = (Vector3)_dir; // 子弹图朝向飞行方向
        }

        void Update()
        {
            _aliveT += Time.deltaTime;
            if (_aliveT >= lifetime)
            {
                Destroy(gameObject);
                return;
            }
            transform.position += (Vector3)(_dir * speed * Time.deltaTime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            if (_owner != null && other.transform.IsChildOf(_owner.transform)) return;
            if (other.CompareTag("Player")) return;
            if (other.CompareTag("Ground"))
            {
                Destroy(gameObject);
                return;
            }
            var d = other.GetComponent<IDamageable>();
            if (d == null) d = other.GetComponentInParent<IDamageable>();
            if (d != null)
            {
                d.TakeDamage(damage, _owner, _dir.x * knockbackImpulse);
                Destroy(gameObject);
            }
        }
    }
}
