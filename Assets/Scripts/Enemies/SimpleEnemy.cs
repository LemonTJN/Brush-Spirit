using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    public class SimpleEnemy : MonoBehaviour, IDamageable
    {
        public float maxHp = 35f;
        public float moveSpeed = 2.2f;
        [Tooltip("与玩家的平面距离小于此值则尝试近战（原仅用水平距 + 过小范围，玩家后退时永远打不到）")]
        public float attackRange = 2.65f;
        public float attackDamage = 4.5f;
        public float attackCooldown = 0.85f;
        public int xpReward = 12;
        public float whiteDropChance = 0.35f;

        public EquipmentData whiteDropA;
        public EquipmentData whiteDropB;
        public Transform player;

        float _hp;
        float _cd;
        WorldHealthBar _bar;
        float _knockVelX;
        Color _idleTint;

        [Tooltip("击退水平速度每秒衰减量")]
        public float knockDecayPerSec = 22f;

        [Header("平台小怪（由 PlatformEnemySpawn + SetPlatformPatrol 启用）")]
        [Tooltip("与玩家高度差小于此值且水平在台面上，视为「已上台」开始追击")]
        public float samePlatformMaxDy = 0.78f;

        [Tooltip("玩家中心 X 可超出台面 patrol 范围的最大容差")]
        public float onPlatformXSlack = 0.42f;

        const float PatrolEdgeSlack = 0.12f;

        bool _platformMode;
        float _platformMinX;
        float _platformMaxX;
        float _lockedY;
        float _patrolDir = 1f;

        /// <summary>将小怪限制在 [minX,maxX]，Y 锁定；未上台时巡逻，上台后追击（仍不离开台面）。</summary>
        public void SetPlatformPatrol(float minX, float maxX)
        {
            if (maxX <= minX) return;
            _platformMode = true;
            _platformMinX = minX;
            _platformMaxX = maxX;
            _lockedY = transform.position.y;
            _patrolDir = Random.value < 0.5f ? -1f : 1f;
        }

        void Start()
        {
            _hp = maxHp;
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null) _idleTint = _sr.color;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 0.58f);
            ResolvePlayerTransform();
        }

        void ResolvePlayerTransform()
        {
            if (player != null) return;
            if (PlayerStats.Active != null)
            {
                player = PlayerStats.Active.transform;
                return;
            }

            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        public void TakeDamage(float amount, GameObject attacker, float knockbackImpulseX = 0f)
        {
            _hp -= amount;
            _bar?.SetHp(_hp);
            if (knockbackImpulseX > 0f)
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker) * knockbackImpulseX;
            Flash();
            if (_hp <= 0f)
                Die(attacker);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Mathf.Abs(_knockVelX) > 0.02f)
            {
                transform.position += new Vector3(_knockVelX * dt, 0f, 0f);
                _knockVelX = Mathf.MoveTowards(_knockVelX, 0f, knockDecayPerSec * dt);
            }

            if (_platformMode)
            {
                if (Mathf.Abs(_knockVelX) <= 0.02f)
                {
                    if (player == null) ResolvePlayerTransform();
                    if (player != null)
                        TickPlatformAi(dt);
                }

                ClampToPlatform();
                return;
            }

            if (player == null) ResolvePlayerTransform();
            if (player == null) return;
            Vector2 delta = (Vector2)(player.position - transform.position);
            float dist = delta.magnitude;
            if (dist > attackRange)
            {
                float dx = delta.x;
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * dt, 0f, 0f);
                _cd = Mathf.Max(0f, _cd - dt);
            }
            else
            {
                _cd -= dt;
                if (_cd <= 0f)
                {
                    TryHitPlayer();
                    _cd = attackCooldown;
                }
            }
        }

        bool PlayerOnThisPlatform()
        {
            if (player == null) return false;
            Vector3 pp = player.position;
            if (Mathf.Abs(pp.y - transform.position.y) > samePlatformMaxDy) return false;
            return pp.x >= _platformMinX - onPlatformXSlack && pp.x <= _platformMaxX + onPlatformXSlack;
        }

        void TickPlatformAi(float dt)
        {
            Vector3 p = transform.position;
            bool aggro = PlayerOnThisPlatform();
            if (aggro)
            {
                Vector2 delta = (Vector2)(player.position - p);
                float dist = delta.magnitude;
                if (dist > attackRange)
                {
                    float dx = delta.x;
                    p.x += Mathf.Sign(dx) * moveSpeed * dt;
                }
                else
                {
                    _cd -= dt;
                    if (_cd <= 0f)
                    {
                        TryHitPlayer();
                        _cd = attackCooldown;
                    }
                }
            }
            else
            {
                _cd = Mathf.Max(0f, _cd - dt);
                p.x += _patrolDir * moveSpeed * dt;
                if (p.x >= _platformMaxX - PatrolEdgeSlack)
                {
                    p.x = _platformMaxX - PatrolEdgeSlack;
                    _patrolDir = -1f;
                }
                else if (p.x <= _platformMinX + PatrolEdgeSlack)
                {
                    p.x = _platformMinX + PatrolEdgeSlack;
                    _patrolDir = 1f;
                }
            }

            p.y = _lockedY;
            transform.position = p;
        }

        void ClampToPlatform()
        {
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, _platformMinX, _platformMaxX);
            p.y = _lockedY;
            transform.position = p;
        }

        void TryHitPlayer()
        {
            var stats = PlayerStats.Active != null ? PlayerStats.Active : player != null ? player.GetComponent<PlayerStats>() : null;
            if (stats == null) stats = Object.FindObjectOfType<PlayerStats>();
            stats?.TakeDamage(attackDamage, gameObject);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);

            if (Random.value < whiteDropChance)
            {
                var drop = Random.value < 0.5f ? whiteDropA : whiteDropB;
                if (drop != null)
                    Pickup.SpawnAt(transform.position + Vector3.up * 0.4f, drop);
            }

            Destroy(gameObject);
        }

        SpriteRenderer _sr;
        float _flashT;

        void Flash()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr != null)
            {
                _sr.color = Color.white;
                _flashT = 0.08f;
            }
        }

        void LateUpdate()
        {
            if (_flashT > 0f)
            {
                _flashT -= Time.deltaTime;
                if (_flashT <= 0f && _sr != null)
                    _sr.color = _idleTint;
            }
        }
    }
}
