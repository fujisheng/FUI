using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// 视觉元素唯一标识查找表
		/// </summary>
		Dictionary<ElementUniqueKey, IElement> elementUniqueLookup;

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
			this.elementUniqueLookup = new Dictionary<ElementUniqueKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();
			this.children = new List<IElement>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsBuffer = new List<Element>();
			//获取所有的视觉元素组件 包含自身
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();

				//获取当前元素的所有元素组件
				elementsBuffer.Clear();
				current.GetComponents(elementsBuffer);

				bool continueFind = true;
				foreach (var element in elementsBuffer)
				{
                    //如果这个元素不是自身则需要初始化
                    if (element != this)
					{
                        element.Parent = this;
                        element.InternalInitialize(AssetLoader);
                    }

					AddElement(element);

					//如果这个元素是容器元素且不是自身则不再继续向下查找
					if (element is IContainerElement && element != this)
					{
						continueFind = false;
					}
				}

				if (continueFind)
				{
                    //否则继续向下查找
                    for (int i = 0; i < current.transform.childCount; i++)
                    {
                        openList.Enqueue(current.transform.GetChild(i));
                    }
                }
			}
		}

		/// <summary>
		/// 添加一个视觉元素
		/// </summary>
		/// <param name="element">要添加的视觉元素</param>
		public void AddElement(IElement element)
		{
			if (string.IsNullOrEmpty(element.Name))
			{
				throw new Exception($"{this} ass element failed, element name is null or empty.");
			}

			var key = new ElementUniqueKey(element.Name, element.GetType());
			if (elementUniqueLookup.ContainsKey(key))
			{
				return;
			}

			elementUniqueLookup[key] = element;
			children.Add(element);
            AddChildToNamedElements(element);
		}

		/// <summary>
		/// 添加一个Element到名字映射中
		/// </summary>
		/// <param name="element">要添加的Element</param>
		void AddChildToNamedElements(IElement element)
		{
			if (!namedElements.TryGetValue(element.Name, out var list))
			{
				list = new List<IElement> { element };
				namedElements[element.Name] = list;
				return;
			}

			if (list.Contains(element))
			{
				return;
			}

			list.Add(element);
		}

		/// <summary>
		/// 移除一个子元素
		/// </summary>
		/// <param name="element"></param>
		public void RemoveElement(IElement element)
		{
			var key = new ElementUniqueKey(element.Name, element.GetType());
			elementUniqueLookup.Remove(key);
            children.Remove(element);
            RemoveFromNamedElements(element);

			if(element is Element uguiElement)
			{
				uguiElement.InternalRelease();
			}
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

			list.Remove(element);
		}

		/// <summary>
		/// 移除所有的子元素
		/// </summary>
		public void RemoveAllElements()
		{
			if(children.Count <= 0)
			{
				return;
			}

            for (int i = children.Count - 1; i >= 0; i--)
			{
				RemoveElement(children[i]);
			}
		}

		/// <summary>
		/// 清空所有子元素
		/// </summary>
		public void ClearElements()
		{
            elementUniqueLookup.Clear();
            namedElements.Clear();
            children.Clear();
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
			if(string.IsNullOrEmpty(path) || elementType == null)
			{
				throw new Exception($"{this} get element failed 'path:{path} type:{elementType}', path or element type is null.");
			}

            var key = new ElementUniqueKey(path, elementType);
            if (!elementUniqueLookup.TryGetValue(key, out var element))
            {
                return GetOrCacheElementToElementUniqueLookup(ref key);
            }
            return element;
        }

        /// <summary>
        /// 尝试从缓存中获取一个视觉元素  如果没有则缓存  防止通过基类型来查找
        /// </summary>
        /// <typeparam name="T">ElementType</typeparam>
        /// <param name="path">ElementPath</param>
        /// <returns></returns>
        IElement GetOrCacheElementToElementUniqueLookup(ref ElementUniqueKey key)
		{
            if (!namedElements.TryGetValue(key.elementPath, out var list))
            {
                return default;
            }

            foreach (var item in list)
            {
                if (item.GetType() != key.elementType)
                {
                    continue;
                }

                elementUniqueLookup[key] = item;
                return item;
            }

            return default;
        }
	}
}