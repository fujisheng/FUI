using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using UnityEngine.Networking;

namespace FUI.Editor
{
    public static class ExtensionMethods
    {
        public static TaskAwaiter<object> GetAwaiter(this UnityWebRequestAsyncOperation op)
        {
            var tcs = new TaskCompletionSource<object>();
            op.completed += (obj) => { tcs.SetResult(null); };
            return tcs.Task.GetAwaiter();
        }
    }
}