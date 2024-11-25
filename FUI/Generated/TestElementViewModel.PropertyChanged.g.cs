﻿//This file was generated by FUICompiler
//The purpose is to achieve binding functionality
//FUICompiler:https://github.com/fujisheng/FUICompiler.git
//FUI:https://github.com/fujisheng/FUI.git
//If you have any questions, please raise them on GitHub
using FUI;
using FUI.Test;
using FUI.UGUI.Control;
using UnityEngine;

namespace Test.Command
{
    public partial class TestElementViewModel : FUI.ISynchronizeProperties
    {
        public string _Text_BackingField = "TextValue";
        public FUI.Bindable.PropertyChangedHandler<string> _Text_Changed;
        public string _Image_BackingField = "testIcon1";
        public FUI.Bindable.PropertyChangedHandler<string> _Image_Changed;
        public string _Texture_BackingField = "testIcon2";
        public FUI.Bindable.PropertyChangedHandler<string> _Texture_Changed;
        public int _Dropdown_BackingField = 0;
        public FUI.Bindable.PropertyChangedHandler<int> _Dropdown_Changed;
        public string _Input_BackingField = "";
        public FUI.Bindable.PropertyChangedHandler<string> _Input_Changed;
        public float _Slider_BackingField = 0.5f;
        public FUI.Bindable.PropertyChangedHandler<float> _Slider_Changed;
        public float _Scrollbar_BackingField = 0.5f;
        public FUI.Bindable.PropertyChangedHandler<float> _Scrollbar_Changed;
        public bool _ToggleValue_BackingField = true;
        public FUI.Bindable.PropertyChangedHandler<bool> _ToggleValue_Changed;
        public int _ButtonClickCount_BackingField = 0;
        public FUI.Bindable.PropertyChangedHandler<int> _ButtonClickCount_Changed;
        void FUI.ISynchronizeProperties.Synchronize()
        {
            _Text_Changed?.Invoke(this, this.Text, this.Text);
            _Image_Changed?.Invoke(this, this.Image, this.Image);
            _Texture_Changed?.Invoke(this, this.Texture, this.Texture);
            _Dropdown_Changed?.Invoke(this, this.Dropdown, this.Dropdown);
            _Input_Changed?.Invoke(this, this.Input, this.Input);
            _Slider_Changed?.Invoke(this, this.Slider, this.Slider);
            _Scrollbar_Changed?.Invoke(this, this.Scrollbar, this.Scrollbar);
            _ToggleValue_Changed?.Invoke(this, this.ToggleValue, this.ToggleValue);
            _ButtonClickCount_Changed?.Invoke(this, this.ButtonClickCount, this.ButtonClickCount);
        }
    }
}