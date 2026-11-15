using BTH.Core.Run;

namespace BTH.Core.Combat
{
    /// <summary>Gợi ý rung màn hình; Unity quyết định cách diễn.</summary>
    public sealed class CameraShakeEffect : IGameEffect
    {
        private readonly float _strength;
        private readonly float _duration;

        public CameraShakeEffect(float strength, float duration)
        {
            _strength = strength;
            _duration = duration;
        }

        public int Priority => 90;

        public void Resolve(EffectContext ctx)
        {
            ctx.Events?.Publish(new CameraShakeHintEvent(_strength, _duration));
        }
    }
}
