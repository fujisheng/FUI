using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
	public partial class View
	{
		/// <summary>
		/// �Ӿ�Ԫ��Ψһ��ʶ���ұ�
		/// </summary>
		Dictionary<ElementUniqueKey, IElement> elementUniqueLookup;

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
			this.elementUniqueLookup = new Dictionary<ElementUniqueKey, IElement>();
			this.namedElements = new Dictionary<string, List<IElement>>();
			this.children = new List<IElement>();

			var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsBuffer = new List<Element>();
			//��ȡ���е��Ӿ�Ԫ����� ��������
			while (openList.Count > 0)
			{
				var current = openList.Dequeue();

				//��ȡ��ǰԪ�ص�����Ԫ�����
				elementsBuffer.Clear();
				current.GetComponents(elementsBuffer);

				bool continueFind = true;
				foreach (var element in elementsBuffer)
				{
                    //������Ԫ�ز�����������Ҫ��ʼ��
                    if (element != this)
					{
                        element.Parent = this;
                        element.InternalInitialize(AssetLoader);
                    }

					AddElement(element);

					//������Ԫ��������Ԫ���Ҳ����������ټ������²���
					if (element is IContainerElement && element != this)
					{
						continueFind = false;
					}
				}

				if (continueFind)
				{
                    //����������²���
                    for (int i = 0; i < current.transform.childCount; i++)
                    {
                        openList.Enqueue(current.transform.GetChild(i));
                    }
                }
			}
		}

		/// <summary>
		/// ���һ���Ӿ�Ԫ��
		/// </summary>
		/// <param name="element">Ҫ��ӵ��Ӿ�Ԫ��</param>
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
		/// ���һ��Element������ӳ����
		/// </summary>
		/// <param name="element">Ҫ��ӵ�Element</param>
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
		/// �Ƴ�һ����Ԫ��
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
        /// �Ƴ�һ��Element������ӳ����
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
		/// �Ƴ����е���Ԫ��
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
		/// ���������Ԫ��
		/// </summary>
		public void ClearElements()
		{
            elementUniqueLookup.Clear();
            namedElements.Clear();
            children.Clear();
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
        /// ���Դӻ����л�ȡһ���Ӿ�Ԫ��  ���û���򻺴�  ��ֹͨ��������������
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