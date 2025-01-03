using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// �洢���е��Ӿ�Ԫ��
		/// </summary>
		Dictionary<ElementUniqueKey, IElement> elements;

		/// <summary>
		/// �������ִ洢���Ӿ�Ԫ��
		/// </summary>
		Dictionary<string, List<IElement>> namedElements;

		List<IElement> children;
        /// <summary>
        /// ���View�������Ӿ�Ԫ�� ��������
        /// </summary>
        public IReadOnlyList<IElement> Elements => children;

		/// <summary>
		/// ��ʼ�����������Ӿ�Ԫ��
		/// </summary>
		protected virtual void InitializeElements()
		{
			this.elements = new Dictionary<ElementUniqueKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();
			this.children = new List<IElement>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<Element>();
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
						AddElement(element);
                    }
                    //������Ԫ�����Ӿ�Ԫ���Ҳ����Լ����ʼ��
                    else
					{
                        element.Parent = this;
                        element.InternalInitialize(AssetLoader);

                        AddElement(element);
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
		public void RemoveAllElements()
		{
			foreach (var item in elements.Values)
			{
				RemoveElement(item);
			}
		}

		/// <summary>
		/// ��ȡһ���Ӿ�Ԫ��
		/// </summary>
		/// <typeparam name="T">�Ӿ�Ԫ������</typeparam>
		/// <param name="path">·��</param>
		/// <returns></returns>
		public T GetElement<T>(string path) where T : IElement
		{
			return (T)GetElement(path, typeof(T));
		}

        /// <summary>
        /// ��ȡһ���Ӿ�Ԫ��
        /// </summary>
        /// <param name="path">·��</param>
        /// <param name="elementType">Ԫ������</param>
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
        /// ���Դӻ����л�ȡһ���Ӿ�Ԫ��  ���û���򻺴�  ��ֹͨ��������������
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