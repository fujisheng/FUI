using FUI.Debug;

using System.Collections.Generic;
using System.Linq;

namespace FUI.Editor
{
    /// <summary>
    /// 存活的UI实体信息
    /// </summary>
    public class UIEntitites
    {
        static UIEntitites instance;
        readonly UIEntititesCollector collector;

        public static UIEntitites Instance
        {
            get
            {
                if(instance == null)
                {
                    instance = new UIEntitites();
                }
                return instance;
            }
        }

        UIEntitites()
        {
            collector = new UIEntititesCollector();
        }

        /// <summary>
        /// 所有激活的UI实体
        /// </summary>
        public IReadOnlyList<UIEntity> Entities => collector.EnabledEntities;

        /// <summary>
        /// 根据一个视图获取对应的UI实体
        /// </summary>
        /// <param name="view">视图</param>
        /// <returns></returns>
        public UIEntity GetEntity(IView view)
        {
            if(collector == null || Entities == null)
            {
                return null;
            }

            return Entities.FirstOrDefault(item => item.OwnsView(view));
        }
    }
}