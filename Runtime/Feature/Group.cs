using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Feature
{
    internal struct ModelInfo
    {
        /// <summary>
        /// 自动初始化
        /// </summary>
        internal readonly bool autoConstruct;
        /// <summary>
        /// Model值
        /// </summary>
        internal readonly IModel value;

        /// <summary>
        /// 构造一个Model信息
        /// </summary>
        /// <param name="value">内容</param>
        /// <param name="autoConstruct">是否是自动初始化</param>
        internal ModelInfo(IModel value, bool autoConstruct = false)
        {
            this.value = value;
            this.autoConstruct = autoConstruct;
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        internal bool IsEmpty => value == null;
    }

    public class Group
    {
        internal readonly Type groupType;
        List<ModelInfo> models;
        Dictionary<Type, Action<IModel>> modifyListeners;

        internal Group(Type groupType)
        {
            this.groupType = groupType;
            models = new List<ModelInfo>();
            modifyListeners = new Dictionary<Type, Action<IModel>>();
        }

        internal void Initialize(IEnumerable<Type> modelTypes)
        {
            var types = modelTypes.Where(t=>t.GetCustomAttribute<AutoConstructOnInitializationAttribute>() != null);

            foreach (var type in types)
            {
                var model = (IModel)Activator.CreateInstance(type);
                UnityEngine.Debug.Log($"{groupType} addModel:{model}");
                Add(model);
            }
        }

        public void AddModifyListener<T>(Action<T> listener)
        {
            if(modifyListeners.TryGetValue(typeof(T), out var onModify))
            {
                onModify += (m) => listener?.Invoke((T)m);
            }
            else
            {
                modifyListeners.Add(typeof(T), (m) => listener?.Invoke((T)m));
            }
        }

        void TriggerModifyEvent<T>(T model) where T : IModel
        {
            if(!modifyListeners.TryGetValue(typeof(T), out var onModify))
            {
                return;
            }

            onModify?.Invoke(model);
        }

        public void Add<T>(T model, bool autoConstruct = false) where T : IModel
        {
            if(models.Exists(m => m.value.GetType() == model.GetType()))
            {
                return;
            }
            models.Add(new ModelInfo(model, autoConstruct));
            model.Initialize();
            TriggerModifyEvent(model);
        }

        public void Remove<T>() where T : IModel
        {
            var model = models.FirstOrDefault(m => m.value.GetType() == typeof(T));
            if(model.IsEmpty)
            {
                return;
            }
            model.value.Release();
            models.Remove(model);
        }

        public ModifySignal<T> Get<T>(out T model) where T : IModel
        {
            var exsitsModel = models.FirstOrDefault(m => m.value.GetType() == typeof(T));
            if(exsitsModel.IsEmpty)
            {
                throw new Exception($"get model exception, {groupType.Name} dont have {typeof(T)}, please call add first");
            }

            model = (T)exsitsModel.value;
            return new ModifySignal<T>(model, (m)=> TriggerModifyEvent(m));
        }

        internal void Release(bool withAutoConstruct = false)
        {
            if(models.Count == 0)
            {
                return;
            }

            for(int i = models.Count - 1; i >= 0; i--)
            {
                var model = models[i];
                if (!withAutoConstruct && model.autoConstruct)
                {
                    continue;
                }
                model.value.Release();
                models.RemoveAt(i);
                if (modifyListeners.TryGetValue(model.GetType(), out var action))
                {
                    modifyListeners.Remove(model.GetType());
                }
            }
        }
    }
}
