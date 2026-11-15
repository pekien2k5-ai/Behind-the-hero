namespace BTH.Core.Run
{
    /// <summary>Core "xin" rung màn hình; Unity tự quyết định có làm hay không.</summary>
    public readonly struct CameraShakeHintEvent
    {
        public readonly float Strength;
        public readonly float Duration;

        public CameraShakeHintEvent(float strength, float duration)
        {
            Strength = strength;
            Duration = duration;
        }
    }
}
