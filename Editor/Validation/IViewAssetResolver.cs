using FUI.Navigation;

namespace FUI.Editor.Validation
{
    /// <summary>
    /// 将 Route 解析为待验证 View Prefab 资源路径。
    /// </summary>
    public interface IViewAssetResolver
    {
        bool TryResolve(Route route, out string assetPath);
    }
}
