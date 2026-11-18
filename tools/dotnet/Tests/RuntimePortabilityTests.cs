using System.Linq;
using FUI.Binding;
using FUI.Navigation;
using NUnit.Framework;

namespace FUI.Portable.Tests
{
    public sealed class RuntimePortabilityTests
    {
        [Test]
        public void RuntimeAssembliesHaveNoUnityReferences()
        {
            foreach (var assembly in new[] { typeof(ObservableObject).Assembly, typeof(Navigator).Assembly })
            {
                Assert.That(assembly.GetReferencedAssemblies().Select(reference => reference.Name),
                    Has.None.StartsWith("Unity"), assembly.FullName);
            }
        }
    }
}
