namespace FUI.BindingDescriptor
{
    /// <summary>
    /// 命令绑定描述
    /// </summary>
    public sealed class CommandBindingDescriptor
    {
        internal CommandBindingDescriptor() { }

        /// <summary>
        /// 目标Element路径
        /// </summary>
        /// <param name="target">路径</param>
        /// <returns></returns>
        public CommandBindingDescriptor ToTarget(string target)
        {
            return this;
        }

        /// <summary>
        /// 目标Command
        /// </summary>
        /// <param name="command">目标命令名字  请使用nameof(XElement.XCommand)</param>
        /// <returns></returns>
        public CommandBindingDescriptor ToCommand(string command)
        {
            return this;
        }
    }
}