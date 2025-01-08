using FUI.Bindable;
using UnityEngine;

namespace FUI.UGUI
{
    /// <summary>
    /// ������UGUI��Ԫ�ػ���
    /// </summary>
    public class Element : MonoBehaviour, IElement
    {
        /// <summary>
        /// Ԫ������
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// ���ڵ�
        /// </summary>
        public IElement Parent { get; internal set; }

        /// <summary>
        /// ��Դ������
        /// </summary>
        protected IAssetLoader AssetLoader { get; private set; }

        /// <summary>
        /// �Ƿ�ɼ�
        /// </summary>
        bool IElement.Visible
        {
            get => Visible.Value;
            set => Visible.Value = value;
        }

        /// <summary>
        /// �ɼ���
        /// </summary>
        public BindableProperty<bool> Visible;

        bool initialized = false;

        #region Transform
        /// <summary>
        /// transform.position
        /// </summary>
        public BindableProperty<Vector3> Position { get; private set; }

        /// <summary>
        /// transform.localPosition
        /// </summary>
        public BindableProperty<Vector3> LocalPosition { get; private set; }

        /// <summary>
        /// transform.rotation
        /// </summary>
        public BindableProperty<Quaternion> Rotation { get; private set; }

        /// <summary>
        /// transform.localRotation
        /// </summary>
        public BindableProperty<Quaternion> LocalRotation { get; private set; }

        /// <summary>
        /// transform.localScale
        /// </summary>
        public BindableProperty<Vector3> LocalScale { get; private set; }
        #endregion

        /// <summary>
        /// ��ʼ��  �ڲ�����
        /// </summary>
        /// <param name="assetLoader">��Դ������</param>
        internal void InternalInitialize(IAssetLoader assetLoader)
        {
            if (initialized)
            {
                return;
            }

            this.AssetLoader = assetLoader;
            this.Name = gameObject.name;

            Visible = new BindableProperty<bool>(gameObject.activeSelf, (oldValue, newValue) => gameObject.SetActive(newValue));

            Position = new BindableProperty<Vector3>(transform.position, (oldValue, newValue) => transform.position = newValue);
            LocalPosition = new BindableProperty<Vector3>(transform.localPosition, (oldValue, newValue) => transform.localPosition = newValue);
            Rotation = new BindableProperty<Quaternion>(transform.rotation, (oldValue, newValue) => transform.rotation = newValue);
            LocalRotation = new BindableProperty<Quaternion>(transform.localRotation, (oldValue, newValue) => transform.localRotation = newValue);
            LocalScale = new BindableProperty<Vector3>(transform.localScale, (oldValue, newValue) => transform.localScale = newValue);

            OnInitialize();

            initialized = true;
        }

        /// <summary>
        /// �ͷ� �ڲ�����
        /// </summary>
        internal void InternalRelease()
        {
            Visible.Dispose();
            Position.Dispose();
            LocalPosition.Dispose();
            Rotation.Dispose();
            LocalRotation.Dispose();
            LocalScale.Dispose();

            OnRelease();
            AssetLoader = null;
        }

        /// <summary>
        /// ����ʼ��ʱ
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// ���ͷ�ʱ
        /// </summary>
        protected virtual void OnRelease() { }
    }
}