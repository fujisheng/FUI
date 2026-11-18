namespace FUI.Rendering
{
    /// <summary>为内部投影工厂准备结构与绑定；几何读取仍遵守后端的同步边界。</summary>
    public interface IViewPreparation
    {
        void PrepareForBinding();
    }
}
