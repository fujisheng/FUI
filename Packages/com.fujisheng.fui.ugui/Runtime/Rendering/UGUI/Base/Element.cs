using FUI.Binding;

using System;
using FUI.Diagnostics;
using UnityEngine;

namespace FUI.Rendering.UGUI
{
	/// <summary>
	/// 适用于UGUI的元素基类
	/// </summary>
	public partial class Element : MonoBehaviour, IElement, IVisibilityElement, IExternalValueSource, IWorldPositionElement, ILocalPositionElement, ILocalScaleElement, IQuaternionRotationElement, ILocalQuaternionRotationElement
	{
		/// <summary>
		/// 描述 Element 初始化与释放事务的内部状态。
		/// </summary>
		enum LifecycleState
		{
			Uninitialized,
			Initializing,
			Initialized,
			Releasing,
		}

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
		public BindableProperty<bool> Visible { get; private set; }

		LifecycleState lifecycleState;
		ElementLifetime lifetime;

		/// <summary>
		/// 标识当前由绑定系统同步的 Transform 属性，避免 Unity 回调造成重复写入。
		/// </summary>
		enum TransformProperty
		{
			None,
			Position,
			LocalPosition,
			Rotation,
			LocalRotation,
			LocalScale,
		}

		TransformProperty syncingTransformProperty;

        partial void InitializeSpatialValues();

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
			if (lifecycleState == LifecycleState.Initialized)
			{
				return;
			}
			if (lifecycleState != LifecycleState.Uninitialized)
			{
				throw new InvalidOperationException($"Element '{name}' cannot initialize while in state {lifecycleState}.");
			}

			lifecycleState = LifecycleState.Initializing;
			lifetime = new ElementLifetime();
			try
			{
				AssetLoader = assetLoader;
				Name = Utility.RemoveCloneSuffix(gameObject.name);

				Visible = CreateProperty(gameObject.activeSelf, (oldValue, newValue) => gameObject.SetActive(newValue));

				Position = CreateProperty(transform.position, (oldValue, newValue) =>
				{
					if (syncingTransformProperty != TransformProperty.Position)
					{
						transform.position = newValue;
					}
				});
				LocalPosition = CreateProperty(transform.localPosition, (oldValue, newValue) =>
				{
					if (syncingTransformProperty != TransformProperty.LocalPosition)
					{
						transform.localPosition = newValue;
					}
				});
				Rotation = CreateProperty(transform.rotation, (oldValue, newValue) =>
				{
					if (syncingTransformProperty != TransformProperty.Rotation)
					{
						transform.rotation = newValue;
					}
				});
				LocalRotation = CreateProperty(transform.localRotation, (oldValue, newValue) =>
				{
					if (syncingTransformProperty != TransformProperty.LocalRotation)
					{
						transform.localRotation = newValue;
					}
				});
				LocalScale = CreateProperty(transform.localScale, (oldValue, newValue) =>
				{
					if (syncingTransformProperty != TransformProperty.LocalScale)
					{
						transform.localScale = newValue;
					}
				});

				InitializeSpatialValues();
				OnInitialize();

				lifecycleState = LifecycleState.Initialized;
			}
			catch
			{
				lifetime.Dispose();
				ResetLifetimeState();
				throw;
			}
		}

		/// <summary>
		/// 释放 内部调用
		/// </summary>
		internal void InternalRelease()
		{
			if (lifecycleState == LifecycleState.Uninitialized || lifecycleState == LifecycleState.Releasing)
			{
				return;
			}

			lifecycleState = LifecycleState.Releasing;
			try
			{
				OnRelease();
			}
			catch (Exception exception)
			{
				Log.Instance.LogError($"Element '{Name}' release hook failed: {exception.Message}");
				Log.Instance.LogException(exception);
			}
			finally
			{
				lifetime?.Dispose();
				ResetLifetimeState();
			}
		}

		/// <summary>
		/// 创建并登记一个随 Element 生命周期释放的可绑定属性。
		/// </summary>
		protected BindableProperty<T> CreateProperty<T>(T initialValue = default, ValueChangedHandler<T> onValueChanged = null)
		{
			return Track(new BindableProperty<T>(initialValue, onValueChanged));
		}

		/// <summary>
		/// 登记一个随 Element 生命周期释放的资源。
		/// </summary>
		protected T Track<T>(T resource) where T : IDisposable
		{
			if (lifetime == null)
			{
				throw new InvalidOperationException($"Element '{name}' does not have an active lifetime.");
			}
			return lifetime.Track(resource);
		}

		/// <summary>
		/// 登记一个随 Element 生命周期执行的清理动作。
		/// </summary>
		protected void TrackCleanup(Action cleanup)
		{
			if (lifetime == null)
			{
				throw new InvalidOperationException($"Element '{name}' does not have an active lifetime.");
			}
			lifetime.TrackCleanup(cleanup);
		}

		void ResetLifetimeState()
		{
			lifetime = null;
			AssetLoader = null;
			Parent = null;
			lifecycleState = LifecycleState.Uninitialized;
		}

        /// <summary>
        /// 在 Canvas 渲染前同步 Transform 的实际值到 BindableProperty，确保双向绑定读取最新外部状态。
        /// </summary>
		void IExternalValueSource.SynchronizeExternalValues()
		{
			if (lifecycleState != LifecycleState.Initialized || Position == null)
			{
				return;
			}

			if (this == null || !transform.hasChanged)
			{
				return;
			}

			SynchronizeTransformValues();
			OnSynchronizeExternalValues();
			transform.hasChanged = false;
		}

		void SynchronizeTransformValues()
		{

			var currentPosition = transform.position;
			var cachedPosition = Position.Value;
			if (!Approximately(currentPosition, cachedPosition))
			{
				SyncBindableFromTransform(Position, currentPosition, TransformProperty.Position);
			}

			var currentRotation = transform.rotation;
			var cachedRotation = Rotation.Value;
			if (!Approximately(currentRotation.eulerAngles, cachedRotation.eulerAngles))
			{
				SyncBindableFromTransform(Rotation, currentRotation, TransformProperty.Rotation);
			}

			var currentLocalPosition = transform.localPosition;
			var cachedLocalPosition = LocalPosition.Value;
			if (!Approximately(currentLocalPosition, cachedLocalPosition))
			{
				SyncBindableFromTransform(LocalPosition, currentLocalPosition, TransformProperty.LocalPosition);
			}

			var currentLocalRotation = transform.localRotation;
			var cachedLocalRotation = LocalRotation.Value;
			if (!Approximately(currentLocalRotation.eulerAngles, cachedLocalRotation.eulerAngles))
			{
				SyncBindableFromTransform(LocalRotation, currentLocalRotation, TransformProperty.LocalRotation);
			}

			var currentLocalScale = transform.localScale;
			var cachedLocalScale = LocalScale.Value;
			if (!Approximately(currentLocalScale, cachedLocalScale))
			{
				SyncBindableFromTransform(LocalScale, currentLocalScale, TransformProperty.LocalScale);
			}

		}

		protected virtual void OnSynchronizeExternalValues()
		{
		}

		/// <summary>
		/// 将 Transform 的实际值同步给绑定属性，避免属性回调把相同值再次写回 Transform
		/// </summary>
		void SyncBindableFromTransform<T>(BindableProperty<T> property, T value, TransformProperty transformProperty)
		{
			var previousProperty = syncingTransformProperty;
			syncingTransformProperty = transformProperty;

			try
			{
				property.Value = value;
			}
			finally
			{
				syncingTransformProperty = previousProperty;
			}
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
