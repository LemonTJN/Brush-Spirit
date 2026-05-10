using BrushSpirit.Core;
using BrushSpirit.Items;
using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.Enemies
{
    public class BossInkTree : MonoBehaviour, IDamageable
    {
        public float maxHp = 220f;
        public float moveSpeed = 2.8f;
        public float slamRadius = 2.2f;
        public float slamDamage = 24f;
        public int xpReward = 80;
        public EquipmentData colorGearDrop;
        public Transform player;

        public System.Action OnDefeated;

        float _hp;
        float _t;
        int _phase;

        enum BossState { Chase, Windup, Slam, Recover }
        BossState _state = BossState.Chase;

        SpriteRenderer _sr;
        WorldHealthBar _bar;
        float _knockVelX;

        public float knockDecayPerSec = 16f;
        [Tooltip("相对小怪，Boss 受 K 击退时乘此系数（仍比 J/L 明显）")]
        public float bossKnockbackMul = 0.55f;

        void Start()
        {
            _hp = maxHp;
            _bar = WorldHealthBar.AddTo(transform, maxHp, 1.35f);
            _sr = GetComponent<SpriteRenderer>();
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
                _knockVelX += KnockbackFromAttacker.SignedHorizontalDir(transform, attacker) *
                              knockbackImpulseX * bossKnockbackMul;
            if (_sr != null)
            {
                _sr.color = Color.white;
                Invoke(nameof(ResetColor), 0.08f);
            }

            if (_hp <= maxHp * 0.55f) _phase = 1;
            if (_hp <= maxHp * 0.28f) _phase = 2;

            if (_hp <= 0f)
                Die(attacker);
        }

        void ResetColor()
        {
            if (_sr != null) _sr.color = new Color(0.1f, 0.1f, 0.12f);
        }

        void Die(GameObject attacker)
        {
            var stats = attacker != null ? attacker.GetComponent<PlayerStats>() : null;
            stats?.AddXp(xpReward);
            if (colorGearDrop != null)
                Pickup.SpawnAt(transform.position + Vector3.up * 0.6f, colorGearDrop);
            OnDefeated?.Invoke();
            Destroy(gameObject);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (Mathf.Abs(_knockVelX) > 0.02f)
            {
                transform.position += new Vector3(_knockVelX * dt, 0f, 0f);
                _knockVelX = Mathf.MoveTowards(_knockVelX, 0f, knockDecayPerSec * dt);
            }

            if (player == null) ResolvePlayerTransform();
            if (player == null) return;
            _t += Time.deltaTime;

            switch (_state)
            {
                case BossState.Chase:
                    Chase();
                    float interval = _phase == 0 ? 2.4f : _phase == 1 ? 1.85f : 1.45f;
                    if (_t >= interval)
                    {
                        _t = 0f;
                        _state = BossState.Windup;
                    }
                    break;
                case BossState.Windup:
                    if (_t >= 0.45f)
                    {
                        _t = 0f;
                        _state = BossState.Slam;
                    }
                    break;
                case BossState.Slam:
                    DoSlam();
                    _t = 0f;
                    _state = BossState.Recover;
                    break;
                case BossState.Recover:
                    if (_t >= (_phase >= 2 ? 0.55f : 0.75f))
                    {
                        _t = 0f;
                        _state = BossState.Chase;
                    }
                    break;
            }
        }

        void Chase()
        {
            float dx = player.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.15f)
                transform.position += new Vector3(Mathf.Sign(dx) * moveSpeed * Time.deltaTime, 0f, 0f);
        }

        void DoSlam()
        {
            if (player == null) ResolvePlayerTransform();
            if (player == null) return;
            float dist = Vector2.Distance(player.position, transform.position);
            if (dist <= slamRadius)
            {
                var stats = PlayerStats.Active != null ? PlayerStats.Active : player.GetComponent<PlayerStats>();
                if (stats == null) stats = Object.FindObjectOfType<PlayerStats>();
                stats?.TakeDamage(slamDamage, gameObject);
            }
        }
    }
}
