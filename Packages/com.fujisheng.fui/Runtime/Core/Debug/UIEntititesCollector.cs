using System.Collections.Generic;

namespace FUI.Debug
{
    /// <summary>
    /// UIʵ���ռ��� �����ռ����м����UIʵ�� ����ʱʹ��
    /// </summary>
    public class UIEntititesCollector
    {
        /// <summary>
        /// ���м����UIʵ��
        /// </summary>
        List<UIEntity> enabledEntities;

        /// <summary>
        /// ���м����UIʵ��
        /// </summary>
        public IReadOnlyList<UIEntity> EnabledEntities => enabledEntities;

        /// <summary>
        /// ����һ��UIʵ���ռ���
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
        /// ����UIʵ�崴��ʱ
        /// </summary>
        /// <param name="entity"></param>
        void OnEntityEnabled(UIEntity entity) => ModifyCollection(enabledEntities, entity, true);

        /// <summary>
        /// ����UIʵ������ʱ
        /// </summary>
        /// <param name="entity"></param>
        void OnEntityDisabled(UIEntity entity) => ModifyCollection(enabledEntities, entity, false);
    }
}