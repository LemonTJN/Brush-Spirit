using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>挂到一次性 VFX 预制体上，按 lifetime 自销毁。</summary>
    public class AutoDestroy : MonoBehaviour
    {
        [Tooltip("从启用开始到销毁的秒数")]
        public float lifetime = 0.3f;

        void OnEnable() => Destroy(gameObject, lifetime);
    }
}
