using NUnit.Framework;

namespace FUI.Rendering.UGUI.Tests
{
    public sealed class UguiPlayModeSmokeTests
    {
        [Test]
        public void RuntimeAssemblyLoads()
        {
            Assert.That(typeof(View).Assembly.GetName().Name, Is.EqualTo("FUI.Rendering.UGUI"));
        }
    }
}
