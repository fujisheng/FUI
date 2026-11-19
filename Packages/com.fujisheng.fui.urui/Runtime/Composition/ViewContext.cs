using System;
using System.Collections.Generic;
using global::URUI.Animation;
using global::URUI.Documents;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 拥有页面的表现行为与资源；DOM、业务绑定和 Presenter 仍由 View/ViewInstance 拥有。
    /// </summary>
    public sealed class ViewContext : IDisposable
    {
        readonly List<IDisposable> owned = new List<IDisposable>();
        bool disposed;
        bool describing;
        readonly ViewScope scope;
        public Document Document => Scope.Root.Document;
        public ViewScope Scope
        {
            get
            {
                if (describing || scope == null)
                {
                    throw new InvalidOperationException("绑定描述阶段不能访问 DOM；请将查询放在适配器工厂内。");
                }
                return scope;
            }
        }
        public ViewDefinition Definition
        {
            get;
        }
        public IViewAssets Assets
        {
            get;
        }
        public IUIClock Clock
        {
            get;
        }
        public ViewContext(ViewScope scope, ViewDefinition definition, IViewAssets assets, IUIClock clock)
            : this(scope ?? throw new ArgumentNullException(nameof(scope)), definition, assets, clock, false) { }
        ViewContext(ViewScope scope, ViewDefinition definition, IViewAssets assets, IUIClock clock, bool metadataOnly)
        {
            this.scope = scope;
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Assets = assets ?? throw new ArgumentNullException(nameof(assets));
            Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (!metadataOnly && !ReferenceEquals(scope.Document.Surface.UIClock, clock))
            {
                throw new ArgumentException("视图时钟必须与所属Document一致；请在Stage加载前配置。", nameof(clock));
            }
        }
        internal static ViewContext Metadata(ViewDefinition definition, IViewAssets assets)
            => new ViewContext(null, definition, assets, UnityUIClock.Unscaled, true);
        internal IReadOnlyList<ElementBinding> DescribeBindings()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ViewContext));
            }
            describing = true;
            try
            {
                var bindings = Definition.CreateBindings(this) ?? throw new InvalidOperationException("页面未返回绑定描述：" + Definition.Name);
                return Array.AsReadOnly(new List<ElementBinding>(bindings).ToArray());
            }
            finally
            {
                describing = false;
            }
        }
        public T Own<T>(T resource) where T : IDisposable
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(ViewContext));
            }
            if (describing)
            {
                throw new InvalidOperationException("绑定描述阶段不能取得资源所有权。");
            }
            if (resource == null)
            {
                throw new ArgumentNullException(nameof(resource));
            }
            owned.Add(resource);
            return resource;
        }
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }
            disposed = true;
            Exception failure = null;
            for (var i = owned.Count - 1; i >= 0; i--)
            {
                try
                {
                    owned[i].Dispose();
                }
                catch (Exception exception)
                {
                    failure ??= exception;
                }
            }
            owned.Clear();
            if (failure != null)
            {
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }
    }
}
