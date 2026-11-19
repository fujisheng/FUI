using System;
using System.Collections.Generic;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 页面资源、绑定及表现组合的唯一不可变描述；不承载标记或样式文本。
    /// </summary>
    public sealed class ViewDefinition
    {
        public string Name
        {
            get;
        }
        public string HtmlUri
        {
            get;
        }
        public IReadOnlyList<string> ImageGroupKeys
        {
            get;
        }
        public Func<ViewContext, IReadOnlyList<ElementBinding>> CreateBindings
        {
            get;
        }
        public IReadOnlyList<IViewBehavior> Behaviors
        {
            get;
        }
        public Action<ViewContext, View> Configure
        {
            get;
        }
        /// <summary>
        /// 只读取绑定能力描述，不创建 DOM 或适配器；供导入及编辑器校验。
        /// </summary>
        public IReadOnlyList<ElementBinding> DescribeBindings(IViewAssets assets)
        {
            using var context = ViewContext.Metadata(this, assets);
            return context.DescribeBindings();
        }
        /// <summary>
        /// 组合已有作者子树；失败时逆序释放已建立的表现资源。
        /// </summary>
        public View Compose(ViewScope scope, IViewAssets assets, global::URUI.Animation.IUIClock clock)
        {
            View view = null;
            try
            {
                view = new View(Name, scope);
                view.ConfigureGeneration(current =>
                {
                    var context = new ViewContext(scope, this, assets, clock);
                    current.Own(context);
                    current.SetBindings(context.DescribeBindings());
                    foreach (var behavior in Behaviors)
                    {
                        context.Own((behavior ?? throw new InvalidOperationException("页面行为不能为空。")).Attach(context));
                    }
                    Configure?.Invoke(context, current);
                });
                return view;
            }
            catch (Exception constructionFailure)
            {
                try
                {
                    view?.Destroy();
                }
                catch (Exception cleanupFailure)
                {
                    throw new AggregateException(constructionFailure, cleanupFailure);
                }
                throw;
            }
        }
        public ViewDefinition(string name, string htmlUri,
            Func<ViewContext, IReadOnlyList<ElementBinding>> createBindings,
            IEnumerable<string> imageGroupKeys = null,
            IEnumerable<IViewBehavior> behaviors = null, Action<ViewContext, View> configure = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("页面名不能为空。", nameof(name));
            }
            if (string.IsNullOrWhiteSpace(htmlUri))
            {
                throw new ArgumentException("HTML URI 不能为空。", nameof(htmlUri));
            }
            Name = name;
            HtmlUri = htmlUri;
            CreateBindings = createBindings ?? throw new ArgumentNullException(nameof(createBindings));
            ImageGroupKeys = Array.AsReadOnly(new List<string>(imageGroupKeys ?? Array.Empty<string>()).ToArray());
            Behaviors = Array.AsReadOnly(new List<IViewBehavior>(behaviors ?? Array.Empty<IViewBehavior>()).ToArray());
            Configure = configure;
        }
    }
}
