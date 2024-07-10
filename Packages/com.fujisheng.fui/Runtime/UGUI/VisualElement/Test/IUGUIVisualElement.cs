using UnityEngine;
using UnityEngine.UI;

namespace FUI.UGUI
{
    public interface IUGUIVisualElement
    {
        void Initialize();
        void Destroy();
    }

    public interface IAssetLoadableElement
    {
        void SetAssetLoader(IAssetLoader assetLoader);
    }

    public interface ITextElement : IVisualElement<string>
    {
    }

    public interface IColorElement : IVisualElement<Color>
    {
    }

    public class TextElement : MonoBehaviour, IUGUIVisualElement, ITextElement, IAssetLoadableElement, IColorElement
    {
        protected IAssetLoader assetLoader;
        protected Text text;

        public void Initialize()
        {
            text = GetComponent<Text>();
        }

        public void Destroy()
        {
            Destroy(text);
        }

        public void SetAssetLoader(IAssetLoader assetLoader)
        {
            this.assetLoader = assetLoader;
        }

        void ITextElement.UpdateValue(string value)
        {
            throw new System.NotSupportedException();
        }

        void IColorElement.UpdateValue(Color value)
        {
            throw new System.NotImplementedException();
        }
    }
}