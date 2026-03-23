using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Feature
{
    public class Models
    {
        static Models instance;
        public static Models Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new Models();
                }
                return instance;
            }
        }

        Dictionary<Type, Group> groups;

        /// <summary>
        /// 初始化所有Model
        /// </summary>
        public void Initialize()
        {
            groups = new Dictionary<Type, Group>();

            //查找所有定义的Model组类型
            var groupTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract
                    && !t.IsInterface
                    && typeof(GroupAttribute).IsAssignableFrom(t))
                .Distinct();

            //查找所有的Model类型
            var modelTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract
                    && !t.IsInterface
                    && typeof(IModel).IsAssignableFrom(t));

            //初始化公共Model组
            var publicGroup = AddGroup(typeof(PublicGroup));
            var publicGroupModelTypes = modelTypes.Where(t =>
            {
                var groupAttribute = t.GetCustomAttribute<GroupAttribute>();
                return groupAttribute == null || groupAttribute.GetType() == typeof(PublicGroup);
            });
            
            publicGroup.Initialize(publicGroupModelTypes);

            //初始化其它Model组
            foreach (var groupType in groupTypes)
            { 
                if(groupType == typeof(PublicGroup))
                {
                    continue;
                }

                var group = AddGroup(groupType);
                var groupModelTypes = modelTypes.Where(t =>
                {
                    var groupAttribute = t.GetCustomAttribute<GroupAttribute>();
                    return groupAttribute != null && groupAttribute.GetType() == groupType;
                });
                group.Initialize(groupModelTypes);
            }
        }

        /// <summary>
        /// 添加一个Model组
        /// </summary>
        /// <param name="groupType">组类型</param>
        /// <returns></returns>
        Group AddGroup(Type groupType)
        {
            if(!groups.TryGetValue(groupType, out var group))
            {
                group = new Group(groupType);
                groups.Add(groupType, group);
            }

            return group;
        }

        /// <summary>
        /// 获取一个Model组
        /// </summary>
        /// <typeparam name="T">组类型</typeparam>
        /// <returns></returns>
        public Group GetGroup<T>() where T : GroupAttribute
        {
            if(!groups.TryGetValue(typeof(T), out var group))
            {
                return null;
            }

            return group;
        }

        
        /// <summary>
        /// 释放一个Model组
        /// </summary>
        /// <typeparam name="T">要释放的组类型</typeparam>
        /// <param name="withAutoConstructOnInitialization">是否包含标记为自动初始化的Model</param>
        public void Release<T>(bool withAutoConstructOnInitialization = false) where T : GroupAttribute
        {
            if(!groups.TryGetValue(typeof(T), out var group))
            {
                return;
            }

            group.Release(!withAutoConstructOnInitialization);
        }

        /// <summary>
        /// 释放所有Model组
        /// </summary>
        /// <param name="withAutoConstructOnInitialization">是否包含标记为自动初始化的Model</param>
        public void ReleaseAll(bool withAutoConstructOnInitialization = false)
        {
            foreach(var group in groups.Values)
            {
                group.Release(withAutoConstructOnInitialization);
            }
        }
    }
}
