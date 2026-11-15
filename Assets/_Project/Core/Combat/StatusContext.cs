namespace BTH.Core.Combat
{
    /// <summary>
    /// Ngữ cảnh cho status effect: unit sở hữu và các cổng cần thiết để phát event/áp sát thương.
    /// Không chứa luật chơi để effect thuần dữ liệu.
    /// </summary>
    public sealed class StatusContext
    {
        public StatusContext(Units.UnitState owner, Run.IEventBus events, Units.IUnitRegistry units)
        {
            Owner = owner;
            Events = events;
            Units = units;
        }

        public Units.UnitState Owner { get; }

        public Run.IEventBus Events { get; }

        public Units.IUnitRegistry Units { get; }
    }
}
