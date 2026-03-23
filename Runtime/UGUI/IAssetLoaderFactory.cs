namespace FUI.UGUI
{
    /// <summary>
    /// IAssetLoader创建工厂
    /// </summary>
    public interface IAssetLoaderFactory
    {
        IAssetLoader Create(string viewName);
    }
}