using System.Collections;
using FUI.Rendering;
using FUI.Rendering.UGUI.Elements;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

namespace FUI.Rendering.UGUI.Tests
{
    public sealed class DragCancellationTests
    {
        GameObject root;
        [UnityTearDown] public IEnumerator TearDown() { if (root) { Object.Destroy(root); } yield return null; }

        public sealed class ScriptedInput:BaseInput
        {
            public Vector2 position;
            public bool down,held,up;
            public override bool mousePresent=>true;
            public override Vector2 mousePosition=>position;
            public override Vector2 mouseScrollDelta=>Vector2.zero;
            public override bool touchSupported=>false;
            public override int touchCount=>0;
            public override bool GetMouseButtonDown(int button)=>button==0&&down;
            public override bool GetMouseButton(int button)=>button==0&&held;
            public override bool GetMouseButtonUp(int button)=>button==0&&up;
            public override float GetAxisRaw(string axisName)=>0;
            public override bool GetButtonDown(string buttonName)=>false;
        }
        [UnityTest]
        public IEnumerator RealEventSystemContactMovesBeforeDragThresholdWithoutDuplicateDragMovement()
        {
            root=new GameObject("Contact EventSystem View",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster),typeof(View));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var child=new GameObject("contact",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(InteractionElement));child.transform.SetParent(root.transform,false);
            var rect=(RectTransform)child.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(200,200);
            var eventHost=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule),typeof(ScriptedInput));eventHost.transform.SetParent(root.transform,false);
            var system=eventHost.GetComponent<EventSystem>();system.pixelDragThreshold=20;
            var module=eventHost.GetComponent<StandaloneInputModule>();var input=eventHost.GetComponent<ScriptedInput>();module.inputOverride=input;
            input.position=new Vector2(Screen.width*.5f,Screen.height*.5f);
            ((IView)root.GetComponent<View>()).Initialize();
            var adapter=child.GetComponent<InteractionElement>();var contact=adapter as IPointerContactElement;Assert.IsNotNull(contact);
            var pressed=0;var moved=0;var dragging=0;var released=0;
            contact.PointerPressed.AddListener(_=>pressed++);contact.PointerMoved.AddListener(_=>moved++);contact.PointerReleased.AddListener(_=>released++);adapter.DragStarted.AddListener(_=>dragging++);
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            module.ActivateModule();module.Process();
            input.down=input.held=true;module.Process();input.down=false;
            Assert.AreEqual(1,pressed);
            input.position+=new Vector2(3,0);module.Process();
            Assert.AreEqual(0,dragging,"小幅移动不改变原drag阈值。");Assert.AreEqual(1,moved,"接触移动必须独立于drag阈值。");
            input.position+=new Vector2(40,0);module.Process();
            Assert.AreEqual(1,dragging);Assert.AreEqual(2,moved,"同一移动的pointer和drag回调不能重复发布。");
            input.up=true;input.held=false;module.Process();input.up=false;Assert.AreEqual(1,released);
        }

        [UnityTest]
        public IEnumerator RealEventSystemContactContinuesOutsideHoverBeforeDragThreshold()
        {
            root=new GameObject("Contact EventSystem View",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.GraphicRaycaster),typeof(View));
            root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var child=new GameObject("contact",typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(InteractionElement));child.transform.SetParent(root.transform,false);
            var rect=(RectTransform)child.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(200,200);
            var eventHost=new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule),typeof(ScriptedInput));eventHost.transform.SetParent(root.transform,false);
            var system=eventHost.GetComponent<EventSystem>();system.pixelDragThreshold=20;
            var module=eventHost.GetComponent<StandaloneInputModule>();var input=eventHost.GetComponent<ScriptedInput>();module.inputOverride=input;
            input.position=new Vector2(Screen.width*.5f+99,Screen.height*.5f);
            ((IView)root.GetComponent<View>()).Initialize();
            var adapter=child.GetComponent<InteractionElement>();var contact=adapter as IPointerContactElement;Assert.IsNotNull(contact);
            var pressed=0;var moved=0;var dragging=0;var released=0;
            contact.PointerPressed.AddListener(_=>pressed++);contact.PointerMoved.AddListener(_=>moved++);contact.PointerReleased.AddListener(_=>released++);adapter.DragStarted.AddListener(_=>dragging++);
            yield return null;yield return null;Canvas.ForceUpdateCanvases();
            module.ActivateModule();module.Process();
            input.down=input.held=true;module.Process();input.down=false;
            Assert.AreEqual(1,pressed);
            input.position+=new Vector2(2,0);module.Process();
            yield return null;
            input.position+=new Vector2(3,0);module.Process();
            yield return null;
            Assert.AreEqual(0,dragging,"移出后仍未达到20px拖拽阈值。");
            Assert.AreEqual(2,moved,"移出后每次物理移动仍要发布contact。");
            input.up=true;input.held=false;module.Process();input.up=false;Assert.AreEqual(1,released);
        }

        [UnityTest]
        public IEnumerator ContactUsesParentPlaneAndCancelCannotRelease()
        {
            root = new GameObject("Contact View", typeof(RectTransform), typeof(Canvas), typeof(View));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var child = new GameObject("contact", typeof(RectTransform), typeof(InteractionElement));
            child.transform.SetParent(root.transform, false);
            ((IView)root.GetComponent<View>()).Initialize();
            Canvas.ForceUpdateCanvases();
            var component = child.GetComponent<InteractionElement>();
            var contact = component as IPointerContactElement;
            Assert.IsNotNull(contact, "UGUI must implement the same contact contract.");
            var pressed = 0; var moved = 0; var cancelled = 0; var released = 0;
            PointerContact value = default;
            contact.PointerPressed.AddListener(v => { pressed++; value = v; });
            contact.PointerMoved.AddListener(v => moved++);
            contact.PointerCancelled.AddListener(v => cancelled++);
            contact.PointerReleased.AddListener(v => released++);
            var data = new PointerEventData(null) { pointerId = 8, position = new Vector2(100, 70) };
            ((IPointerDownHandler)component).OnPointerDown(data);
            Assert.AreEqual(1, pressed);
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)root.transform, data.position, null, out var local);
            Assert.AreEqual(local.x, value.local.x, .001f);
            Assert.AreEqual(local.y, value.local.y, .001f);
            Assert.AreEqual(100, value.screen.x);
            ((IPointerDownHandler)component).OnPointerDown(new PointerEventData(null) { pointerId = 9 });
            Assert.AreEqual(1, pressed);
            data.position+=new Vector2(5,5);
            ((IDragHandler)component).OnDrag(data);
            Assert.AreEqual(1, moved);
            child.SetActive(false);
            Assert.AreEqual(1, cancelled);
            ((IPointerUpHandler)component).OnPointerUp(data);
            Assert.AreEqual(0, released);
            child.SetActive(true);
            ((IPointerDownHandler)component).OnPointerDown(data);
            ((IPointerUpHandler)component).OnPointerUp(data);
            Assert.AreEqual(1, released);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableCancelsOnceAndLaterUnityEndCannotCommit()
        {
            root = new GameObject("Drag Cancellation View", typeof(RectTransform), typeof(View));
            var child = new GameObject("drag", typeof(RectTransform), typeof(InteractionElement));
            child.transform.SetParent(root.transform, false);
            var view = (IView)root.GetComponent<View>();
            view.Initialize();
            var cancel = view.GetElement<IPointerDragCancellationElement>("drag");
            Assert.IsNotNull(cancel);
            var drag = view.GetElement<IPointerDragElement>("drag");
            var cancelled = 0; var ended = 0;
            cancel.DragCancelled.AddListener(value => cancelled++);
            drag.DragEnded.AddListener(value => ended++);
            var component = child.GetComponent<InteractionElement>();
            var data = new PointerEventData(null) { position = new Vector2(40, 50) };
            ((IBeginDragHandler)component).OnBeginDrag(data);
            child.SetActive(false);
            Assert.AreEqual(1, cancelled);
            ((IEndDragHandler)component).OnEndDrag(data);
            Assert.AreEqual(0, ended);
            child.SetActive(true);
            ((IBeginDragHandler)component).OnBeginDrag(data);
            ((ICancelHandler)component).OnCancel(new BaseEventData(null));
            Assert.AreEqual(2, cancelled);
            Assert.AreEqual(0, ended);
            yield return null;
        }
    }
}
