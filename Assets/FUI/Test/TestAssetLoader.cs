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

        public Task<GameObject> CreateGameObjectAsync(string path)
        {
            throw new NotImplementedException();
        }

        public void DestroyGameObject(GameObject gameObject)
        {
            throw new NotImplementedException();
        }

        public T Load<T>(string path) where T : UnityEngine.Object
        {
            return Resources.Load<T>(path);
        }

        public async Task<T> LoadAsync<T>(string path) where T : UnityEngine.Object
        {
            throw new NotImplementedException();
        }

        public void Release()
        {
            
        }
    }
}
