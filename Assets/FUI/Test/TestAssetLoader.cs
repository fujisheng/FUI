using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using FUI.UGUI;

namespace FUI.Test
{
    internal class TestAssetLoader : IAssetLoader
    {
        public GameObject CreateGameObject(string path)
        {
            var prefab = Load<GameObject>(path);
            return UnityEngine.Object.Instantiate(prefab);
        }

        public async Task<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken)
        {
            var prefab = await LoadAsync<GameObject>(path, cancellationToken);
            return UnityEngine.Object.Instantiate(prefab);
        }

        public void DestroyGameObject(GameObject gameObject)
        {
            UnityEngine.Object.DestroyImmediate(gameObject, false);
        }

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }

        public async Task<T> LoadAsync<T>(string path, CancellationToken? cancellationToken) where T : UnityEngine.Object
        {
            await Task.Delay(1000);
            return Resources.Load<T>(path);
        }

        public void Release()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
