using System.Threading;
using System.Threading.Tasks;

using UnityEngine;

namespace FUI.UGUI
{
    public interface IAssetLoader
    {
        T Load<T>(string path) where T : Object;
        Task<T> LoadAsync<T>(string path, CancellationToken? cancellationToken = null) where T : Object;
        GameObject CreateGameObject(string path);
        Task<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken = null);
        void DestroyGameObject(GameObject gameObject);
        void Release();
    }
}