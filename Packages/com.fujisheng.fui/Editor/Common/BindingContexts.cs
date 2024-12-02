using System.Collections.Generic;
using System.IO;

namespace FUI.Editor
{
    /// <summary>
    /// 所有的UI绑定上下文信息
    /// </summary>
    public class BindingContexts
    {
        static BindingContexts instance;
        Dictionary<string, ContextBindingInfo> contexts;
        public static BindingContexts Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BindingContexts();
                }
                return instance;
            }
        }

        BindingContexts()
        {
            LoadAllBindingContextInfo("./FUI/BindingInfo/");
        }

        [AssemblyCompilationFinished]
        static void OnCompilationComplete(string file, List<object> messages)
        {
            if(instance == null)
            {
                return;
            }

            instance.LoadAllBindingContextInfo("./FUI/BindingInfo/");
        }

        void LoadAllBindingContextInfo(string path)
        {
            var files = Directory.GetFiles(path, "*.binding", SearchOption.AllDirectories);
            contexts = new Dictionary<string, ContextBindingInfo>(files.Length);
            foreach (var filePath in files)
            {
                var file = File.ReadAllText(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var info = Unity.Plastic.Newtonsoft.Json.JsonConvert.DeserializeObject<ContextBindingInfo>(file);
                contexts[fileName] = info;
            }
        }

        /// <summary>
        /// 根据实体获取上下文信息
        /// </summary>
        /// <param name="entity">ui实体</param>
        /// <returns></returns>
        public ContextBindingInfo GetContextInfo(UIEntity entity)
        {
            if(entity == null)
            {
                return null;
            }

            if (contexts.TryGetValue(entity.BindingContextType.FullName, out var info))
            {
                return info;
            }
            return null;
        }
    }
}