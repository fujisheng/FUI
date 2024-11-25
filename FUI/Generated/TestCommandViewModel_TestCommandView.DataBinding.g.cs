﻿//This file was generated by FUICompiler
//The purpose is to achieve binding functionality
//FUICompiler:https://github.com/fujisheng/FUICompiler.git
//FUI:https://github.com/fujisheng/FUI.git
//If you have any questions, please raise them on GitHub
using FUI;
using FUI.Test;
using FUI.UGUI.Control;
using System;
using UnityEngine;

namespace Test.Command
{
    [FUI.ViewModelAttribute(typeof(TestCommandViewModel))]
    [FUI.ViewAttribute("TestCommandView")]
    public class __TestCommandViewModel_TestCommandView_Binding_Generated : FUI.BindingContext
    {
        BoolToStringConverter BoolToStringConverter = new BoolToStringConverter();
        public __TestCommandViewModel_TestCommandView_Binding_Generated(FUI.IView view, FUI.Bindable.ObservableObject viewModel) : base(view, viewModel)
        {
        }

        protected override void Binding()
        {
            if (this.ViewModel is TestCommandViewModel TestCommandViewModel)
            {
                TestCommandViewModel._ToggleValue_Changed += PropertyChanged__TestCommandViewModel_ToggleValue__toggleValue_FUI_UGUI_Control_TextElement_Text;
                TestCommandViewModel._ToggleValue_Changed += PropertyChanged__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn;
                BindingV2VM__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(TestCommandViewModel);
                BindingCommand__TestCommandViewModel___EventMethod_ToggleValueChangedAction__toggle_ToggleElement_OnValueChanged(TestCommandViewModel);
                BindingCommand__TestCommandViewModel_OnToggleChanged__toggle_ToggleElement_OnValueChanged(TestCommandViewModel);
                return;
            }
        }

        protected override void Unbinding()
        {
            if (this.ViewModel is TestCommandViewModel TestCommandViewModel)
            {
                TestCommandViewModel._ToggleValue_Changed -= PropertyChanged__TestCommandViewModel_ToggleValue__toggleValue_FUI_UGUI_Control_TextElement_Text;
                TestCommandViewModel._ToggleValue_Changed -= PropertyChanged__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn;
                UnbindingV2VM__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(TestCommandViewModel);
                UnbindingCommand__TestCommandViewModel___EventMethod_ToggleValueChangedAction__toggle_ToggleElement_OnValueChanged(TestCommandViewModel);
                UnbindingCommand__TestCommandViewModel_OnToggleChanged__toggle_ToggleElement_OnValueChanged(TestCommandViewModel);
                return;
            }
        }

        void PropertyChanged__TestCommandViewModel_ToggleValue__toggleValue_FUI_UGUI_Control_TextElement_Text(object sender, bool preValue, bool @value)
        {
            var convertedValue = BoolToStringConverter.Convert(@value);
            var element = FUI.Extensions.ViewExtensions.GetElement<FUI.UGUI.Control.TextElement>(this.View, @"toggleValue");
            element.Text?.SetValue(convertedValue);
        }

        void PropertyChanged__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(object sender, bool preValue, bool @value)
        {
            var convertedValue = @value;
            var element = FUI.Extensions.ViewExtensions.GetElement<FUI.UGUI.Control.ToggleElement>(this.View, @"toggle");
            element.IsOn?.SetValue(convertedValue);
        }

        void V2VMInvocation__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(bool oldValue, bool newValue)
        {
            if (this.ViewModel is TestCommandViewModel TestCommandViewModel)
            {
                TestCommandViewModel.ToggleValue = newValue;
            }
        }

        void BindingV2VM__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<FUI.UGUI.Control.ToggleElement>(this.View, @"toggle");
            element.IsOn.OnValueChanged += V2VMInvocation__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn;
        }

        void UnbindingV2VM__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<FUI.UGUI.Control.ToggleElement>(this.View, @"toggle");
            element.IsOn.OnValueChanged -= V2VMInvocation__TestCommandViewModel_ToggleValue__toggle_FUI_UGUI_Control_ToggleElement_IsOn;
        }

        void BindingCommand__TestCommandViewModel___EventMethod_ToggleValueChangedAction__toggle_ToggleElement_OnValueChanged(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<ToggleElement>(this.View, "toggle");
            element.OnValueChanged?.AddListener(TestCommandViewModel.__EventMethod_ToggleValueChangedAction);
        }

        void UnbindingCommand__TestCommandViewModel___EventMethod_ToggleValueChangedAction__toggle_ToggleElement_OnValueChanged(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<ToggleElement>(this.View, "toggle");
            element.OnValueChanged?.RemoveListener(TestCommandViewModel.__EventMethod_ToggleValueChangedAction);
        }

        void BindingCommand__TestCommandViewModel_OnToggleChanged__toggle_ToggleElement_OnValueChanged(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<ToggleElement>(this.View, "toggle");
            element.OnValueChanged?.AddListener(TestCommandViewModel.OnToggleChanged);
        }

        void UnbindingCommand__TestCommandViewModel_OnToggleChanged__toggle_ToggleElement_OnValueChanged(TestCommandViewModel TestCommandViewModel)
        {
            var element = FUI.Extensions.ViewExtensions.GetElement<ToggleElement>(this.View, "toggle");
            element.OnValueChanged?.RemoveListener(TestCommandViewModel.OnToggleChanged);
        }
    }
}