using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using FUI.Navigation;
using FUI.Resources;
using FUI.Transitions;

namespace FUI.Rendering.UGUI
{
    /// <summary>
    /// 资源加载器
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        /// <returns></returns>
        T Load<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <typeparam name="T">资源类型</typeparam>
        /// <param name="path">资源路径</param>
        ValueTask<T> LoadAsync<T>(string path, CancellationToken? cancellationToken = null) where T : Object;

        /// <summary>
        /// 同步创建对象实例
        /// </summary>
        /// <param name="path">资源路径</param>
        GameObject CreateGameObject(string path);

        /// <summary>
        /// 异步创建对象实例
        /// </summary>
        /// <param name="path">路径</param>
        ValueTask<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken = null);

        /// <summary>
        /// 销毁一个对象实例
        /// </summary>
        void DestroyGameObject(GameObject gameObject);

        /// <summary>
        /// 释放这个资源加载器
        /// </summary>
        void Release();
    }

    /// <summary>
    /// 为动态资源提供可独立释放的 Lease。
    /// </summary>
    public interface ILeaseAssetLoader : IAssetLoader
    {
        IAssetLease<T> LoadLease<T>(string path) where T : Object;

        IAssetLease<GameObject> InstantiateLease(string path);
    }
}
