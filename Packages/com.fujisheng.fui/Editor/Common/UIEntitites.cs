using FUI.Debug;

using System.Collections.Generic;
using System.Linq;

namespace FUI.Editor
{
    /// <summary>
    /// ����UIʵ����Ϣ
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
        /// ���м����UIʵ��
        /// </summary>
        public IReadOnlyList<UIEntity> Entities => collector.EnabledEntities;

        /// <summary>
        /// ����һ����ͼ��ȡ��Ӧ��UIʵ��
        /// </summary>
        /// <param name="view">��ͼ</param>
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