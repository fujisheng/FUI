namespace FUI.UGUI
{
    /// <summary>
    /// IAssetLoader´´½¨¹¤³§
    /// </summary>
    public interface IAssetLoaderFactory
    {
        IAssetLoader Create(string viewName);
    }
}