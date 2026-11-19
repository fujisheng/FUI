using System.Collections.Generic;
using UnityEngine;
using global::URUI.Resources;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 由宿主解释逻辑 URI；框架不猜测 Unity Resources 路径。
    /// </summary>
    public interface IViewAssets
    {
        TextAsset LoadText(string uri);
        /// <summary>
        /// 正式页面由 Editor 编译，不在运行时解析 HTML/CSS。
        /// </summary>
        CompiledDocumentAsset LoadCompiled(string uri);
        /// <summary>
        /// 由 HTML/CSS 实际依赖生成的规范 URL 文件集合，包含字体原始字节。
        /// </summary>
        IReadOnlyList<TextResource> LoadDependencies(string htmlUri);
        ImageLease AcquireImage(string uri);
        IReadOnlyList<ImageAsset> LoadImageGroup(string key);
    }
}
