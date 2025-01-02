using System;
using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using FUI.UGUI;
using System.Runtime.CompilerServices;

namespace FUI.Test
{
    public class ResourceRequestAwaiter : INotifyCompletion
    {
        public Action Continuation;
        public ResourceRequest resourceRequest;
        public bool IsCompleted => resourceRequest.isDone;
        public ResourceRequestAwaiter(ResourceRequest resourceRequest)
        {
            this.resourceRequest = resourceRequest;

            this.resourceRequest.completed += Accomplish;
        }

        public void OnCompleted(Action continuation) => this.Continuation = continuation;

        public void Accomplish(AsyncOperation asyncOperation) => Continuation?.Invoke();

        public void GetResult() { }
    }

    public static class ResourceRequestExtensions
    {
        public static ResourceRequestAwaiter GetAwaiter(this ResourceRequest resourceRequest)
        {
            return new ResourceRequestAwaiter(resourceRequest);
        }
    }

    internal class TestAssetLoader : IAssetLoader
    {
        public GameObject CreateGameObject(string path)
        {
            var prefab = Load<GameObject>(path);
            return UnityEngine.Object.Instantiate(prefab);
        }

        public async ValueTask<GameObject> CreateGameObjectAsync(string path, CancellationToken? cancellationToken)
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

        public async ValueTask<T> LoadAsync<T>(string path, CancellationToken? cancellationToken) where T : UnityEngine.Object
        {
            var request = Resources.LoadAsync(path);
            await request;
            return request.asset as T;
        }

        public void Release()
        {
            Resources.UnloadUnusedAssets();
        }
    }
}
