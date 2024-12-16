using FUI;
using FUI.BindingDescriptor;
using FUI.UGUI.Control;

namespace Test.Descriptor
{
    public class TestDescriptorContextDescriptor : ContextDescriptor<TestDescriptorViewModel>
    {
        protected override string ViewName => "TestDescriptorView";

        protected override PropertyBindingDescriptor[] Properties => new PropertyBindingDescriptor[]
        {
            BindingProperty(VM.TestInt)
                .ToTarget("txt_int")
                .ToElement(nameof(TextElement.TextObject)),

            BindingProperty(VM.TestString)
                .ToTarget("txt_string")
                .ToElement(nameof(TextElement.Text))
                .WithBindingMode(BindingMode.OneWay),

            BindingProperty(VM.TestFloat)
                .ToTarget("txt_float")
                .ToElement(nameof(TextElement.Text))
                .WithConverter<FloatToStringConverter>(),

            BindingProperty(VM.TestBool)
                .ToTarget("txt_bool")
                .ToElement(nameof(TextElement.Text))
                .WithConverter<BoolToStringConverter>(),
        };

        protected override CommandBindingDescriptor[] Commands => new CommandBindingDescriptor[]
        {
            BindingCommand(VM.OnClick)
                .ToTarget("btn_click")
                .ToCommand(nameof(ButtonElement.OnClick)),
        };
    }
}