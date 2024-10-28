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

		string IElement.Name => gameObject.name;

		public IElement Parent { get; private set; }

		public IElement[] Children { get; private set; }

		/// <summary>
		/// ��ʼ�����������Ӿ�Ԫ��
		/// </summary>
		protected virtual void InitializeElements()
		{
            this.elements = new Dictionary<ElementKey, IElement>();

            var openList = new Queue<Transform>();
			openList.Enqueue(this.gameObject.transform);
			var elementsTemp = new List<UGUIView>();
			//��ȡ���е��Ӿ�Ԫ����� ��������
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
			if(!(element is UGUIView uguiElement))
			{
				return;
			}

			var key = new ElementKey(element.Name, element.GetType());
            elements[key] = uguiElement;
        }

		/// <summary>
		/// �Ƴ�һ����Ԫ��
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
        /// �Ƴ����е���Ԫ��
        /// </summary>
        public void RemoveAllChildren()
		{
			foreach(var item in elements.Values)
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
                return default;
            }
			return (T)element;
		}
	}
}