using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace FUI.UGUI
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
        /// 同步创建游戏实体
        /// </summary>
        /// <param name="path">资源路径</param>
        GameObject CreateGameObject(string path);

        /// <summary>
        /// 异步创建游戏实体
        /// </summary>
        /// <param name="path">路径</param>
        ValueTask<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken = null);

        /// <summary>
        /// 销毁一个游戏实体
        /// </summary>
        void DestroyGameObject(GameObject gameObject);

        /// <summary>
        /// 释放这个资源加载器
        /// </summary>
        void Release();
    }
}