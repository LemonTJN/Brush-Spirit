using UnityEngine;

namespace BrushSpirit.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 7f;
        public float jumpForce = 14f;

        [Header("滑墙 / 蹬墙跳")]
        [Tooltip("贴墙下滑最大速度（值越小下滑越慢）")]
        public float wallSlideSpeed = 1.4f;
        [Tooltip("蹬墙跳水平冲量")]
        public float wallJumpXForce = 6.5f;
        [Tooltip("蹬墙跳后短暂锁定水平输入，让弹出效果显现（秒）")]
        public float wallJumpInputLockTime = 0.18f;

        [Header("冲刺 / Dash")]
        [Tooltip("冲刺水平速度")]
        public float dashSpeed = 18f;
        [Tooltip("冲刺持续时间（秒）")]
        public float dashDuration = 0.18f;
        [Tooltip("冲刺冷却（秒）")]
        public float dashCooldown = 2f;
        [Tooltip("双击方向键触发的最大间隔（秒）")]
        public float doubleTapWindow = 0.25f;

        public Transform groundCheck;
        public float groundCheckRadius = 0.22f;
        public float wallCheckRadius = 0.14f;

        Rigidbody2D _body;
        float _inputX;
        bool _jumpQueued;
        SpriteRenderer _sprite;
        Animator _anim;
        static readonly int kSpeed = Animator.StringToHash("Speed");
        static readonly int kIsGrounded = Animator.StringToHash("IsGrounded");
        static readonly int kIsWallSliding = Animator.StringToHash("IsWallSliding");
        static readonly int kDash = Animator.StringToHash("Dash");

        Transform _wallCheckL;
        Transform _wallCheckR;
        float _wallJumpLockX;   // 蹬墙跳后水平输入锁定倒计时

        // 冲刺状态
        float _lastTapTime = -10f;
        int _lastTapDir;            // -1 左, +1 右
        float _dashCdRemaining;
        float _dashRemaining;       // > 0 表示正在冲刺中
        int _dashDir;               // 当前冲刺方向

        public float DashCdRemaining => _dashCdRemaining;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponent<SpriteRenderer>();
            _anim = GetComponent<Animator>();
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                var pm = new PhysicsMaterial2D("PlayerNoWallStick") { friction = 0f, bounciness = 0f };
                col.sharedMaterial = pm;
            }

            if (groundCheck == null)
            {
                var gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform, false);
                gc.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                groundCheck = gc.transform;
            }

            var wl = new GameObject("WallCheckL");
            wl.transform.SetParent(transform, false);
            wl.transform.localPosition = new Vector3(-0.36f, 0f, 0f);
            _wallCheckL = wl.transform;

            var wr = new GameObject("WallCheckR");
            wr.transform.SetParent(transform, false);
            wr.transform.localPosition = new Vector3(0.36f, 0f, 0f);
            _wallCheckR = wr.transform;
        }

        void Update()
        {
            _inputX = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) _inputX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) _inputX += 1f;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W))
                _jumpQueued = true;

            if (_inputX > 0.05f) _sprite.flipX = false;
            else if (_inputX < -0.05f) _sprite.flipX = true;

            // 双击方向键触发冲刺
            int tapDir = 0;
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) tapDir = -1;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) tapDir = 1;
            if (tapDir != 0)
            {
                if (_dashCdRemaining <= 0f && _dashRemaining <= 0f
                    && tapDir == _lastTapDir
                    && Time.time - _lastTapTime <= doubleTapWindow)
                {
                    _dashDir = tapDir;
                    _dashRemaining = dashDuration;
                    _dashCdRemaining = dashCooldown;
                    _lastTapTime = -10f;
                    if (_anim != null) _anim.SetTrigger(kDash);
                }
                else
                {
                    _lastTapTime = Time.time;
                    _lastTapDir = tapDir;
                }
            }
        }

        static bool TouchesGround(Vector2 pos, float radius)
        {
            var cols = Physics2D.OverlapCircleAll(pos, radius);
            foreach (var c in cols)
                if (c.CompareTag("Ground")) return true;
            return false;
        }

        /// <summary>
        /// 仅当脚下有「朝上的支撑面」时算着地。OverlapCircle 贴在平台竖直面时也会与同一 BoxCollider 相交，
        /// 会误判 grounded → onWall 失效，按住方向键会持续 vx 顶墙被摩擦卡死。
        /// </summary>
        bool IsFeetOnSupport()
        {
            Vector2 origin = (Vector2)groundCheck.position + Vector2.up * 0.04f;
            float dist = groundCheckRadius + 0.14f;
            float castR = groundCheckRadius * 0.82f;
            var hits = Physics2D.CircleCastAll(origin, castR, Vector2.down, dist);
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h.collider == null || !h.collider.CompareTag("Ground")) continue;
                if (h.collider.attachedRigidbody == _body) continue;
                // 竖壁/侧向挤压的法线接近水平，排除；台面法线朝上
                if (h.normal.y > 0.45f)
                    return true;
            }

            return false;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            if (_wallJumpLockX > 0f) _wallJumpLockX -= dt;
            if (_dashCdRemaining > 0f) _dashCdRemaining -= dt;

            // 冲刺期间：覆盖水平速度、抵消重力、跳过常规移动逻辑
            if (_dashRemaining > 0f)
            {
                _dashRemaining -= dt;
                _body.velocity = new Vector2(_dashDir * dashSpeed, 0f);
                if (_anim != null)
                {
                    _anim.SetFloat(kSpeed, dashSpeed);
                    _anim.SetBool(kIsGrounded, IsFeetOnSupport());
                    _anim.SetBool(kIsWallSliding, false);
                }
                return;
            }

            bool grounded = IsFeetOnSupport();
            bool wallL    = TouchesGround(_wallCheckL.position, wallCheckRadius);
            bool wallR    = TouchesGround(_wallCheckR.position, wallCheckRadius);

            // 贴墙方向：+1=右墙 / -1=左墙 / 0=无墙
            int wallDir = wallR ? 1 : (wallL ? -1 : 0);
            bool onWall = wallDir != 0 && !grounded;
            // 贴墙且不在地面就滑墙，无论是否按方向键
            bool sliding = onWall;

            Vector2 v = _body.velocity;

            // 水平速度：蹬墙跳锁定期间不覆盖，让弹出冲量生效
            if (_wallJumpLockX <= 0f)
            {
                float targetVx = _inputX * moveSpeed;
                // 持续向墙内施加速率会与 2D 碰撞解算摩擦，易把垂直速度卡在 0；只允许离开墙的方向
                if (onWall)
                {
                    if (wallDir > 0 && targetVx > 0f) targetVx = 0f;
                    else if (wallDir < 0 && targetVx < 0f) targetVx = 0f;
                }

                v.x = targetVx;
            }

            // 跳跃处理
            if (_jumpQueued)
            {
                _jumpQueued = false;
                if (grounded)
                {
                    v.y = jumpForce;
                }
                else if (onWall)
                {
                    // 蹬墙跳：向反方向弹出
                    v.y = jumpForce;
                    v.x = -wallDir * wallJumpXForce;
                    _wallJumpLockX = wallJumpInputLockTime;
                    _sprite.flipX = wallDir > 0; // 朝离开墙的方向
                }
            }

            // 滑墙：匀速缓慢下落（按住贴墙键时不再被摩擦卡在 vy≈0）；上升阶段不覆盖（蹬墙跳等）
            if (sliding && v.y <= 0f)
                v.y = -wallSlideSpeed;

            _body.velocity = v;

            if (_anim != null)
            {
                _anim.SetFloat(kSpeed, Mathf.Abs(v.x));
                _anim.SetBool(kIsGrounded, grounded);
                _anim.SetBool(kIsWallSliding, sliding);
            }
        }

        public bool IsFacingRight => !_sprite.flipX;
    }
}
