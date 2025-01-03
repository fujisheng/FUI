using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// 存储所有的视觉元素
		/// </summary>
		Dictionary<ElementUniqueKey, IElement> elements;

		/// <summary>
		/// 根据名字存储的视觉元素
		/// </summary>
		Dictionary<string, List<IElement>> namedElements;

		List<IElement> children;
        /// <summary>
        /// 这个View的所有视觉元素 包含自身
        /// </summary>
        public IReadOnlyList<IElement> Elements => children;

		/// <summary>
		/// 初始化这个界面的视觉元素
		/// </summary>
		protected virtual void InitializeElements()
		{
			this.elements = new Dictionary<ElementUniqueKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();
			this.children = new List<IElement>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<Element>();
			//获取所有的视觉元素组件 包含自身
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();
				elementsTemp.Clear();
				current.GetComponents(elementsTemp);
				bool continueFind = true;
				foreach (var element in elementsTemp)
				{
                    //如果这个元素是自身则添加到子元素中 不再初始化且不用设置AssetLoader
                    if (element.gameObject == this.gameObject)
					{
						AddElement(element);
                    }
                    //如果这个元素是视觉元素且不是自己则初始化
                    else
					{
                        element.Parent = this;
                        element.InternalInitialize(AssetLoader);

                        AddElement(element);
                    }

					//如果这个元素是容器元素且不是自身则不再继续向下查找
					if (element is IContainerElement && element.gameObject != this.gameObject)
					{
						continueFind = false;
					}
				}

				if (!continueFind)
				{
					continue;
				}

				//否则继续向下查找
				for (int i = 0; i < current.transform.childCount; i++)
				{
					openList.Enqueue(current.transform.GetChild(i));
				}
			}
		}

		/// <summary>
		/// 添加一个视觉元素
		/// </summary>
		/// <param name="element">要添加的视觉元素</param>
		public void AddElement(IElement element)
		{
			if (!(element is Element uguiElement))
			{
				return;
			}

			var key = new ElementUniqueKey(element.Name, element.GetType());
			elements[key] = uguiElement;
			children.Add(uguiElement);
            AddChildToNamedElements(uguiElement);
		}

		/// <summary>
		/// 添加一个Element到名字映射中
		/// </summary>
		/// <param name="element">要添加的Element</param>
		void AddChildToNamedElements(IElement element)
		{
			if (!namedElements.TryGetValue(element.Name, out var list))
			{
				list = new List<IElement> { };
			}

			if (list.Contains(element))
			{
				return;
			}

			list.Add(element);
			namedElements[element.Name] = list;
		}

		/// <summary>
		/// 移除一个子元素
		/// </summary>
		/// <param name="element"></param>
		public void RemoveElement(IElement element)
		{
			if (!(element is Element uguiElement))
			{
				return;
			}

			elements.Remove(new ElementUniqueKey(element.Name, element.GetType()));
            children.Remove(uguiElement);
			uguiElement.InternalOnRelease();
			RemoveFromNamedElements(uguiElement);
		}

        /// <summary>
        /// 移除一个Element从名字映射中
        /// </summary>
        /// <param name="element"></param>
        void RemoveFromNamedElements(IElement element)
		{
			if (!namedElements.TryGetValue(element.Name, out var list))
			{
				return;
			}

			if (list.Contains(element))
			{
				list.Remove(element);
			}
		}

		/// <summary>
		/// 移除所有的子元素
		/// </summary>
		public void RemoveAllElements()
		{
			foreach (var item in elements.Values)
			{
				RemoveElement(item);
			}
		}

		/// <summary>
		/// 获取一个视觉元素
		/// </summary>
		/// <typeparam name="T">视觉元素类型</typeparam>
		/// <param name="path">路径</param>
		/// <returns></returns>
		public T GetElement<T>(string path) where T : IElement
		{
			return (T)GetElement(path, typeof(T));
		}

        /// <summary>
        /// 获取一个视觉元素
        /// </summary>
        /// <param name="path">路径</param>
        /// <param name="elementType">元素类型</param>
        /// <returns></returns>
        public IElement GetElement(string path, Type elementType)
        {
            var key = new ElementUniqueKey(path, elementType);
            if (!elements.TryGetValue(key, out var element))
            {
                return GetOrCacheElement(path, elementType);
            }
            return element;
        }

        /// <summary>
        /// 尝试从缓存中获取一个视觉元素  如果没有则缓存  防止通过基类型来查找
        /// </summary>
        /// <typeparam name="T">ElementType</typeparam>
        /// <param name="path">ElementPath</param>
        /// <returns></returns>
        IElement GetOrCacheElement(string path, Type elementType)
		{
            if (!namedElements.TryGetValue(path, out var list))
            {
                return default;
            }

            foreach (var item in list)
            {
                if (item.GetType() != elementType)
                {
                    continue;
                }

                var key = new ElementUniqueKey(path, elementType);
                elements[key] = item;
                return item;
            }

            return default;
        }
	}
}