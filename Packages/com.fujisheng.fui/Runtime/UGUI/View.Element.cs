using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// �洢���е��Ӿ�Ԫ��
		/// </summary>
		Dictionary<ElementKey, IElement> elements;

		/// <summary>
		/// �������ִ洢���Ӿ�Ԫ��
		/// </summary>
		Dictionary<string, List<IElement>> namedElements;

		string IElement.Name => gameObject.name;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// ��ʼ�����������Ӿ�Ԫ��
		/// </summary>
		protected virtual void InitializeElements()
		{
			this.elements = new Dictionary<ElementKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<View>();
			//��ȡ���е��Ӿ�Ԫ����� ��������
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();
				elementsTemp.Clear();
				current.GetComponents(elementsTemp);
				bool continueFind = true;
				foreach (var element in elementsTemp)
				{
                    //������Ԫ������������ӵ���Ԫ���� ���ٳ�ʼ���Ҳ�������AssetLoader
                    if (element.gameObject == this.gameObject)
					{
						AddChild(element);
                    }
                    //������Ԫ�����Ӿ�Ԫ���Ҳ����Լ����ʼ��
                    else
					{
                        element.Parent = this;
                        element.AssetLoader = AssetLoader;
                        element.InternalInitialize();

                        AddChild(element);
                    }

					//������Ԫ��������Ԫ���Ҳ����������ټ������²���
					if (element is IContainerElement && element.gameObject != this.gameObject)
					{
						continueFind = false;
					}
				}

				if (!continueFind)
				{
					continue;
				}

				//����������²���
				for (int i = 0; i < current.transform.childCount; i++)
				{
					openList.Enqueue(current.transform.GetChild(i));
				}
			}
		}

		/// <summary>
		/// ���һ���Ӿ�Ԫ��
		/// </summary>
		/// <param name="element">Ҫ��ӵ��Ӿ�Ԫ��</param>
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
		/// ���һ��Element������ӳ����
		/// </summary>
		/// <param name="element">Ҫ��ӵ�Element</param>
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
		/// �Ƴ�һ����Ԫ��
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
        /// �Ƴ�һ��Element������ӳ����
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
		/// �Ƴ����е���Ԫ��
		/// </summary>
		public void RemoveAllChildren()
		{
			foreach (var item in elements.Values)
			{
				RemoveChild(item);
			}
		}

		/// <summary>
		/// ��ȡһ���Ӿ�Ԫ��
		/// </summary>
		/// <typeparam name="T">�Ӿ�Ԫ������</typeparam>
		/// <param name="path">·��</param>
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
		/// ���Դӻ����л�ȡһ���Ӿ�Ԫ��  ���û���򻺴�  ��ֹͨ��������������
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