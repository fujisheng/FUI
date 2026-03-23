using System;
using System.Collections.Generic;

namespace EntityComponent
{
    /// <summary>
    /// 实体
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// 实体名字
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 所有组件
        /// </summary>
        readonly List<Component> components;

        /// <summary>
        /// 构建一个实体
        /// </summary>
        internal Entity()
        {
            components = new List<Component>();
        }

        /// <summary>
        /// 构建一个实体
        /// </summary>
        /// <param name="name">实体名字</param>
        internal Entity(string name) : this()
        {
            this.Name = name;
        }

        /// <summary>
        /// 构建一个实体
        /// </summary>
        /// <param name="name">实体名字</param>
        /// <param name="components">实体默认的组件</param>
        internal Entity(string name, params Type[] components) : this(name)
        {
            foreach(var  component in components)
            {
                AddComponent(component);
            }
        }

        /// <summary>
        /// 确保一个类型是组件
        /// </summary>
        /// <param name="type">类型</param>
        /// <exception cref="Exception"></exception>
        void EnsureIsComponent(Type type)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                throw new Exception($"type '{type}' not type of '{typeof(Component)}'");
            }
        }

        /// <summary>
        /// 根据类型添加一个组件
        /// </summary>
        /// <param name="type">要添加的组件类型</param>
        /// <returns></returns>
        public Component AddComponent(Type type)
        {
            EnsureIsComponent(type);
            return AddComponentNotCheck(type);
        }

        /// <summary>
        /// 添加一个组件不检查组件类型
        /// </summary>
        /// <param name="type">组件类型</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        Component AddComponentNotCheck(Type type)
        {
            if (components.Exists(item => item.GetType() == type))
            {
                throw new System.Exception($"{this} areadly has component '{type}'");
            }

            var component = Activator.CreateInstance(type) as Component;
            component.InternalInitialize(this);
            components.Add(component);
            return component;
        }

        /// <summary>
        /// 添加一个组件
        /// </summary>
        /// <typeparam name="TComponent">组件类型</typeparam>
        /// <returns></returns>
        public TComponent AddComponent<TComponent>() where TComponent : Component
        {
            return (TComponent)AddComponentNotCheck(typeof(TComponent));
        }

        /// <summary>
        /// 移除一个组件
        /// </summary>
        /// <param name="type">要移除的组件类型</param>
        public void RemoveComponent(Type type)
        {
            EnsureIsComponent(type);
            RemoveComponentNotCheck(type);
        }

        /// <summary>
        /// 移除一个组件不检查组件类型
        /// </summary>
        /// <param name="type">要移除的组件类型</param>
        void RemoveComponentNotCheck(Type type)
        {
            var index = components.FindIndex(item=> item.GetType() == type);
            if(index < 0)
            {
                return;
            }

            var component = components[index];
            component.InternalRelease();
            components.RemoveAt(index);
        }

        /// <summary>
        /// 移除一个组件
        /// </summary>
        /// <typeparam name="TComponent">要移除的组件类型</typeparam>
        public void RemoveComponent<TComponent>() where TComponent : Component
        {
            RemoveComponentNotCheck(typeof(TComponent));
        }

        /// <summary>
        /// 获取一个组件
        /// </summary>
        /// <param name="type">要获取的组件类型</param>
        /// <returns></returns>
        public Component GetComponent(Type type)
        {
            EnsureIsComponent(type);
            return GetComponentNotCheck(type);
        }

        /// <summary>
        /// 获取一个组件不检查组件类型
        /// </summary>
        /// <param name="type">要获取的组件类型</param>
        /// <returns></returns>
        Component GetComponentNotCheck(Type type)
        {
            var index = components.FindIndex(item=>type.IsAssignableFrom(item.GetType()));
            if(index >= 0)
            {
                return components[index];
            }

            return null;
        }

        /// <summary>
        /// 获取一个组件
        /// </summary>
        /// <typeparam name="TComponent">组件类型</typeparam>
        /// <returns></returns>
        public TComponent GetComponent<TComponent>() where TComponent : Component
        {
            var index = components.FindIndex(item=>item is TComponent);
            if(index >= 0)
            {
                return (TComponent)components[index];
            }

            return default;
        }

        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <typeparam name="TComponent">要获取的组件类型</typeparam>
        /// <returns></returns>
        public IEnumerable<TComponent> GetComponents<TComponent>() where TComponent : Component
        {
            var result = new List<TComponent>();
            foreach( var component in components)
            {
                if(component is TComponent tComponent)
                {
                    result.Add(tComponent);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <typeparam name="TComponent">要获取的组件类型</typeparam>
        /// <param name="buffer">缓冲区</param>
        public void GetComponents<TComponent>(ICollection<TComponent> buffer) where TComponent : Component
        {
            foreach (var component in components)
            {
                if (component is TComponent tComponent)
                {
                    buffer.Add(tComponent);
                }
            }
        }

        /// <summary>
        /// 获取所有组件
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Component> GetComponents()
        {
            return components;
        }

        /// <summary>
        /// 判断是否有某种类型的组件
        /// </summary>
        /// <typeparam name="TComponent">组件类型</typeparam>
        /// <returns></returns>
        public bool HasComponent<TComponent>() where TComponent : Component
        {
            var index = components.FindIndex(item => item is TComponent);
            return index >= 0;
        }

        /// <summary>
        /// 当这个实体被销毁的时候
        /// </summary>
        internal void InternalOnDestroy()
        {
            foreach (var component in components)
            {
                component.InternalRelease();
            }
            components.Clear();
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Name) ? base.ToString() : Name;
        }
    }
}