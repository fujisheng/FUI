using System;
using System.Collections.Generic;

namespace FUI
{
    /// <summary>
    /// 视图接口
    /// </summary>
    public interface IView : IElement
    {
        /// <summary>
        /// 视图名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 层级
        /// </summary>
        int Layer { get; set; }

        /// <summary>
        /// 顺序
        /// </summary>
        int Order { get; set; }

        /// <summary>
        /// 所有子节点
        /// </summary>
        IReadOnlyList<IElement> Elements { get; }

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <typeparam name="T">子节点的类型</typeparam>
        /// <param name="path">子节点的路径</param>
        /// <returns></returns>
        T GetElement<T>(string path) where T : IElement;

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <param name="path">子节点路径</param>
        /// <param name="elementType">子节点类型</param>
        /// <returns></returns>
        IElement GetElement(string path, Type elementType);

        /// <summary>
        /// 销毁
        /// </summary>
        void Destroy();
    }
}
