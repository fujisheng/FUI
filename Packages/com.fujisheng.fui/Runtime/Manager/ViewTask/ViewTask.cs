namespace FUI.Manager
{
    /// <summary>
    /// 界面任务
    /// </summary>
    abstract class ViewTask
    {
        internal abstract string ViewName { get; set; }
        internal abstract bool IsCompleted { get; set; }
        internal abstract UIEntity Result { get; set; }
        internal abstract void Execute();
        internal abstract void Complete();
        internal abstract void Cancel();
    }
}