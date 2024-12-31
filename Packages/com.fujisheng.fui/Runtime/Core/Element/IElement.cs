using System;
using System.Collections.Generic;

namespace FUI
{
    public interface IElement
    {
        /// <summary>
        /// 名字
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 父节点
        /// </summary>
        IElement Parent { get; }

        /// <summary>
        /// 所有子节点
        /// </summary>
        IReadOnlyList<IElement> Children { get; }

        /// <summary>
        /// 添加一个子节点
        /// </summary>
        /// <param name="element">要添加的子节点</param>
        void AddChild(IElement element);

        /// <summary>
        /// 移除一个子节点
        /// </summary>
        /// <param name="element">要移除的子节点</param>
        void RemoveChild(IElement element);

        /// <summary>
        /// 移除所有子节点
        /// </summary>
        void RemoveAllChildren();

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <typeparam name="T">子节点的类型</typeparam>
        /// <param name="path">子节点的路径</param>
        /// <returns></returns>
        T GetChild<T>(string path) where T : IElement;

        /// <summary>
        /// 获取一个子节点
        /// </summary>
        /// <param name="path">子节点路径</param>
        /// <param name="elementType">子节点类型</param>
        /// <returns></returns>
        IElement GetChild(string path, Type elementType);
    }
}
