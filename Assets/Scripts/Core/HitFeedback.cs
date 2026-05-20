using System.Collections;
using UnityEngine;

namespace BrushSpirit.Core
{
    /// <summary>
    /// 命中反馈：顿帧（Hit-stop）+ 震屏（Camera shake）。
    /// 设计参考《空洞骑士》《只沃》《大乱斗》：命中瞬间 Time.timeScale 短暂压低，
    /// 让玩家在生理上感到「咬到肉」，再叠加随时间衰减的相机抖动，给攻击量感。
    ///
    /// 用法：直接 HitFeedback.Light() / Heavy() / Heavy(BodyCenter) 一行调用。
    /// 不需要在场景里挂任何 Manager 组件，第一次用时自动创建一个 DontDestroyOnLoad 单例。
    /// </summary>
    public static class HitFeedback
    {
        // ── 强度预设。amp 是相机偏移（世界单位），stop 是顿帧持续时间。
        // 经测试这套值偏温和：玩家连按时不会晕，关键招（K 墨爆）仍有明显量感。
        // 顿帧别超过 0.06s，否则连击节奏会被反复打断。
        public const float LightAmp = 0.035f, LightStop = 0.018f, LightShakeDur = 0.08f;
        public const float MedAmp   = 0.060f, MedStop   = 0.028f, MedShakeDur   = 0.10f;
        public const float HeavyAmp = 0.090f, HeavyStop = 0.040f, HeavyShakeDur = 0.13f;
        public const float HugeAmp  = 0.150f, HugeStop  = 0.060f, HugeShakeDur  = 0.20f;

        static HitFeedbackRunner _runner;

        static HitFeedbackRunner Runner()
        {
            if (_runner != null) return _runner;
            var go = new GameObject("~HitFeedbackRunner");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<HitFeedbackRunner>();
            return _runner;
        }

        public static void Light()  => Trigger(LightAmp, LightStop, LightShakeDur);
        public static void Medium() => Trigger(MedAmp,   MedStop,   MedShakeDur);
        public static void Heavy()  => Trigger(HeavyAmp, HeavyStop, HeavyShakeDur);
        public static void Huge()   => Trigger(HugeAmp,  HugeStop,  HugeShakeDur);

        /// <summary>自定义强度。</summary>
        public static void Trigger(float shakeAmp, float hitStopSec, float shakeDur)
        {
            if (CameraFollowPlayer2D.Active != null)
                CameraFollowPlayer2D.Active.Shake(shakeAmp, shakeDur);
            if (hitStopSec > 0f) Runner().BeginHitStop(hitStopSec);
        }
    }

    internal class HitFeedbackRunner : MonoBehaviour
    {
        Coroutine _co;
        float _savedScale = 1f;

        public void BeginHitStop(float seconds)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(HitStopRoutine(seconds));
        }

        IEnumerator HitStopRoutine(float sec)
        {
            // 同一次顿帧期间又有命中：以原先的 timeScale 为基准，不会越拖越慢
            if (Mathf.Approximately(Time.timeScale, 1f)) _savedScale = 1f;
            Time.timeScale = 0.05f;
            // 用 realtime 等待，否则 timeScale=0.05 会让 sec 被拉长 20 倍
            yield return new WaitForSecondsRealtime(sec);
            Time.timeScale = _savedScale;
            _co = null;
        }
    }
}
