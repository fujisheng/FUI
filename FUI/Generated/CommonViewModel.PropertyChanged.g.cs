﻿//This file was generated by FUICompiler
//The purpose is to achieve binding functionality
//FUICompiler:https://github.com/fujisheng/FUICompiler.git
//FUI:https://github.com/fujisheng/FUI.git
//If you have any questions, please raise them on GitHub
using FUI;
using FUI.Manager;
using FUI.Test;
using FUI.UGUI.Control;
using System;
using UnityEngine;

namespace Test.Layer
{
    public partial class CommonViewModel : FUI.ISynchronizeProperties
    {
        public string _Title_BackingField = "CommonView";
        public FUI.Bindable.PropertyChangedHandler<string> _Title_Changed;
        void FUI.ISynchronizeProperties.Synchronize()
        {
            _Title_Changed?.Invoke(this, this.Title, this.Title);
        }
    }
}