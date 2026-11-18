using System;
using System.Collections.Generic;
using System.Linq;
using FUI.Binding;
using FUI.Editor;
using FUI.Infrastructure.Binding;
using FUI.Rendering;
using NUnit.Framework;

namespace FUI.Tests
{
    public sealed class BindingInspectionTests
    {
        interface IInheritedContent : ITextContentElement { }
        sealed class NamedElement : IElement
        {
            public string Name => "target";
            public IElement Parent => null;
        }
        sealed class ContentElement : IInheritedContent
        {
            public string Name => "target";
            public IElement Parent => null;
            public BindableProperty<object> Content { get; set; } = new BindableProperty<object>();
        }
        sealed class ProbeView : IView
        {
            public string Name => "probe";
            public int Layer { get; set; }
            public int Order { get; set; }
            public bool Visible { get; set; }
            public bool Interactable { get; set; }
            public IReadOnlyList<IElement> Elements { get; }
            public ProbeView(params IElement[] elements) { Elements = elements; }
            public T GetElement<T>(string path) where T : IElement => (T)GetElement(path, typeof(T));
            public IElement GetElement(string path, Type elementType) => Elements.FirstOrDefault(e => e.Name == path && elementType.IsInstanceOfType(e));
            public void Initialize() => Assert.Fail("inspection must not initialize views");
            public void Destroy() => Assert.Fail("inspection must not destroy views");
        }
        static BindingManifest Manifest(string path = "target", string type = null, string member = "Content") => new BindingManifest
        {
            viewName = "probe", viewModelType = "ProbeModel",
            properties = new List<PropertyBindingDescriptor>
            {
                new PropertyBindingDescriptor
                {
                    propertyInfo = new SourcePropertyDescriptor { name = "Value" },
                    targetInfo = new ElementTargetDescriptor { path = path, type = type ?? typeof(IInheritedContent).FullName, propertyName = member },
                }
            }
        };

        sealed class BaseProbeModel { }
        sealed class ChildProbeModel { }
        [ViewModel(typeof(BaseProbeModel)), View("base")]
        class BaseProbeContext { }
        [ViewModel(typeof(ChildProbeModel)), View("child")]
        sealed class ChildProbeContext : BaseProbeContext { }

        [Test]
        public void EmptyDerivedManifestStillAuditsBaseBindingTargets()
        {
            var child = new BindingManifest { viewModelType = typeof(ChildProbeModel).FullName };
            var parent = Manifest(path: "absent");
            var report = BindingInspection.InspectContext(new ProbeView(), typeof(ChildProbeContext),
                (type, contract) => type == typeof(ChildProbeModel) ? child : parent);
            Assert.IsFalse(report.IsValid);
            Assert.AreEqual(1, report.TargetCount);
            Assert.AreEqual("missing-node", report.Issues.Single().Code);
        }

        [Test]
        public void MissingBaseManifestIsUnknownEvenWhenDerivedHasNoTargets()
        {
            var child = new BindingManifest { viewModelType = typeof(ChildProbeModel).FullName };
            var report = BindingInspection.InspectContext(new ProbeView(), typeof(ChildProbeContext),
                (type, contract) => type == typeof(ChildProbeModel) ? child : null);
            Assert.IsFalse(report.IsValid);
            Assert.AreEqual("manifest-missing", report.Issues.Single().Code);
        }

        [Test]
        public void InspectionUsesTypedBackendResolutionAndInheritedInterfaceMember()
        {
            var report = BindingInspection.Inspect(new ProbeView(new NamedElement(), new ContentElement()), Manifest());
            Assert.IsTrue(report.IsValid);
            Assert.AreEqual(1, report.TargetCount);
        }
        [Test]
        public void MissingPathAndMissingCapabilityAreDistinguished()
        {
            var view = new ProbeView(new NamedElement());
            Assert.AreEqual("missing-node", BindingInspection.Inspect(view, Manifest("absent")).Issues.Single().Code);
            Assert.AreEqual("missing-capability", BindingInspection.Inspect(view, Manifest()).Issues.Single().Code);
        }
        [Test]
        public void MissingMemberAndNullMemberAreReportedWithoutReadingValuePayload()
        {
            Assert.AreEqual("missing-member", BindingInspection.Inspect(new ProbeView(new ContentElement()), Manifest(member: "Absent")).Issues.Single().Code);
            Assert.AreEqual("null-member", BindingInspection.Inspect(new ProbeView(new ContentElement { Content = null }), Manifest()).Issues.Single().Code);
        }
        [Test]
        public void MissingManifestAndUnresolvedTypeAreUnknownInsteadOfPassing()
        {
            var view = new ProbeView(new ContentElement());
            var missing = BindingInspection.Inspect(view, null);
            Assert.IsFalse(missing.IsValid);
            Assert.AreEqual("manifest-missing", missing.Issues.Single().Code);
            var unknown = BindingInspection.Inspect(view, Manifest(type: "Missing.Assembly.Capability"));
            Assert.IsFalse(unknown.IsValid);
            Assert.AreEqual("type-unresolved", unknown.Issues.Single().Code);
        }
        [Test]
        public void CommandTargetsAreInspectedTooWithoutInvokingThem()
        {
            var manifest = Manifest();
            manifest.properties.Clear();
            manifest.commands.Add(new CommandBindingDescriptor
            {
                commandInfo = new CommandDescriptor { name = "Click" },
                targetInfo = new CommandTargetDescriptor { path = "absent", type = typeof(IClickElement).FullName, propertyName = "Click" },
            });
            var report = BindingInspection.Inspect(new ProbeView(), manifest);
            Assert.AreEqual(1, report.TargetCount);
            Assert.AreEqual("missing-node", report.Issues.Single().Code);
        }
    }
}
