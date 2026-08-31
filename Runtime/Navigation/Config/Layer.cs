namespace FUI.Navigation
{
    /// <summary>
    /// 独立 Canvas 的 sortingOrder 基准，每层预留 1000 个 LocalOrder。
    /// </summary>
    public enum Layer
    {
        Background = -30000,
        Scene = -20000,
        Panel = 0,
        Popup = 8000,
        Tips = 16000,
        Overlay = 24000,
        // Canvas.sortingOrder 是有符号 16 位值，Top 加 999 后仍必须小于等于 32767。
        Top = 30000,
    }
}
