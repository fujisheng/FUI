using FUI.Bindable;

using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// UI管理器
    /// </summary>
    public class UIManager
    {
        /// <summary>
        /// 所有没有被销毁的容器
        /// </summary>
        Stack<Container> containers;

        /// <summary>
        /// View构造器
        /// </summary>
        IViewCreator creator;

        public UIManager(IViewCreator creator)
        {
            containers = new Stack<Container>();
            this.creator = creator;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public virtual void Initialize()
        {
            EnsureCreator();
            creator.Initialize();
        }

        /// <summary>
        /// 设置View构造器
        /// </summary>
        /// <param name="creator"></param>
        public void SetViewCreator(IViewCreator creator)
        {
            this.creator = creator;
        }

        /// <summary>
        /// 通过默认的ViewModel打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        /// <param name="param">打开时的参数</param>
        public void Open(string viewName, object param = null)
        {
            EnsureCreator();
            var view = creator.CreateView(new ViewCreateParam(viewName), out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        /// <summary>
        /// 通过默认的ViewModel异步打开一个界面
        /// </summary>
        /// <param name="viewName">界面名字</param>
        public async void OpenAsync(string viewName, object param = null)
        {
            EnsureCreator();
            var view = await creator.CreateViewAsync(new ViewCreateParam(viewName), default, out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        /// <summary>
        /// 通过指定的ViewModel打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            EnsureCreator();
            var view = creator.CreateView(new ViewCreateParam(viewName, typeof(TViewModel)), out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        /// <summary>
        /// 通过指定的ViewModel异步打开一个View
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async void OpenAsync<TViewModel>(string viewName, object param) where TViewModel : ObservableObject
        {
            EnsureCreator();
            var view = await creator.CreateViewAsync(new ViewCreateParam(viewName, typeof(TViewModel)), default, out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public void Open<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            EnsureCreator();
            var view = creator.CreateView(new ViewCreateParam(viewName, typeof(TViewModel), typeof(TBehavior)), out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        /// <summary>
        /// 通过指定ViewModel和Behavior来异步打开一个界面
        /// </summary>
        /// <typeparam name="TViewModel">指定的ViewModel类型</typeparam>
        /// <typeparam name="TBehavior">指定的ViewBehavior类型</typeparam>
        /// <param name="viewName">要打开的界面名字</param>
        public async void OpenAsync<TViewModel, TBehavior>(string viewName, object param) where TViewModel : ObservableObject where TBehavior : ViewBehavior<TViewModel>
        {
            EnsureCreator();
            var view = await creator.CreateViewAsync(new ViewCreateParam(viewName, typeof(TViewModel), typeof(TBehavior)), default, out var viewModel, out var behavior);
            InternalOpen(viewName, view, param, viewModel, behavior);
        }

        void InternalOpen(string viewName, View view, object param, ObservableObject viewModel, ViewBehavior behavior)
        {
            if (view == null)
            {
                UnityEngine.Debug.LogWarning($"open view:{viewName} failed");
                return;
            }
            var container = new Container(view, viewModel, behavior);
            containers.Push(container);
            container.Open(param);
        }

        void EnsureCreator()
        {
            if(creator == null)
            {
                throw new System.Exception("viewCreator is null, please call SetCreator first");
            }
        }

        /// <summary>
        /// 返回上一个界面
        /// </summary>
        public void Back()
        {

        }
    }
}
