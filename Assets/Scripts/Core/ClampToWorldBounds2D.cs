using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 将物体中心限制在主相机正交视口内（内边距与 PlayfieldBoundaryController 一致）。
    /// 用于无 Rigidbody2D 的小怪/Boss/掉落物；玩家可同时靠碰撞体与此组件双保险。
    /// </summary>
    public class ClampToWorldBounds2D : MonoBehaviour
    {
        [Tooltip("在全局内边距基础上，再为半宽留的余量（世界单位）")]
        public float halfWidthPad = 0.35f;

        [Tooltip("在全局内边距基础上，再为半高留的余量（世界单位）")]
        public float halfHeightPad = 0.5f;

        [Tooltip("为 true 时：仅当物体在主相机视口内才钳制，避免远处关卡内单位被拽进画面")]
        public bool skipClampWhenOutsideViewport = true;

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            if (skipClampWhenOutsideViewport)
            {
                Vector3 v = cam.WorldToViewportPoint(transform.position);
                if (v.z < 0f || v.x < -0.02f || v.x > 1.02f || v.y < -0.02f || v.y > 1.02f) return;
            }

            float inset = OrthoCameraBounds.PlayInnerInset;
            OrthoCameraBounds.GetWorldRect(cam, inset, out float minX, out float maxX, out float minY, out float maxY);
            minX += halfWidthPad;
            maxX -= halfWidthPad;
            minY += halfHeightPad;
            maxY -= halfHeightPad;
            if (minX > maxX) minX = maxX = (minX + maxX) * 0.5f;
            if (minY > maxY) minY = maxY = (minY + maxY) * 0.5f;

            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, minY, maxY);
            transform.position = p;
        }
    }
}
