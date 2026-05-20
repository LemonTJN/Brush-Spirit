using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 正交相机跟随目标（含纵向），并以世界坐标限制相机中心，避免露出未布置区域。
    /// </summary>
    public class CameraFollowPlayer2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 focusOffset = new Vector2(2.5f, 1.2f);
        [Tooltip("位置平滑时间；≤0 则每帧直接贴合目标。")]
        public float smoothTime = 0.06f;

        public float minX = -50f;
        public float maxX = 50f;
        public float minY = -6f;
        public float maxY = 10f;

        Vector3 _smoothVel;

        void LateUpdate()
        {
            if (target == null) return;

            Vector3 goal = target.position + (Vector3)focusOffset;
            goal.z = transform.position.z;

            Vector3 p;
            if (smoothTime <= 0.0001f)
                p = goal;
            else
                p = Vector3.SmoothDamp(transform.position, goal, ref _smoothVel, smoothTime);

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            p.z = transform.position.z;
            transform.position = p;
        }
    }
}
