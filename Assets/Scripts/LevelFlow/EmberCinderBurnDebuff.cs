using BrushSpirit.Player;
using UnityEngine;

namespace BrushSpirit.LevelFlow
{
    /// <summary>
    /// 烬谷余烬灼烧：离开烬口后仍短时间持续掉血；再次接触会刷新持续时间。
    /// </summary>
    public class EmberCinderBurnDebuff : MonoBehaviour
    {
        float _remaining;
        float _sinceTick;
        float _damagePerTick = 2f;
        float _tickInterval = 0.45f;
        GameObject _source;

        public void Apply(float duration, float damagePerTick, float tickInterval, GameObject source)
        {
            _remaining = Mathf.Max(_remaining, duration);
            _damagePerTick = damagePerTick;
            _tickInterval = Mathf.Max(0.12f, tickInterval);
            _source = source != null ? source : gameObject;
            _sinceTick = _tickInterval;
            enabled = true;
        }

        void Update()
        {
            if (_remaining <= 0f)
            {
                enabled = false;
                return;
            }

            _remaining -= Time.deltaTime;
            _sinceTick += Time.deltaTime;
            if (_sinceTick < _tickInterval) return;
            _sinceTick = 0f;

            var stats = GetComponent<PlayerStats>();
            stats?.TakeDamage(_damagePerTick, _source);
        }
    }
}
