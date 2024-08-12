&nbsp;
# FUI

FUI是一个适用于Unity的MVVM UI框架，通过Roslyn生成绑定代码并注入，从而实现了更高效的性能和简洁的使用方式。

# 特性

- 声明式绑定
- 从ViewModel到View0GC
- 可扩展的绑定转换从而支持一份数据对应多个表现
- 支持多对多的绑定
- 绑定方式可扩展，只需输出对应的配置文件即可
- 支持将一个ViewModel作为另一个ViewModel的属性从而实现组件化UI开发

# **最简例子**

```c#
[Binding("TestCodeBindingView")]
public class TestCodeBindingViewModel : ViewModel
{
    [Binding("txt_int", typeof(IntToStringConverter), typeof(TextElement))]
    public int TestInt { get; set; }

    [Binding("txt_string", elementType: typeof(TextElement))]
    public string TestString { get; set; }

    [Binding("txt_float", converterType: typeof(FloatToStringConverter))]
    public float TestFloat { get; set; }

    [Binding("txt_bool")]
    public bool TestBool { get; set; }
}

public class TestCodeBindingViewBehavior : ViewBehavior<TestCodeBindingViewModel>
{
    protected override void OnOpen(object param)
    {
        UnityEngine.Debug.Log("OnOpen  TestCodeBindingView");
        VM.TestInt = 1;
        VM.TestString = "Hello, code binding";
        VM.TestFloat = 3.14f;
        VM.TestBool = true;
    }
}

public class TestCodeBinding : MonoBehaviour
{
    void Start()
    {
        TestLauncher.Instance.UIManager.OpenAsync("TestCodeBindingView");
    }
}
```

# 实现原理

将Unity对某个程序集的编译hook到FUICompiler，从而实现编译过程中解析绑定标签并生成胶水代码后输出DLL。

例如原本的ViewModel为

```c#
[Binding("TestCodeBindingView")]
public class TestCodeBindingViewModel : ViewModel
{
    [Binding("txt_int", typeof(IntToStringConverter), typeof(TextElement))]
    public int TestInt { get; set; }

    [Binding("txt_string", elementType: typeof(TextElement), bindingType:BindingType.OneWay)]
    public string TestString { get; set; }

    [Binding("txt_float", converterType: typeof(FloatToStringConverter))]
    public float TestFloat { get; set; }

    [Binding("txt_bool")]
    public bool TestBool { get; set; }
}
```

在编译的过程中会生成如下几个文件

1、生成对应的绑定上下文

```c#
[FUI.ViewModelAttribute(typeof(TestCodeBindingViewModel))]
[FUI.ViewAttribute("TestCodeBindingView")]
public class __TestCodeBindingViewModel_TestCodeBindingView_Binding_Generated : FUI.BindingContext
{
    IntToStringConverter IntToStringConverter = new IntToStringConverter();
    FloatToStringConverter FloatToStringConverter = new FloatToStringConverter();
    public __TestCodeBindingViewModel_TestCodeBindingView_Binding_Generated(FUI.IView view, FUI.Bindable.ObservableObject viewModel) : base(view, viewModel)
    {
    }

    protected override void Binding()
    {
        if (this.ViewModel is TestCodeBindingViewModel TestCodeBindingViewModel)
        {
            TestCodeBindingViewModel._TestInt_Changed += TestCodeBindingViewModel_TestInt_PropertyChanged;
            TestCodeBindingViewModel._TestString_Changed += TestCodeBindingViewModel_TestString_PropertyChanged;
            TestCodeBindingViewModel._TestFloat_Changed += TestCodeBindingViewModel_TestFloat_PropertyChanged;
            TestCodeBindingViewModel._TestBool_Changed += TestCodeBindingViewModel_TestBool_PropertyChanged;
            return;
        }
    }

    protected override void Unbinding()
    {
        if (this.ViewModel is TestCodeBindingViewModel TestCodeBindingViewModel)
        {
            TestCodeBindingViewModel._TestInt_Changed -= TestCodeBindingViewModel_TestInt_PropertyChanged;
            TestCodeBindingViewModel._TestString_Changed -= TestCodeBindingViewModel_TestString_PropertyChanged;
            TestCodeBindingViewModel._TestFloat_Changed -= TestCodeBindingViewModel_TestFloat_PropertyChanged;
            TestCodeBindingViewModel._TestBool_Changed -= TestCodeBindingViewModel_TestBool_PropertyChanged;
            return;
        }
    }

    void TestCodeBindingViewModel_TestInt_PropertyChanged(object sender, int preValue, int @value)
    {
        var convertedValue = IntToStringConverter.Convert(@value);
        var element = this.View.GetVisualElement<TextElement>("txt_int");
        if (element == null)
        {
            throw new System.Exception($"{this.View.Name} GetVisualElement type:<TextElement> path:{@"txt_int"} failed");
        }

        element.UpdateValue(convertedValue);
    }

    void TestCodeBindingViewModel_TestString_PropertyChanged(object sender, string preValue, string @value)
    {
        var convertedValue = @value;
        var element = this.View.GetVisualElement<TextElement>("txt_string");
        if (element == null)
        {
            throw new System.Exception($"{this.View.Name} GetVisualElement type:<TextElement> path:{@"txt_string"} failed");
        }

        element.UpdateValue(convertedValue);
    }

    void TestCodeBindingViewModel_TestFloat_PropertyChanged(object sender, float preValue, float @value)
    {
        var convertedValue = FloatToStringConverter.Convert(@value);
        var element = this.View.GetVisualElement("txt_float");
        if (element == null)
        {
            throw new System.Exception($"{this.View.Name} GetVisualElement type: path:{@"txt_float"} failed");
        }

        element.UpdateValue(convertedValue);
    }

    void TestCodeBindingViewModel_TestBool_PropertyChanged(object sender, bool preValue, bool @value)
    {
        var convertedValue = @value;
        var element = this.View.GetVisualElement("txt_bool");
        if (element == null)
        {
            throw new System.Exception($"{this.View.Name} GetVisualElement type: path:{@"txt_bool"} failed");
        }

        element.UpdateValue(convertedValue);
    }
}
```

