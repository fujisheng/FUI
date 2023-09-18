using System;
using System.Collections.Generic;

using UnityEngine;

namespace FUI.UGUI
{
    [Serializable]
    public class BindingProperty
    {
        public string name;
        public string type;
        public string converterType;
        public string elementPath;
        public string elementType;

        public string converterValueType;
        public string converterTargetType;
        public string elementValueType;
    }

    [Serializable]
    public class BindingContext
    {
        public string type = typeof(ViewModel).FullName;
        [SerializeField]
        public List<BindingProperty> properties = new List<BindingProperty>();
    }

    [Serializable]
    public class BindingConfig
    {
        public string viewName = "TestView";
        [SerializeField]
        public List<BindingContext> contexts = new List<BindingContext>();
    }

    public class Binding : MonoBehaviour
    {
        [SerializeField]
        public BindingConfig config = new BindingConfig();
    }
}