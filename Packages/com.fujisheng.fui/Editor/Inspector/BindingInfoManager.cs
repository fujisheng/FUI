using System;
using System.Collections.Generic;
using System.IO;

namespace FUI.Editor
{
    public class BindingInfoManager
    {
        static BindingInfoManager instance;
        Dictionary<string, ContextBindingInfo> bindings;
        public static BindingInfoManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new BindingInfoManager();
                }
                return instance;
            }
        }

        BindingInfoManager()
        {
            LoadAllBindingInfo("./FUI/BindingInfo/");
            CompilerEditor.OnCompilationComplete += () =>
            {
                LoadAllBindingInfo("./FUI/BindingInfo/");
            };
        }

        void LoadAllBindingInfo(string path)
        {
            var files = Directory.GetFiles(path, "*.binding", SearchOption.AllDirectories);
            bindings = new Dictionary<string, ContextBindingInfo>(files.Length);
            foreach (var filePath in files)
            {
                var file = File.ReadAllText(filePath);
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var info = Unity.Plastic.Newtonsoft.Json.JsonConvert.DeserializeObject<ContextBindingInfo>(file);
                bindings[fileName] = info;
            }
        }

        public ContextBindingInfo GetContextInfo(UIEntity entity)
        {
            if (bindings.TryGetValue(entity.BindingContextType.FullName, out var info))
            {
                return info;
            }
            return null;
        }
    }
}