using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using URUI.Resources;

namespace FUI.Rendering.URUI.Tests
{
    public sealed class UruiImageSemanticsTests
    {
        GameObject root, cameraObject;
        Camera camera;
        RenderTexture output;
        Texture2D texture, pixel;
        Document document;
        UruiImageElement adapter;
        int resolverCalls,resolverReleases;
        IEnumerator Setup(int width,int height,bool authored=false,bool overrideAmount=false,bool resolving=false)
        {
            root=new GameObject("Image semantics"); cameraObject=new GameObject("Main Camera",typeof(Camera)); cameraObject.tag="MainCamera";
            camera=cameraObject.GetComponent<Camera>(); camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=Color.black;
            output=new RenderTexture(Screen.width,Screen.height,0); output.Create(); camera.targetTexture=output;
            texture=new Texture2D(width,height,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
            var colors=new Color[width*height]; System.Array.Fill(colors,Color.white); texture.SetPixels(colors);texture.Apply();
            pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
            yield return null;
            document=FuiHtmlFixtureAssets.Create(root);
            resolverCalls=resolverReleases=0;
            if(resolving){document.ImageResolver=source=>{resolverCalls++;return source=="URUI/Tests/resolved.png"?new ImageLease(texture,()=>resolverReleases++):null;};}
            document.Images=new[]{new ImageAsset("test-image.png",texture)};
            document.TextResources=new[]{new TextResource("URUI/Tests/ImageSemantics.styles.css",global::UnityEngine.Resources.Load<TextAsset>("URUI/Tests/ImageSemantics.styles"))};
            FuiHtmlFixtureAssets.Load(document, "URUI/Tests/ImageSemantics");
            if(authored){document.Q("image").AddToClassList("authored-values");}
            adapter=new UruiImageElement(document.Q("image"),"image");
            if(overrideAmount){adapter.FillAmount.Value=1;}
            yield return null;yield return null;
        }
        Color Sample(int x,int y)
        {
            var previous=RenderTexture.active;
            try{RenderTexture.active=output;pixel.ReadPixels(new Rect(x,output.height-y-1,1,1),0,0);pixel.Apply();return pixel.GetPixel(0,0);}
            finally{RenderTexture.active=previous;}
        }
        [TearDown] public void TearDown()
        {
            adapter?.Dispose();adapter=null;
            if(root){Object.DestroyImmediate(root);} if(camera){camera.targetTexture=null;} if(cameraObject){Object.DestroyImmediate(cameraObject);}
            if(output){output.Release();Object.DestroyImmediate(output);} if(texture){Object.DestroyImmediate(texture);} if(pixel){Object.DestroyImmediate(pixel);}
        }
        [UnityTest] public IEnumerator PreserveAspectChangesDrawingWithoutChangingAuthoredBounds()
        {
            yield return Setup(2,1);
            adapter.PreserveAspect.Value=true;
            yield return null;yield return null;camera.Render();
            Assert.AreEqual(global::URUI.Controls.ImageFit.Contain,((global::URUI.Controls.Image)document.Q("image")).Fit);
            Assert.Less(Sample(70,25).r,.1f,"保留比例时上方应留空，不能只是保存属性。");
            Assert.Greater(Sample(70,70).r,.9f);
            Assert.That(document.Q("image").WorldBound.size,Is.EqualTo(new Vector2(100,100)));
            adapter.PreserveAspect.Value=false;
            yield return null;yield return null;camera.Render();Assert.Greater(Sample(70,25).r,.9f);
        }
        [UnityTest] public IEnumerator InitialNullSourcePreservesAuthoredImageAndLaterClearRemovesAnExplicitSource()
        {
            yield return Setup(2,1);
            adapter.SpriteSource.Value=null;
            Assert.AreEqual("test-image.png",document.Q("image").GetAttribute("src"),"与 UGUI 一致，初次空绑定保留作者预置图片。");
            adapter.SpriteSource.Value="test-image.png";
            adapter.SpriteSource.Value=null;
            Assert.IsNull(document.Q("image").GetAttribute("src"),"显式动态资源随后清空时必须移除图片。");
        }
        [UnityTest] public IEnumerator PreloadingUsesTheDocumentCacheAndOwnsTheResolvedLeaseUntilUnload()
        {
            yield return Setup(2,1,resolving:true);
            Assert.IsTrue(document.TryLoadImage("resolved.png",out var size));Assert.AreEqual(new Vector2Int(2,1),size);
            Assert.IsTrue(document.TryLoadImage("resolved.png",out _));Assert.AreEqual(1,resolverCalls);
            Assert.AreEqual(0,resolverReleases,"预加载的资源必须继续由文档纹理缓存持有。");
            Assert.IsFalse(document.TryLoadImage("missing.png",out size));Assert.AreEqual(Vector2Int.zero,size);
            Assert.IsFalse(document.TryLoadImage("missing.png",out _));Assert.AreEqual(2,resolverCalls,"失败也只能解析一次。");
            adapter.Dispose();adapter=null;document.Unload();
            Assert.IsFalse(document.IsLoaded);
            for(var frame=0;frame<60&&resolverReleases==0;frame++){yield return null;}
            Assert.AreEqual(1,resolverReleases,"预加载资源应沿文档的退役流程释放一次。");
        }
        [UnityTest] public IEnumerator FailedSourcePreservesTheAuthoredAndLastSuccessfulImage()
        {
            yield return Setup(2,1);
            adapter.SpriteSource.Value="unavailable-image.png";
            Assert.AreEqual("test-image.png",document.Q("image").GetAttribute("src"),"加载失败不能移除作者图片。");
            Assert.IsNull(adapter.SpriteSource.Value,"失败的源必须回滚，不能报告已成功绑定。");
            adapter.SpriteSource.Value="test-image.png";
            adapter.SpriteSource.Value="another-unavailable-image.png";
            Assert.AreEqual("test-image.png",document.Q("image").GetAttribute("src"));
            Assert.AreEqual("test-image.png",adapter.SpriteSource.Value);
        }
        [UnityTest] public IEnumerator SlicesKeepBorderSizeAndFillCenterControlsGeometry()
        {
            yield return Setup(6,6);
            for(var y=0;y<6;y++)for(var x=0;x<6;x++){texture.SetPixel(x,y,x==0||y==0||x==5||y==5?Color.red:Color.green);}texture.Apply();
            document.Q("image").AddToClassList("sliced");
            yield return null;yield return null;camera.Render();
            var inside=Sample(23,70);
            // 编码域双线性采样的边界恰为 60%；8 位读回对应绿色通道 153。
            Assert.GreaterOrEqual(((Color32)inside).g,153,"九宫格边界应保持作者像素尺寸，允许源图边缘双线性采样。");
            Assert.Greater(inside.g,inside.r);
            adapter.FillCenter.Value=false;
            yield return null;yield return null;camera.Render();
            Assert.Less(Sample(70,70).maxColorComponent,.1f,"关闭中心应实际移除中心几何。");
            Assert.Greater(Sample(20,70).r,.9f);
        }
        [UnityTest] public IEnumerator FillAmountOriginAndClockwiseChangeVisibleGeometry()
        {
            yield return Setup(2,2);
            var image=document.Q("image");image.AddToClassList("horizontal");adapter.FillAmount.Value=.25f;
            yield return null;yield return null;camera.Render();
            Assert.Greater(Sample(30,70).r,.9f);Assert.Less(Sample(100,70).r,.1f);
            adapter.FillOrigin.Value=1;
            yield return null;yield return null;camera.Render();
            Assert.Less(Sample(30,70).r,.1f);Assert.Greater(Sample(110,70).r,.9f);
            image.RemoveFromClassList("horizontal");image.AddToClassList("vertical");adapter.FillOrigin.Value=0;
            yield return null;yield return null;camera.Render();
            Assert.Greater(Sample(70,110).r,.9f);Assert.Less(Sample(70,30).r,.1f);
            image.RemoveFromClassList("vertical");image.AddToClassList("radial");
            yield return null;yield return null;camera.Render();
            Assert.Greater(Sample(40,100).r,.9f);Assert.Less(Sample(100,100).r,.1f);
            adapter.FillClockwise.Value=false;
            yield return null;yield return null;camera.Render();
            Assert.Less(Sample(40,100).r,.1f);Assert.Greater(Sample(100,100).r,.9f);
        }
        [UnityTest] public IEnumerator Radial180UsesTheAngularMetricOfEachHalfRectangle()
        {
            yield return Setup(2,2);
            document.Q("image").AddToClassList("radial180");adapter.FillAmount.Value=.25f;
            yield return null;yield return null;camera.Render();
            Assert.Greater(Sample(30,50).r,.9f,"四分之一填充必须覆盖原 UGUI 左半矩形的三角形。");
            Assert.Less(Sample(60,50).r,.1f);
            adapter.FillClockwise.Value=false;
            yield return null;yield return null;camera.Render();
            Assert.Greater(Sample(110,50).r,.9f);Assert.Less(Sample(30,50).r,.1f);
        }
        [UnityTest] public IEnumerator FirstLayoutKeepsAuthoredImageValuesWithoutOverrides()
        {
            yield return Setup(2,1,true);
            var image=(global::URUI.Controls.Image)document.Q("image");
            Assert.AreEqual(global::URUI.Controls.ImageFit.Cover,image.Fit,"读取作者属性不能把 Cover 改写为 Fill。");
            Assert.That(adapter.FillAmount.Value,Is.EqualTo(.3f).Within(.001));
        }
        [UnityTest] public IEnumerator ExplicitDefaultBeforeLayoutOverridesAuthorStyle()
        {
            yield return Setup(2,1,true,true);
            Assert.AreEqual(1,adapter.FillAmount.Value);
            Assert.AreEqual(1,((global::URUI.Controls.Image)document.Q("image")).FillAmount);
        }
    }
}
