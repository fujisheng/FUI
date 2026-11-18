using System.Collections;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using URUI.Documents;
using FUI.Rendering.URUI;
using FUI.Rendering.URUI.Tests;
namespace FUI.URUI.Tests
{
 public sealed class UruiClassTransitionGroupTests
 {
  [UnityTest] public IEnumerator ShortPropertyCannotCompleteTheLongerNamedAnimationGroup()
  {
   var root=new GameObject("Transition group");var camera=new GameObject("Group camera",typeof(Camera));camera.tag="MainCamera";
   ClassTransition transition=null;
   try{
    var document=FuiHtmlFixtureAssets.Create(root);FuiHtmlFixtureAssets.Load(document, "FUI.URUI.Tests/AnimationGroup");document.SynchronizeLayout();
    transition=new ClassTransition(document.DocumentElement,"running",document.Q("box"));var finished=transition.PlayAsync(CancellationToken.None).AsTask();
    yield return new WaitForSecondsRealtime(.08f);Assert.IsFalse(finished.IsCompleted,"一个短属性结束不能提前完成同节点的较长命名动画。");
    yield return new WaitForSecondsRealtime(.3f);Assert.IsTrue(finished.IsCompletedSuccessfully);
   }finally{transition?.Dispose();Object.DestroyImmediate(root);Object.DestroyImmediate(camera);}
  }
 }
}
