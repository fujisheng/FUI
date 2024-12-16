namespace FUI.Test
{
    public class TestAssetLoaderFactory : FUI.UGUI.IAssetLoaderFactory
    {
        public FUI.UGUI.IAssetLoader Create(string viewName)
        {
            return new TestAssetLoader();
        }
    }
}