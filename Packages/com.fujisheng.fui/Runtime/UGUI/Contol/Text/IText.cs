using UnityEngine;

namespace FUI.UGUI.Control
{
    public interface IText
    {
        /// <summary>
        /// 文本内容
        /// </summary>
        string Text { get; set; }

        /// <summary>
        /// 字体大小
        /// </summary>
        int FontSize { get; set; }

        /// <summary>
        /// 文本颜色
        /// </summary>
        Color Color { get; set; }
    }
}