2、修改对应可绑定属性的set方法，并生成对应字段和泛型委托，经过反编译后的TestCodeBindingViewModel已经被改成了如下

```c#
[Binding("TestCodeBindingView", null, null, BindingType.OneWay)]
public class TestCodeBindingViewModel : ViewModel, ISynchronizeProperties
{
    private int _TestInt_BackingField;

    public PropertyChangedHandler<int> _TestInt_Changed;

    private string _TestString_BackingField;

    public PropertyChangedHandler<string> _TestString_Changed;

    private float _TestFloat_BackingField;

    public PropertyChangedHandler<float> _TestFloat_Changed;

    private bool _TestBool_BackingField;

    public PropertyChangedHandler<bool> _TestBool_Changed;

    [Binding("txt_int", typeof(IntToStringConverter), typeof(TextElement), BindingType.OneWay)]
    public int TestInt
    {
        get
        {
            return _TestInt_BackingField;
        }
        set
        {
            _ = _TestInt_BackingField;
            if (!_TestInt_BackingField.Equals(value))
            {
                int testInt_BackingField = _TestInt_BackingField;
                _TestInt_BackingField = value;
                _TestInt_Changed?.Invoke(this, testInt_BackingField, value);
            }
        }
    }

    [Binding("txt_string", typeof(TextElement), null, BindingType.OneWay)]
    public string TestString
    {
        get
        {
            return _TestString_BackingField;
        }
        set
        {
            if (_TestString_BackingField == null || !_TestString_BackingField.Equals(value))
            {
                string testString_BackingField = _TestString_BackingField;
                _TestString_BackingField = value;
                _TestString_Changed?.Invoke(this, testString_BackingField, value);
            }
        }
    }

    [Binding("txt_float", null, typeof(FloatToStringConverter), BindingType.OneWay)]
    public float TestFloat
    {
        get
        {
            return _TestFloat_BackingField;
        }
        set
        {
            _ = _TestFloat_BackingField;
            if (!_TestFloat_BackingField.Equals(value))
            {
                float testFloat_BackingField = _TestFloat_BackingField;
                _TestFloat_BackingField = value;
                _TestFloat_Changed?.Invoke(this, testFloat_BackingField, value);
            }
        }
    }

    [Binding("txt_bool", null, null, BindingType.OneWay)]
    public bool TestBool
    {
        get
        {
            return _TestBool_BackingField;
        }
        set
        {
            _ = _TestBool_BackingField;
            if (!_TestBool_BackingField.Equals(value))
            {
                bool testBool_BackingField = _TestBool_BackingField;
                _TestBool_BackingField = value;
                _TestBool_Changed?.Invoke(this, testBool_BackingField, value);
            }
        }
    }

    void ISynchronizeProperties.Synchronize()
    {
        _TestInt_Changed?.Invoke(this, TestInt, TestInt);
        _TestString_Changed?.Invoke(this, TestString, TestString);
        _TestFloat_Changed?.Invoke(this, TestFloat, TestFloat);
        _TestBool_Changed?.Invoke(this, TestBool, TestBool);
    }
}
```

如此便实现了ViewModel到View的变化通知，并且以上生成的代码是不可见的并不会影响使用者的代码习惯。

和传统的绑定方式相比，因为为每个属性都生成了对应的泛型委托，所以不存在拆装箱。

# 各个模块详解

Model作为存储游戏运行过程中产生和需要的数据，可以来自于网络消息、配置表、游戏中间数据等。

ViewModel用来存储某个界面的数据，可以来自于Model或View产生的数据。

ViewBehavior用于操作ViewModel的变化，主要用于实现UI的控制逻辑。

ValueConverer用于将ViewModel中的数据转换为View所需要的数据，从而实现同一数据的不同表现。

View作为各个UI控件的容器，负责表现层的组装。

VisualElement是一个界面的最小单位，实现每个控件的功能，例如Button、Text等。

其中View和VisualElement可以根据不同UI框架进行扩展，例如实现UGUI、FGUI等不同UI框架的相关接口。

&nbsp;
