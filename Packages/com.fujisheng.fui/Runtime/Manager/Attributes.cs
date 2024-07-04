using System;

namespace FUI.Manager
{
    public class DefaultViewConfigAttribute : Attribute
    {
        public int layer;
        public ViewType viewType;

        public DefaultViewConfigAttribute(Layer layer = Layer.Common, ViewType viewType = ViewType.NonFullScreen)
        {
            this.layer = (int)layer;
            this.viewType = viewType;
        }

        public DefaultViewConfigAttribute(int layer, ViewType viewType)
        {
            this.layer = layer;
            this.viewType = viewType;
        }
    }
}