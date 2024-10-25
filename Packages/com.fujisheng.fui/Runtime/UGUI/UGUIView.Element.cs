using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
    public partial class UGUIView : IElement
	{
		/// <summary>
		/// �洢���е��Ӿ�Ԫ��
		/// </summary>
		Dictionary<ElementKey, IElement> elements;

		/// <summary>
		/// �洢Ĭ�ϵ��Ӿ�Ԫ��
		/// </summary>
		Dictionary<string, IElement> defaultElements;

		string elementName;
		string IElement.Name => elementName;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// ��ʼ�����������Ӿ�Ԫ��
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
			//��ȡ���е��Ӿ�Ԫ�����
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
		/// <param name="elementName">����Ӿ�Ԫ�ص�����</param>
		/// <param name="visualElement">Ҫ��ӵ��Ӿ�Ԫ��</param>
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
		/// ��ȡһ���Ӿ�Ԫ��
		/// </summary>
		/// <typeparam name="T">�Ӿ�Ԫ������</typeparam>
		/// <param name="path">·��</param>
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
		/// ��ȡһ���Ӿ�Ԫ�� ����ж���Ӿ�Ԫ���򷵻ص�һ��
		/// </summary>
		/// <param name="path">·��</param>
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