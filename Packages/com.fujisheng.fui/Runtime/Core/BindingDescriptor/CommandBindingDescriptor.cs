namespace FUI.BindingDescriptor
{
    /// <summary>
    /// ���������
    /// </summary>
    public sealed class CommandBindingDescriptor
    {
        internal CommandBindingDescriptor() { }

        /// <summary>
        /// Ŀ��Element·��
        /// </summary>
        /// <param name="target">·��</param>
        /// <returns></returns>
        public CommandBindingDescriptor ToTarget(string target)
        {
            return this;
        }

        /// <summary>
        /// Ŀ��Command
        /// </summary>
        /// <param name="command">Ŀ����������  ��ʹ��nameof(XElement.XCommand)</param>
        /// <returns></returns>
        public CommandBindingDescriptor ToCommand(string command)
        {
            return this;
        }
    }
}