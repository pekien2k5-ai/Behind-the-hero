namespace BTH.Core.Combat
{
    /// <summary>Id trạng thái cốt lõi; Content có thể đặt id riêng bằng chuỗi khác.</summary>
    public static class StatusIds
    {
        public static readonly StatusId Stagger = new StatusId("stagger");
        public static readonly StatusId Flanked = new StatusId("flanked");
        public static readonly StatusId Root = new StatusId("root");
        public static readonly StatusId Poison = new StatusId("poison");
        public static readonly StatusId Guard = new StatusId("guard");
    }
}
