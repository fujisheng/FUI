using UnityEngine;
using UnityEngine.UI;

namespace FUI.Rendering.UGUI.Elements
{
    /// <summary>将 UGUI Shadow 适配为可绑定 Element。</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Shadow))]
    [AddComponentMenu("FUI/Elements/Effects/Shadow")]
    public class ShadowElement : GraphicEffectElement<Shadow>
    {
    }
}
