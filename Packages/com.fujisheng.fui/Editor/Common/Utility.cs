namespace FUI.Editor
{
    public static class Utility
    {
        /// <summary>
        /// 获取当前编辑器平台信息
        /// </summary>
        /// <returns></returns>
        public static string GetPlatformInfo()
        {
#if UNITY_EDITOR_WIN
            return "win-x64";
#elif UNITY_EDITOR_OSX
            return "osx-x64";
#elif UNITY_EDITOR_LINUX
            return "linux-x64";
#else
            return "win-x64";
#endif
        }
    }
}