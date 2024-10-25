using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
    public partial class UGUIView : IElement
	{
		/// <summary>
		/// 存储所有的视觉元素
		/// </summary>
		Dictionary<ElementKey, IElement> elements;

		/// <summary>
		/// 存储默认的视觉元素
		/// </summary>
		Dictionary<string, IElement> defaultElements;

		string elementName;
		string IElement.Name => elementName;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// 初始化这个界面的视觉元素
		/// </summary>
		protected virtual void InitializeElements()
		{
			if (this.elements == null)
			{
				this.elements = new Dictionary<ElementKey, IElement>();
                defaultElements = new Dictionary<string, IElement>();
			}

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elements = new List<UGUIView>();
			//获取所有的视觉元素组件
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();
				elements.Clear();
				current.GetComponents(elements);
				bool continueFind = true;
				foreach (var element in elements)
				{
					if (element != null)
					{
						element.Parent = this;
						element.assetLoader = assetLoader;
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
		/// <param name="elementName">这个视觉元素的名字</param>
		/// <param name="visualElement">要添加的视觉元素</param>
		public void AddChild(IElement element)
		{
			var key = new ElementKey(element.Name, element.GetType());
			if (elements.ContainsKey(key))
			{
				UnityEngine.Debug.LogWarning($"{element.Name} already contains element {key} will replace it");
			}
			elements[key] = element;

			if (!defaultElements.ContainsKey(elementName))
			{
				defaultElements[elementName] = element;
			}
		}

		public void RemoveChild(IElement element)
		{
			throw new System.NotImplementedException();
		}

		public void RemoveAllChildren()
		{
			throw new System.NotImplementedException();
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
			if (!elements.TryGetValue(key, out var visualElement))
            {
                return default;
            }
			return (T)visualElement;
		}

		/// <summary>
		/// 获取一个视觉元素 如果有多个视觉元素则返回第一个
		/// </summary>
		/// <param name="path">路径</param>
		/// <returns></returns>
		public IElement GetChild(string path)
		{
			if (!defaultElements.TryGetValue(path, out var visualElement))
			{
				return default;
			}
			return visualElement;
		}
	}
}