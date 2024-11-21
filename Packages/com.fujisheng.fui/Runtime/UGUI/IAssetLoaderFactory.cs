namespace FUI.UGUI
{
    /// <summary>
    /// IAssetLoader��������
    /// </summary>
    public interface IAssetLoaderFactory
    {
        IAssetLoader Create(string viewName);
    }
}