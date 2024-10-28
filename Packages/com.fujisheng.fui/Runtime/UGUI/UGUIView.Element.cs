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

		string IElement.Name => gameObject.name;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// 初始化这个界面的视觉元素
		/// </summary>
		protected virtual void InitializeElements()
		{
            this.elements = new Dictionary<ElementKey, IElement>();

            var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<UGUIView>();
			//获取所有的视觉元素组件 包含自身
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();
				elementsTemp.Clear();
				current.GetComponents(elementsTemp);
				bool continueFind = true;
				foreach (var element in elementsTemp)
				{
					if (element != null)
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
			if(!(element is UGUIView uguiElement))
			{
				return;
			}

			var key = new ElementKey(element.Name, element.GetType());
            elements[key] = uguiElement;
        }

		/// <summary>
		/// 移除一个子元素
		/// </summary>
		/// <param name="element"></param>
		public void RemoveChild(IElement element)
		{
            if (!(element is UGUIView uguiElement))
            {
                return;
            }

            elements.Remove(new ElementKey(element.Name, element.GetType()));
			uguiElement.Destroy();
        }

        /// <summary>
        /// 移除所有的子元素
        /// </summary>
        public void RemoveAllChildren()
		{
			foreach(var item in elements.Values)
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
                return default;
            }
			return (T)element;
		}
	}
}