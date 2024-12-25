using System;
using System.Collections.Generic;

namespace EntityComponent
{
    /// <summary>
    /// ʵ��
    /// </summary>
    public class Entity
    {
        /// <summary>
        /// ʵ������
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// �������
        /// </summary>
        readonly List<Component> components;

        /// <summary>
        /// ����һ��ʵ��
        /// </summary>
        internal Entity()
        {
            components = new List<Component>();
        }

        /// <summary>
        /// ����һ��ʵ��
        /// </summary>
        /// <param name="name">ʵ������</param>
        internal Entity(string name) : this()
        {
            this.Name = name;
        }

        /// <summary>
        /// ����һ��ʵ��
        /// </summary>
        /// <param name="name">ʵ������</param>
        /// <param name="components">ʵ��Ĭ�ϵ����</param>
        internal Entity(string name, params Type[] components) : this(name)
        {
            foreach(var  component in components)
            {
                AddComponent(component);
            }
        }

        /// <summary>
        /// ȷ��һ�����������
        /// </summary>
        /// <param name="type">����</param>
        /// <exception cref="Exception"></exception>
        void EnsureIsComponent(Type type)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                throw new Exception($"type '{type}' not type of '{typeof(Component)}'");
            }
        }

        /// <summary>
        /// �����������һ�����
        /// </summary>
        /// <param name="type">Ҫ��ӵ��������</param>
        /// <returns></returns>
        public Component AddComponent(Type type)
        {
            EnsureIsComponent(type);
            return AddComponentNotCheck(type);
        }

        /// <summary>
        /// ���һ�����������������
        /// </summary>
        /// <param name="type">�������</param>
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
        /// ���һ�����
        /// </summary>
        /// <typeparam name="TComponent">�������</typeparam>
        /// <returns></returns>
        public TComponent AddComponent<TComponent>() where TComponent : Component
        {
            return (TComponent)AddComponentNotCheck(typeof(TComponent));
        }

        /// <summary>
        /// �Ƴ�һ�����
        /// </summary>
        /// <param name="type">Ҫ�Ƴ����������</param>
        public void RemoveComponent(Type type)
        {
            EnsureIsComponent(type);
            RemoveComponentNotCheck(type);
        }

        /// <summary>
        /// �Ƴ�һ�����������������
        /// </summary>
        /// <param name="type">Ҫ�Ƴ����������</param>
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
        /// �Ƴ�һ�����
        /// </summary>
        /// <typeparam name="TComponent">Ҫ�Ƴ����������</typeparam>
        public void RemoveComponent<TComponent>() where TComponent : Component
        {
            RemoveComponentNotCheck(typeof(TComponent));
        }

        /// <summary>
        /// ��ȡһ�����
        /// </summary>
        /// <param name="type">Ҫ��ȡ���������</param>
        /// <returns></returns>
        public Component GetComponent(Type type)
        {
            EnsureIsComponent(type);
            return GetComponentNotCheck(type);
        }

        /// <summary>
        /// ��ȡһ�����������������
        /// </summary>
        /// <param name="type">Ҫ��ȡ���������</param>
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
        /// ��ȡһ�����
        /// </summary>
        /// <typeparam name="TComponent">�������</typeparam>
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
        /// ��ȡ�������
        /// </summary>
        /// <typeparam name="TComponent">Ҫ��ȡ���������</typeparam>
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
        /// ��ȡ�������
        /// </summary>
        /// <typeparam name="TComponent">Ҫ��ȡ���������</typeparam>
        /// <param name="buffer">������</param>
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
        /// ��ȡ�������
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Component> GetComponents()
        {
            return components;
        }

        /// <summary>
        /// �ж��Ƿ���ĳ�����͵����
        /// </summary>
        /// <typeparam name="TComponent">�������</typeparam>
        /// <returns></returns>
        public bool HasComponent<TComponent>() where TComponent : Component
        {
            var index = components.FindIndex(item => item is TComponent);
            return index >= 0;
        }

        /// <summary>
        /// �����ʵ�屻���ٵ�ʱ��
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