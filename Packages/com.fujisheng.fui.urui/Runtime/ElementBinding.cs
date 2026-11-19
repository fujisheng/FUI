using System;
using System.Collections.Generic;
using global::URUI.Elements;
namespace FUI.Rendering.URUI
{
    /// <summary>
    /// 业务绑定名到作用域内标准选择器和具体适配器能力的显式映射。
    /// </summary>
    public sealed class ElementBinding
    {
        public string Name
        {
            get;
        }
        public string Selector
        {
            get;
        }
        public Type AdapterType
        {
            get;
        }
        public Func<Element, string, IElement> Create
        {
            get;
        }
        ElementBinding(string name, string selector, Type adapterType, Func<Element, string, IElement> create)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("绑定名不能为空。", nameof(name));
            }
            if (string.IsNullOrEmpty(selector) || selector.Length < 2 || (selector[0] != '.' && selector[0] != '#'))
            {
                throw new ArgumentException("绑定仅接受简单 #id 或 .class 选择器。", nameof(selector));
            }
            for (var i = 1; i < selector.Length; i++)
            {
                var c = selector[i];
                if (!(char.IsLetterOrDigit(c) || c == '-' || c == '_'))
                {
                    throw new ArgumentException("绑定仅接受简单 #id 或 .class 选择器。", nameof(selector));
                }
            }
            if (adapterType.IsInterface || adapterType.IsAbstract)
            {
                throw new ArgumentException("必须声明具体适配器类型。", nameof(adapterType));
            }
            Name = name;
            Selector = selector;
            AdapterType = adapterType;
            Create = create ?? throw new ArgumentNullException(nameof(create));
        }
        public static ElementBinding For<TAdapter>(string name, string selector, Func<Element, string, TAdapter> factory) where TAdapter : class, IElement
        {
            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }
            return new ElementBinding(name, selector, typeof(TAdapter), (element, key) => factory(element, key));
        }
        public IReadOnlyList<Element> Resolve(Element root) => Selector[0] == '#'
            ? root.Query(Selector.Substring(1)) : root.Query(className: Selector.Substring(1));
    }
}
