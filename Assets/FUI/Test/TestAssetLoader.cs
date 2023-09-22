using System;
using System.Threading.Tasks;

using UnityEngine;

namespace FUI.Test
{
    internal class TestAssetLoader : IAssetLoader
    {
        public GameObject CreateGameObject(string path)
        {
            var prefab = Load<GameObject>(path);
            return UnityEngine.Object.Instantiate(prefab);
        }

        public async Task<GameObject> CreateGameObjectAsync(string path)
        {
            var prefab = await LoadAsync<GameObject>(path);
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

        public async Task<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            return null;
        }

        public void Release()
        {
            
        }
    }
}
