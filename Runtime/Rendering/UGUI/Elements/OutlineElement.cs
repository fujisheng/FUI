using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI Outline 适配为可绑定 Element。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Outline))]
    [AddComponentMenu("FUI/Elements/Effects/Outline")]
    public class OutlineElement : GraphicEffectElement<Outline>
    {
    }
}
