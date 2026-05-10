using UnityEngine;

namespace BrushSpirit.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        public float moveSpeed = 7f;
        public float jumpForce = 14f;
        public Transform groundCheck;
        public float groundCheckRadius = 0.22f;

        Rigidbody2D _body;
        float _inputX;
        bool _jumpQueued;
        SpriteRenderer _sprite;

        void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _sprite = GetComponent<SpriteRenderer>();
            if (groundCheck == null)
            {
                var gc = new GameObject("GroundCheck");
                gc.transform.SetParent(transform, false);
                gc.transform.localPosition = new Vector3(0f, -0.55f, 0f);
                groundCheck = gc.transform;
            }
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
        }

        void FixedUpdate()
        {
            bool grounded = false;
            var cols = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i].CompareTag("Ground"))
                {
                    grounded = true;
                    break;
                }
            }

            Vector2 v = _body.velocity;
            v.x = _inputX * moveSpeed;
            if (_jumpQueued && grounded)
                v.y = jumpForce;
            _jumpQueued = false;

            _body.velocity = v;
        }

        public bool IsFacingRight => !_sprite.flipX;
    }
}
