namespace BTH.Core.Run
{
    /// <summary>
    /// Nguồn ngẫu nhiên có state tường minh để snapshot/rewind tái lập đúng chuỗi.
    /// </summary>
    public interface IRandomSource
    {
        uint State { get; set; }

        int NextInt(int minInclusive, int maxExclusive);

        float NextFloat();
    }
}
