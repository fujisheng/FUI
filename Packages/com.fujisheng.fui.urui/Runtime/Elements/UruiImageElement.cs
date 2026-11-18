using FUI.Binding;
using FUI.Rendering;
using UnityEngine;
using System.Linq;

namespace FUI.Rendering.URUI
{
    /// <summary>图片适配：SpriteSource 映射为 HTML img 的 src。</summary>
    public class UruiImageElement : UruiElement, IImageElement
    {
        readonly global::URUI.Controls.Image image;
        bool initialized;
        bool readingAuthor;
        bool restoringSource;
        int authoredOverrides;

        public UruiImageElement(global::URUI.Elements.Element element, string name) : this(element, name, ResolveImage(element)) { }

        /// <summary>显式图片表面；容器保留布局、可见性和空间属性，图片表面只接收图片属性。</summary>
        public UruiImageElement(global::URUI.Elements.Element element, string name, global::URUI.Controls.Image image) : base(ValidateSurface(element,image), name)
        {
            this.image = image;
            // UGUI keeps the authored Sprite outside its initially empty dynamic source slot.
            SpriteSource = new BindableProperty<string>(null, (previous, value) =>
            {
                if (restoringSource) { return; }
                try
                {
                    var source = value;
                    if (!string.IsNullOrEmpty(source) && !image.Document.TryLoadImage(source, out _))
                    {
                        RestoreSource(previous);
                        FUI.Diagnostics.Log.Instance.LogError($"Element '{Name}' failed to load Sprite from source '{value}'.");
                        return;
                    }
                    if (string.IsNullOrEmpty(source)) image.RemoveAttribute("src");
                    else image.SetAttribute("src", source);
                }
                catch (System.Exception exception)
                {
                    RestoreSource(previous);
                    FUI.Diagnostics.Log.Instance.LogError($"Element '{Name}' failed to load Sprite from source '{value}': {exception.Message}");
                    FUI.Diagnostics.Log.Instance.LogException(exception);
                }
            });
        }

        void EnsureImageProperties()
        {
            if (Element == null) { throw new System.ObjectDisposedException(nameof(UruiImageElement)); }
            if (initialized) { return; }
            // 仅实际使用图片属性时才读取级联；构造普通图片适配器不能冲刷整个更新批次。
            var hasLayout = image.HasLayout;
            // 几何屏障会同步派发用户回调，回调可能已初始化或释放此适配器。
            if (Element == null) { throw new System.ObjectDisposedException(nameof(UruiImageElement)); }
            if (initialized) { return; }
            preserveAspect = Property(image.Fit == global::URUI.Controls.ImageFit.Contain,1,
                value => image.Fit = value ? global::URUI.Controls.ImageFit.Contain : global::URUI.Controls.ImageFit.Fill);
            fillCenter = Property(image.FillCenter,2,value => image.FillCenter=value);
            fillOrigin = Property(image.FillOrigin,4,value => image.FillOrigin=value, value =>
                value>=0 && value<=3 ? value : throw new System.ArgumentOutOfRangeException(nameof(value)));
            fillClockwise = Property(image.FillClockwise,8,value => image.FillClockwise=value);
            fillAmount = Property(image.FillAmount,16,value => image.FillAmount=value, value =>
                float.IsFinite(value) ? Mathf.Clamp01(value) : throw new System.ArgumentOutOfRangeException(nameof(value)));
            initialized=true;
            if (!hasLayout) { image.Document.LayoutUpdated+=ReadAuthoredProperties; }
        }

        void RestoreSource(string previous)
        {
            restoringSource = true;
            try { SpriteSource.Value = previous; } finally { restoringSource = false; }
        }

        BindableProperty<T> Property<T>(T initial,int bit,System.Action<T> apply,System.Func<T,T> normalize=null)
            => new(initial,null,value =>
            {
                var result=normalize!=null?normalize(value):value;
                if(initialized && !readingAuthor) { authoredOverrides|=bit; apply(result); }
                return result;
            });

        void ReadAuthoredProperties(global::URUI.Documents.Document document)
        {
            if(!image.IsValid || !image.HasLayout) { return; }
            document.LayoutUpdated-=ReadAuthoredProperties;
            // Before the first cascade, no default may overwrite authored CSS.
            // Explicit VM writes take precedence even when equal to the default.
            readingAuthor=true;
            try
            {
                if((authoredOverrides&1)==0) { PreserveAspect.Value=image.Fit==global::URUI.Controls.ImageFit.Contain; }
                if((authoredOverrides&2)==0) { FillCenter.Value=image.FillCenter; }
                if((authoredOverrides&4)==0) { FillOrigin.Value=image.FillOrigin; }
                if((authoredOverrides&8)==0) { FillClockwise.Value=image.FillClockwise; }
                if((authoredOverrides&16)==0) { FillAmount.Value=image.FillAmount; }
            }
            finally { readingAuthor=false; }
        }

        public BindableProperty<string> SpriteSource { get; }
        BindableProperty<bool> preserveAspect, fillCenter, fillClockwise;
        BindableProperty<int> fillOrigin;
        BindableProperty<float> fillAmount;
        public BindableProperty<bool> PreserveAspect { get { EnsureImageProperties(); return preserveAspect; } }
        public BindableProperty<bool> FillCenter { get { EnsureImageProperties(); return fillCenter; } }
        public BindableProperty<int> FillOrigin { get { EnsureImageProperties(); return fillOrigin; } }
        public BindableProperty<bool> FillClockwise { get { EnsureImageProperties(); return fillClockwise; } }
        public BindableProperty<float> FillAmount { get { EnsureImageProperties(); return fillAmount; } }

        static global::URUI.Elements.Element ValidateSurface(global::URUI.Elements.Element host, global::URUI.Controls.Image surface)
        {
            if(host==null){throw new System.ArgumentNullException(nameof(host));}
            if(surface==null){throw new System.ArgumentNullException(nameof(surface));}
            var owner=(global::URUI.Elements.Element)surface;
            while(owner!=null && !ReferenceEquals(owner,host)){owner=owner.Parent;}
            if(owner==null){throw new System.ArgumentException("图片表面必须属于适配器容器。",nameof(surface));}
            return host;
        }

        static global::URUI.Controls.Image ResolveImage(global::URUI.Elements.Element element)
        {
            if (element is global::URUI.Controls.Image direct) { return direct; }
            throw new System.ArgumentException("Image adapters require an img or a declared direct image surface.", nameof(element));
        }

        protected override void ApplyTint(Rgba value)
        {
            image.Style.SetColor("--urui-image-color", new Color(value.r, value.g, value.b, value.a));
        }

        public override void Dispose()
        {
            image.Document.LayoutUpdated-=ReadAuthoredProperties;
            SpriteSource?.Dispose();
            preserveAspect?.Dispose();
            fillCenter?.Dispose();
            fillOrigin?.Dispose();
            fillClockwise?.Dispose();
            fillAmount?.Dispose();
            base.Dispose();
        }
    }
}
