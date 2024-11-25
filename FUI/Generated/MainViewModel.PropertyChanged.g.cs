﻿//This file was generated by FUICompiler
//The purpose is to achieve binding functionality
//FUICompiler:https://github.com/fujisheng/FUICompiler.git
//FUI:https://github.com/fujisheng/FUI.git
//If you have any questions, please raise them on GitHub
using FUI;
using FUI.Test;
using FUI.UGUI.Control;
using System.Threading.Tasks;
using UnityEngine;

namespace Test.Sub
{
    public partial class MainViewModel : FUI.ISynchronizeProperties
    {
        public string _Titile_BackingField = "i am main view title";
        public FUI.Bindable.PropertyChangedHandler<string> _Titile_Changed;
        public SubViewModel _SubViewModel_BackingField = new SubViewModel();
        public FUI.Bindable.PropertyChangedHandler<SubViewModel> _SubViewModel_Changed;
        public string _ButtonText_BackingField;
        public FUI.Bindable.PropertyChangedHandler<string> _ButtonText_Changed;
        void FUI.ISynchronizeProperties.Synchronize()
        {
            _Titile_Changed?.Invoke(this, this.Titile, this.Titile);
            _SubViewModel_Changed?.Invoke(this, this.SubViewModel, this.SubViewModel);
            _ButtonText_Changed?.Invoke(this, this.ButtonText, this.ButtonText);
        }
    }
}