using System;
using System.Diagnostics;
using System.Text;

using UnityEditor;

using Debug = UnityEngine.Debug;

namespace FUI.Editor
{
    public static class BatHelper
    {
        static bool hasError;

        /// <summary>
        /// 运行批处理文件
        /// </summary>
        /// <param name="path">批处理路径</param>
        /// <param name="onComplete">执行完成回调</param>
        /// <param name="refreshAsset">是否刷新资源</param>
        public static void RunBat(string path, Action<bool> onComplete, bool refreshAsset = true)
        {
            hasError = false;
            var p = new Process();
            p.StartInfo.FileName = path;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.CreateNoWindow = true;

            p.StartInfo.RedirectStandardInput = true;  // 重定向输入
            p.StartInfo.RedirectStandardOutput = true; // 重定向标准输出
            p.StartInfo.RedirectStandardError = true;  // 重定向错误输出 
            p.StartInfo.StandardOutputEncoding = Encoding.GetEncoding("GBK");
            p.StartInfo.StandardErrorEncoding = Encoding.GetEncoding("GBK");

            p.OutputDataReceived += new DataReceivedEventHandler(CmdOutputDataReceived);
            p.ErrorDataReceived += new DataReceivedEventHandler(CmdErrorDataReceived);

            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit(5000);
            p.Close();
            if (refreshAsset) AssetDatabase.Refresh();
            onComplete?.Invoke(hasError);
        }

        private static void CmdOutputDataReceived(object sender, DataReceivedEventArgs output)
        {
            if (String.IsNullOrEmpty(output.Data)) return;
            Debug.Log(output.Data);
        }

        private static void CmdErrorDataReceived(object sender, DataReceivedEventArgs output)
        {
            if (String.IsNullOrEmpty(output.Data)) return;
            Debug.LogError(output.Data);
            hasError = true;
        }
    }
}