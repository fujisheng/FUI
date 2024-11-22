using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// 存储所有的视觉元素
		/// </summary>
		Dictionary<ElementKey, IElement> elements;

		/// <summary>
		/// 根据名字存储的视觉元素
		/// </summary>
		Dictionary<string, List<IElement>> namedElements;

		string IElement.Name => gameObject.name;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// 初始化这个界面的视觉元素
		/// </summary>
		protected virtual void InitializeElements()
		{
			this.elements = new Dictionary<ElementKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<View>();
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
						AddChild(element);
                    }
                    //如果这个元素是视觉元素且不是自己则初始化
                    else
					{
                        element.Parent = this;
                        element.AssetLoader = AssetLoader;
                        element.InternalInitialize();

                        AddChild(element);
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
		public void AddChild(IElement element)
		{
			if (!(element is View uguiElement))
			{
				return;
			}

			var key = new ElementKey(element.Name, element.GetType());
			elements[key] = uguiElement;
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
		public void RemoveChild(IElement element)
		{
			if (!(element is View uguiElement))
			{
				return;
			}

			elements.Remove(new ElementKey(element.Name, element.GetType()));
			uguiElement.Destroy();
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
		public void RemoveAllChildren()
		{
			foreach (var item in elements.Values)
			{
				RemoveChild(item);
			}
		}

		/// <summary>
		/// 获取一个视觉元素
		/// </summary>
		/// <typeparam name="T">视觉元素类型</typeparam>
		/// <param name="path">路径</param>
		/// <returns></returns>
		public T GetChild<T>(string path) where T : IElement
		{
			var key = new ElementKey(path, typeof(T));
			if (!elements.TryGetValue(key, out var element))
			{
				return GetOrCacheElement<T>(path);
			}
			return (T)element;
		}

		/// <summary>
		/// 尝试从缓存中获取一个视觉元素  如果没有则缓存  防止通过基类型来查找
		/// </summary>
		/// <typeparam name="T">ElementType</typeparam>
		/// <param name="path">ElementPath</param>
		/// <returns></returns>
		T GetOrCacheElement<T>(string path) where T : IElement
		{
			if (!namedElements.TryGetValue(path, out var list))
			{
				return default;
			}

			foreach (var item in list)
			{
				if (!(item is T tItem))
				{
					continue;
				}

				var key = new ElementKey(path, typeof(T));
				elements[key] = item;
				return tItem;
			}

			return default;
		}
	}
}