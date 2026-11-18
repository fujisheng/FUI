using NUnit.Framework;

namespace FUI.Rendering.URUI.Editor.Tests
{
    public sealed class UruiValidatorTests
    {
        [Test]
        public void DuplicateClassesAndOrdinaryDataAttributesDoNotDeclareBindings()
        {
            var result = UruiValidator.Validate("<div class='row' data-fui-name='same'></div><div class='row' data-fui-name='same'></div>");
            Assert.IsTrue(result.IsValid, "标准 class/data 属性不应触发旧的隐式绑定协议。");
        }
        [Test]
        public void DuplicateIdsReportAuthoredLocation()
        {
            var result = UruiValidator.Validate("<div id='same'></div>\n<div id='same'></div>");
            Assert.IsFalse(result.IsValid);
            Assert.That(result.Issues, Has.Some.Matches<UruiValidationIssue>(issue => issue.Code == "duplicate-id"));
        }
        [Test]
        public void EmptyMarkupIsRejected()
        {
            Assert.IsFalse(UruiValidator.Validate("  ").IsValid);
        }
    }
}
