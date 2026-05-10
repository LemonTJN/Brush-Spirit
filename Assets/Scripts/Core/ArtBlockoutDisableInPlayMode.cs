using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 场景内「美术对照」用的占位几何：运行时 <see cref="GameRuntimeBootstrap"/> 会生成真实关卡，
    /// 进入 Play 后关闭此子树，避免重复显示；编辑模式下保留便于量尺与替换美术资源。
    /// </summary>
    [DisallowMultipleComponent]
    public class ArtBlockoutDisableInPlayMode : MonoBehaviour
    {
        void Awake()
        {
            if (Application.isPlaying)
                gameObject.SetActive(false);
        }
    }
}
