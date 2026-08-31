using System;
using System.Collections.Generic;

using FUI.Diagnostics;

using UnityEngine;

namespace FUI.Rendering.UGUI
{
	/// <summary>
	/// 维护 View 内 Element 的层级遍历、唯一索引和强类型查找逻辑。
	/// </summary>
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

		/// <summary>
		/// 所有的子元素
		/// </summary>
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
			var initializedElements = new List<Element>();

			try
			{
				var openList = new Queue<Transform>();
				openList.Enqueue(gameObject.transform);
				var elementsBuffer = new List<IElement>();
				// 获取所有视觉元素组件，包含 View 自身。
				while (openList.Count > 0)
				{
					var current = openList.Dequeue();

					elementsBuffer.Clear();
					current.GetComponents(elementsBuffer);

					var continueFind = true;
					foreach (var element in elementsBuffer)
					{
						if (ReferenceEquals(element, excludedElement))
						{
							continue;
						}

						var isContainer = element is IContainerElement;
						if (isContainer)
						{
							continueFind = false;
						}

						Element initializedElement = null;
						if (!ReferenceEquals(element, this) && element is Element uguiElement)
						{
							uguiElement.Parent = this;
							uguiElement.InternalInitialize(assetLoader);
							initializedElement = uguiElement;
						}

						var thisName = (this as IElement).Name;
						if (!RegisterElement(element, null, true))
						{
							initializedElement?.InternalRelease();
							continue;
						}

						if (initializedElement != null)
						{
							initializedElements.Add(initializedElement);
						}

						// Root 只增加查询别名，不重复加入所有权列表。
						if (element.Name == thisName && element.Name != Element.RootName)
						{
							RegisterElement(element, Element.RootName, false);
						}
					}

					if (continueFind)
					{
						for (var index = 0; index < current.childCount; index++)
						{
							openList.Enqueue(current.GetChild(index));
						}
					}
				}
			}
			catch
			{
				for (var index = initializedElements.Count - 1; index >= 0; index--)
				{
					initializedElements[index].InternalRelease();
				}
				children.Clear();
				elementUniqueLookup.Clear();
				namedElements.Clear();
				throw;
			}
		}

        /// <summary>
        /// 添加一个视觉元素
        /// </summary>
        /// <param name="element">要添加的视觉元素</param>
        internal void AddElement(IElement element) => RegisterElement(element, null, true);

        /// <summary>
        /// 添加一个视觉元素
        /// </summary>
        /// <param name="element">要添加的视觉元素</param>
        /// <param name="name">指定的特殊名字</param>
		bool RegisterElement(IElement element, string name, bool ownsElement)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			name ??= element.Name;
			if (string.IsNullOrEmpty(name))
			{
				throw new Exception($"{this} as element failed, element name is null or empty.");
			}

			var key = new ElementUniqueKey(name, element.GetType());
			if (elementUniqueLookup.TryGetValue(key, out var existing))
			{
				if (ReferenceEquals(existing, element))
				{
					return true;
				}

				Log.Instance.LogError(
					$"View '{((IView)this).Name}' ignored duplicate element key '{name}' for type " +
					$"'{element.GetType().FullName}'. The first registered element will be used.");
				return false;
			}

			elementUniqueLookup[key] = element;
			if (ownsElement && !children.Contains(element))
			{
				children.Add(element);
			}
            AddChildToNamedElements(element, name);
			return true;
		}

		/// <summary>
		/// 添加一个Element到名字映射中
		/// </summary>
		/// <param name="element">要添加的Element</param>
		/// <param name="name">指定的名字</param>
		void AddChildToNamedElements(IElement element, string name = null)
		{
			name ??= element.Name;
            if (!namedElements.TryGetValue(name, out var list))
			{
				list = new List<IElement> { element };
				namedElements[name] = list;
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
        /// <param name="element">要移除的元素</param>
        internal void RemoveElement(IElement element) => InternalRemoveElement(element, null);

        /// <summary>
        /// 移除一个子元素
        /// </summary>
        /// <param name="element"></param>
        void InternalRemoveElement(IElement element, string name)
		{
            var thisName = (this as IElement).Name;
            //如果是自身 额外移除名字为Root的元素
            if (element.Name == thisName && thisName != Element.RootName && name == null)
			{
				InternalRemoveElement(element, Element.RootName);
            }

			name ??= element.Name;

            var key = new ElementUniqueKey(name, element.GetType());
			if(!elementUniqueLookup.Remove(key))
			{
				return;
			}

            var removed = name == element.Name && children.Remove(element);
            RemoveFromNamedElements(element, name);

			if(removed && element is Element uguiElement)
			{
				uguiElement.InternalRelease();
			}
		}

        /// <summary>
        /// 移除一个Element从名字映射中
        /// </summary>
        /// <param name="element"></param>
        void RemoveFromNamedElements(IElement element, string name)
		{
            name ??= element.Name;

            if (!namedElements.TryGetValue(name, out var list))
			{
				return;
			}

			list.Remove(element);
		}

		/// <summary>
		/// 移除所有的子元素
		/// </summary>
        internal void RemoveAllElements()
		{
			if(children == null || children.Count <= 0)
			{
				return;
			}

			for (var index = children.Count - 1; index >= 0; index--)
			{
				var element = children[index];
                if (element is Element uguiElement)
                {
                    uguiElement.InternalRelease();
                }
            }

			children.Clear();
            elementUniqueLookup.Clear();
            namedElements.Clear();
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
				if(!key.elementType.IsAssignableFrom(item.GetType()))
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
