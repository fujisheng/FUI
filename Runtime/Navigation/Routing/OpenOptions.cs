namespace FUI.Navigation
{
    /// <summary>
    /// 单次打开的导航提交方式，不属于 Route 固有配置。
    /// </summary>
    public enum OpenMode
    {
        /// <summary>保留当前页面并压入新页面。</summary>
        Push,
        /// <summary>新页面成功创建和绑定后关闭当前焦点页面。</summary>
        Replace,
    }

    /// <summary>
    /// 非多实例 Route 已经存在时，显式传入的新 ViewModel 应如何处理。
    /// </summary>
    public enum ExistingViewModelMode
    {
        /// <summary>只允许继续使用同一个实例；传入不同实例时抛出异常。</summary>
        Keep,
        /// <summary>保留当前页面和 Handle，把当前投影重新绑定到新实例。</summary>
        Rebind,
    }

    /// <summary>
    /// 一次打开操作的可选参数。
    /// </summary>
    public readonly struct OpenOptions
    {
        public OpenMode OpenMode { get; }

        public ExistingViewModelMode ExistingViewModelMode { get; }

        public OpenOptions(
            OpenMode openMode,
            ExistingViewModelMode existingViewModelMode = ExistingViewModelMode.Keep)
        {
            OpenMode = openMode;
            ExistingViewModelMode = existingViewModelMode;
        }

        public static OpenOptions Push => new OpenOptions(OpenMode.Push);

        public static OpenOptions Replace => new OpenOptions(OpenMode.Replace);

        /// <summary>保留当前导航位置，并把已存在页面重新绑定到本次传入的数据。</summary>
        public static OpenOptions Rebind => new OpenOptions(
            OpenMode.Push,
            ExistingViewModelMode.Rebind);
    }
}
