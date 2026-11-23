using System;
using global::URUI.Elements;
using Controls = global::URUI.Controls;

namespace FUI.Rendering.URUI
{
    /// <summary>Maps a native control to its default FUI binding capabilities.</summary>
    internal static class StandardElementAdapters
    {
        internal static IElement Create(Element node, string name, Type capability)
        {
            var adapterType = node switch
            {
                Controls.Button => typeof(ButtonElement),
                Controls.Slider => typeof(SliderElement),
                Controls.Toggle => typeof(ToggleElement),
                Controls.Image => typeof(ImageElement),
                Controls.TextField => typeof(InputFieldElement),
                _ when IsTextTag(node.TagName) => typeof(TextElement),
                _ => typeof(ElementAdapter)
            };

            if (!capability.IsAssignableFrom(adapterType))
            {
                return null;
            }

            if (adapterType == typeof(ButtonElement))
            {
                return new ButtonElement(node, name);
            }
            if (adapterType == typeof(SliderElement))
            {
                return new SliderElement(node, name);
            }
            if (adapterType == typeof(ToggleElement))
            {
                return new ToggleElement(node, name);
            }
            if (adapterType == typeof(ImageElement))
            {
                return new ImageElement(node, name);
            }
            if (adapterType == typeof(InputFieldElement))
            {
                return new InputFieldElement(node, name);
            }
            if (adapterType == typeof(TextElement))
            {
                return new TextElement(node, name);
            }
            return new ElementAdapter(node, name);
        }

        internal static bool IsTextTag(string tag)
        {
            return tag is "label" or "span" or "p" or "output" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6";
        }

        internal static Type GetAuthoredAdapterType(string tag, string inputType)
        {
            if (tag == "input")
            {
                return (inputType ?? "text").ToLowerInvariant() switch
                {
                    "range" => typeof(SliderElement),
                    "checkbox" => typeof(ToggleElement),
                    "text" or "password" => typeof(InputFieldElement),
                    _ => typeof(ElementAdapter)
                };
            }

            return tag switch
            {
                "button" => typeof(ButtonElement),
                "img" => typeof(ImageElement),
                "textarea" => typeof(InputFieldElement),
                _ when IsTextTag(tag) => typeof(TextElement),
                _ => typeof(ElementAdapter)
            };
        }
    }
}
