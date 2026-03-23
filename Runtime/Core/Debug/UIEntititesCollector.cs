using System.Collections.Generic;

namespace FUI.Debug
{
    /// <summary>
    /// UI实体收集器 用于收集所有激活的UI实体 调试时使用
    /// </summary>
    public class UIEntititesCollector
    {
        /// <summary>
        /// 所有激活的UI实体
        /// </summary>
        List<UIEntity> enabledEntities;

        /// <summary>
        /// 所有激活的UI实体
        /// </summary>
        public IReadOnlyList<UIEntity> EnabledEntities => enabledEntities;

        /// <summary>
        /// 构造一个UI实体收集器
        /// </summary>
        public UIEntititesCollector()
        {
            enabledEntities = new List<UIEntity>();
            UIEntity.OnEntityEnabled += OnEntityEnabled;
            UIEntity.OnEntityDisabled += OnEntityDisabled;
        }

        ~UIEntititesCollector()
        {
            UIEntity.OnEntityEnabled -= OnEntityEnabled;
            UIEntity.OnEntityDisabled -= OnEntityDisabled;
            enabledEntities.Clear();
            enabledEntities = null;
        }

        static void ModifyCollection(List<UIEntity> collection, UIEntity entity, bool add)
        {
            if (entity == null)
            {
                return;
            }

            if (add && !collection.Contains(entity))
            {
                collection.Add(entity);
            }
            else if (!add && collection.Contains(entity))
            {
                collection.Remove(entity);
            }
        }

        /// <summary>
        /// 当有UI实体创建时
        /// </summary>
        /// <param name="entity"></param>
        void OnEntityEnabled(UIEntity entity) => ModifyCollection(enabledEntities, entity, true);

        /// <summary>
        /// 当有UI实体销毁时
        /// </summary>
        /// <param name="entity"></param>
        void OnEntityDisabled(UIEntity entity) => ModifyCollection(enabledEntities, entity, false);
    }
}