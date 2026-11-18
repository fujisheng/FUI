using System;
using System.Collections.Generic;
using FUI.Binding;
using FUI.Infrastructure;
using FUI.Infrastructure.Binding;
using FUI.Presentation;
using FUI.Rendering;
using NUnit.Framework;

namespace FUI.Tests
{
    public sealed class ViewContentLifecycleTests
    {
        sealed class Model : ObservableObject
        {
            string text = "initial";
            public string Text { get => text; set { text = value; OnPropertyChanged(nameof(Text)); } }
        }
        sealed class Presenter : Presenter<Model> { }
        sealed class View : IView, IViewContentLifecycle
        {
            public string Name => "content lifecycle";
            public int Layer { get; set; }
            public int Order { get; set; }
            public bool Visible { get; set; } = true;
            public bool Interactable { get; set; } = true;
            public IReadOnlyList<IElement> Elements => Array.Empty<IElement>();
            public bool IsContentAvailable { get; private set; } = true;
            public event Action ContentUnavailable;
            public event Action ContentAvailable;
            public string Text;
            public int Writes;
            public T GetElement<T>(string path) where T : IElement => default;
            public IElement GetElement(string path, Type elementType) => null;
            public void Initialize() { }
            public void Destroy() { }
            public void Suspend() { IsContentAvailable = false; ContentUnavailable?.Invoke(); }
            public void Resume() { IsContentAvailable = true; ContentAvailable?.Invoke(); }
        }
        sealed class Context : BindingContext<Model>
        {
            public bool FailBinding, FailSynchronization;
            public int ActiveBindings;
            public Context(IView view, Model model) : base(view, model) { }
            protected override void OnBinding()
            {
                ActiveBindings++;
                if (FailBinding) throw new InvalidOperationException("expected binding failure");
            }
            protected override void OnUnbinding() => ActiveBindings--;
            protected override void OnPropertyChanged(object sender, string propertyName) => OnSynchronize();
            protected override void OnSynchronize()
            {
                if (FailSynchronization) throw new InvalidOperationException("expected synchronization failure");
                var view = (View)View; view.Text = ViewModel.Text; view.Writes++;
            }
        }
        Context context;
        View view;
        Model model;
        ProjectionHandle<Model> projection;
        [SetUp]
        public void SetUp()
        {
            GeneratedBindingRegistry.Register<Model>((v, m) => context = new Context(v, m), () => new Presenter());
            view = new View(); model = new Model(); projection = Projection.Create(view, model); projection.Activate();
        }
        [TearDown] public void TearDown() => projection.Dispose();

        [Test]
        public void RepeatedRestorationKeepsOneBindingAndSynchronizesCurrentModel()
        {
            for (var i = 0; i < 3; i++)
            {
                view.Suspend(); var writes = view.Writes; model.Text = "suspended " + i;
                Assert.AreEqual(writes, view.Writes); Assert.AreEqual(0, context.ActiveBindings);
                view.Resume(); Assert.AreEqual(model.Text, view.Text); Assert.AreEqual(1, context.ActiveBindings);
                writes = view.Writes; model.Text = "active " + i; Assert.AreEqual(writes + 1, view.Writes);
            }
        }
        [Test]
        public void ClosingWhileSuspendedDoesNotResumeTheBinding()
        {
            view.Suspend(); projection.Dispose(); var writes = view.Writes;
            view.Resume(); model.Text = "closed";
            Assert.AreEqual(writes, view.Writes); Assert.AreEqual(0, context.ActiveBindings);
        }
        [TestCase(true)]
        [TestCase(false)]
        public void FailedRestoreReleasesListenersAndRetainsRetryIntent(bool failBinding)
        {
            view.Suspend(); context.FailBinding = failBinding; context.FailSynchronization = !failBinding;
            Assert.Throws<InvalidOperationException>(() => view.Resume());
            Assert.AreEqual(0, context.ActiveBindings);
            var writes = view.Writes; model.Text = "pending retry"; Assert.AreEqual(writes, view.Writes);
            context.FailBinding = context.FailSynchronization = false;
            view.Suspend(); view.Resume();
            Assert.AreEqual(1, context.ActiveBindings); Assert.AreEqual(model.Text, view.Text);
        }
    }
}
