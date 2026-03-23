using FUI.Bindable;

using UnityEngine;

namespace FUI.UGUI
{
	/// <summary>
	/// 适用于UGUI的元素基类
	/// </summary>
	public class Element : MonoBehaviour, IElement
	{
		/// <summary>
		/// 根元素的名字
		/// </summary>
		public const string RootName = "__Root";

		/// <summary>
		/// 元素名字
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// 父节点
		/// </summary>
		public IElement Parent { get; internal set; }

		/// <summary>
		/// 资源加载器
		/// </summary>
		protected IAssetLoader AssetLoader { get; private set; }

		/// <summary>
		/// 可见性
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
		/// 初始化  内部调用
		/// </summary>
		/// <param name="assetLoader">资源加载器</param>
		internal void InternalInitialize(IAssetLoader assetLoader)
		{
			if (initialized)
			{
				return;
			}

			this.AssetLoader = assetLoader;
			this.Name = Utility.RemoveCloneSuffix(gameObject.name);

			Visible = new BindableProperty<bool>(gameObject.activeSelf, (oldValue, newValue) => gameObject.SetActive(newValue));

			Position = new BindableProperty<Vector3>(transform.position, (oldValue, newValue) => transform.position = newValue);
			LocalPosition = new BindableProperty<Vector3>(transform.localPosition, (oldValue, newValue) => transform.localPosition = newValue);
			Rotation = new BindableProperty<Quaternion>(transform.rotation, (oldValue, newValue) => transform.rotation = newValue);
			LocalRotation = new BindableProperty<Quaternion>(transform.localRotation, (oldValue, newValue) => transform.localRotation = newValue);
			LocalScale = new BindableProperty<Vector3>(transform.localScale, (oldValue, newValue) => transform.localScale = newValue);

			OnInitialize();

			Canvas.willRenderCanvases += SyncTransformToBindable;

			initialized = true;
		}

		/// <summary>
		/// 释放 内部调用
		/// </summary>
		internal void InternalRelease()
		{
			if (!initialized)
			{
				return;
			}

			initialized = false;

			Canvas.willRenderCanvases -= SyncTransformToBindable;

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
        /// 在 Canvas 渲染前同步 Transform 的实际值到 BindableProperty，仅在有变更时执行  TODO 有待优化  类似的实现双向绑定的时候 如果有其他情况修改了其属性的值  可能获取到的值不是最新的
        /// </summary>
        void SyncTransformToBindable()
		{
			if (!initialized || Position == null)
			{
				return;
			}

			if (this == null || !transform.hasChanged)
			{
				return;
			}

			var currentPosition = transform.position;
			var cachedPosition = Position.Value;
			if (!Approximately(currentPosition, cachedPosition))
			{
				Position.Value = currentPosition;
			}

			var currentRotation = transform.rotation;
			var cachedRotation = Rotation.Value;
			if (!Approximately(currentRotation.eulerAngles, cachedRotation.eulerAngles))
			{
				Rotation.Value = currentRotation;
			}

			var currentLocalPosition = transform.localPosition;
			var cachedLocalPosition = LocalPosition.Value;
			if (!Approximately(currentLocalPosition, cachedLocalPosition))
			{
				LocalPosition.Value = currentLocalPosition;
			}

			var currentLocalRotation = transform.localRotation;
			var cachedLocalRotation = LocalRotation.Value;
			if (!Approximately(currentLocalRotation.eulerAngles, cachedLocalRotation.eulerAngles))
			{
				LocalRotation.Value = currentLocalRotation;
			}

			var currentLocalScale = transform.localScale;
			var cachedLocalScale = LocalScale.Value;
			if (!Approximately(currentLocalScale, cachedLocalScale))
			{
				LocalScale.Value = currentLocalScale;
			}

			// 重置变更标志，避免重复同步
			transform.hasChanged = false;
		}

		/// <summary>
		/// 比较两个 Vector3 是否近似相等（用于避免浮点抖动）
		/// </summary>
		bool Approximately(Vector3 a, Vector3 b)
		{
			const float Epsilon = 0.0001f;

			return
				Mathf.Abs(a.x - b.x) < Epsilon &&
				Mathf.Abs(a.y - b.y) < Epsilon &&
				Mathf.Abs(a.z - b.z) < Epsilon;
		}

		/// <summary>
		/// 当初始化时
		/// </summary>
		protected virtual void OnInitialize() { }

		/// <summary>
		/// 当释放时
		/// </summary>
		protected virtual void OnRelease() { }
	}
}