using FUI.Bindable;
using FUI.UGUI;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEditor.UIElements;

using UnityEngine;
using UnityEngine.UIElements;

namespace FUI.Editor.Drawer
{
    [CustomEditor(typeof(Binding))]
    public class BindingPropertyEditor : UnityEditor.Editor
    {
        const string BindingOutputPath = "./Binding";
        
        public override VisualElement CreateInspectorGUI()
        {
            return CreateBinding();
        }

        VisualElement CreateBinding()
        {
            var binding = target as Binding;
            binding.config.viewName = binding.gameObject.name;

            var rootElement = new VisualElement();
            var saveButton = new Button();
            saveButton.text = "Save";
            saveButton.name = "SaveButton";
            saveButton.visible = binding.config.contexts.Count > 0;
            saveButton.clicked += () =>
            {
                var fileName = $"{binding.config.viewName}.binding";
                var filePath = Path.Combine(BindingOutputPath, fileName);
                if(!Directory.Exists(BindingOutputPath)) 
                {
                    Directory.CreateDirectory(BindingOutputPath);
                }

                var json = JsonUtility.ToJson(binding.config, true);
                if(File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.WriteAllText(filePath, json);
                UnityEngine.Debug.Log($"save to {filePath}");
            };
            rootElement.Add(saveButton);

            var addContextBtn = new Button();
            addContextBtn.text = "Add Context";
            addContextBtn.name = "AddContextButton";
            addContextBtn.clicked += () =>
            {
                binding.config.contexts.Add(new UGUI.BindingContext());
                rootElement.Q<ListView>("ContextList").Rebuild();
                RefreshRoot(rootElement, binding);
            };
            rootElement.Add(addContextBtn);
           
            var contextList = new ListView
            {
                itemHeight = 200,
                makeItem = () => CreateContextItem(),
            };
            contextList.name = "ContextList";
            contextList.style.height = 400;
            contextList.bindItem = (e, i) =>
            {
                var itemData = contextList.itemsSource[i] as UGUI.BindingContext;
                RefreshContextItem(rootElement, contextList, e, i);
            };
            contextList.itemsSource = binding.config.contexts;
            contextList.selectionType = SelectionType.None;

            rootElement.Add(contextList);
            RefreshRoot(rootElement, binding);
            return rootElement;
        }

        void RefreshRoot(VisualElement rootElement, Binding binding)
        {
            var saveButton = rootElement.Q<Button>("SaveButton");
            saveButton.visible = binding.config.contexts.Count > 0;
            saveButton.style.height = binding.config.contexts.Count > 0 ? 20 : 0;

            var addContextBtn = rootElement.Q<Button>("AddContextButton");
            addContextBtn.visible = binding.config.contexts.Count == 0;
            addContextBtn.style.height = binding.config.contexts.Count == 0 ? 20 : 0;
        }

        void SetChoices<T>(PopupField<T> popupField, List<T> choices)
        {
            var prop = popupField.GetType().GetField("m_Choices", BindingFlags.NonPublic | BindingFlags.Instance);
            prop.SetValue(popupField, choices);
        }

        void RefreshContextItem(VisualElement root, ListView list, VisualElement itemView, int index)
        {
            var itemData = list.itemsSource[index] as UGUI.BindingContext;
            var observableObjects = GetHasCustomAttributeTypes<ObservableObjectAttribute>();
            var observableObjectSelector = itemView.Q<PopupField<Type>>("ObservableObjectSelector");
            SetChoices(observableObjectSelector, observableObjects);
            var selected = observableObjects.Find((x)=>x.FullName == itemData.type);
            if (selected != null)
            {
                observableObjectSelector.value = selected;
            }
            observableObjectSelector.RegisterCallback<ChangeEvent<Type>>((evt) =>
            {
                itemData.type = evt.newValue.FullName;
                if (evt.newValue != null)
                {
                    list.Rebuild();
                }
            });

            var propertyList = itemView.Q<ListView>("PropertyList");
            propertyList.itemsSource = itemData.properties;
            propertyList.style.height = propertyList.itemsSource.Count * propertyList.itemHeight;

            var addPropertyBtn = itemView.Q<Button>("AddPropertyBtn");
            addPropertyBtn.visible = observableObjectSelector.value != null && (propertyList.itemsSource == null || propertyList.itemsSource.Count == 0);
            addPropertyBtn.clicked += () =>
            {
                itemData.properties.Add(new BindingProperty());
                list.Rebuild();
            };

            var addContextBtn = itemView.Q<Button>("AddContextBtn");
            addContextBtn.clicked += () =>
            {
                if(index >= list.itemsSource.Count - 1)
                {
                    list.itemsSource.Add(new UGUI.BindingContext());
                }
                else
                {
                    list.itemsSource.Insert(index + 1, new UGUI.BindingContext());
                }

                list.Rebuild();
            };

            var removeContextBtn = itemView.Q<Button>("RemoveContextBtn");
            removeContextBtn.clicked += () =>
            {
                list.itemsSource.RemoveAt(index);
                list.Rebuild();
                RefreshRoot(root, target as Binding);
            };
        }

        static readonly List<string> Titles = new List<string> { "Property", "ValueConverter", "Element", "ElementType" };
        VisualElement CreateContextItem()
        {
            var element = new VisualElement();
            {
                var toolbar = new VisualElement();
                {
                    toolbar.style.justifyContent = Justify.FlexStart;
                    toolbar.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                    toolbar.style.width = 680;
                    toolbar.style.backgroundColor = new Color(38f / 255f, 38f / 255f, 38f / 255f, 1f);
                    var viewModelSelector = new PopupField<Type>
                    {
                        name = "ObservableObjectSelector",
                        formatSelectedValueCallback = (x) => x == null ? string.Empty : x.Name,
                        formatListItemCallback= (x) => x == null ? string.Empty : x.FullName,
                    };
                    viewModelSelector.style.width = 620;
                    toolbar.Add(viewModelSelector);


                    var addContextBtn = new Button
                    {
                        text = "+",
                        name = "AddContextBtn"
                    };
                    toolbar.Add(addContextBtn);

                    var removeContextBtn = new Button
                    {
                        text = "-",
                        name = "RemoveContextBtn"
                    };
                    toolbar.Add(removeContextBtn);
                }
                element.Add(toolbar);

                var title = new VisualElement();
                {
                    title.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
                    title.style.width = 620;
                    foreach(var t in Titles)
                    {
                        var titleItem = new TextElement { text = t };
                        titleItem.style.width = 155;
                        titleItem.style.left = 5;
                        titleItem.style.height = 30;
                        title.Add(titleItem);
                    }
                }
                element.Add(title);

                var propertyList = new ListView
                {
                    itemHeight = 20,
                    name = "PropertyList",
                    makeItem = () => CreatePropertyItem()
                };
                propertyList.bindItem = (e, i) =>
                {
                    RefreshPropertyItem(propertyList, e, i);
                };
                propertyList.style.height = 200;
                propertyList.selectionType = SelectionType.None;
                element.Add(propertyList);

                var addBtn = new Button
                {
                    text = "+",
                    name = "AddPropertyBtn"
                };
                element.Add(addBtn);
            }
            return element;
        }

        void RefreshPropertyItem(ListView list, VisualElement itemView, int index)
        {
            var itemData = list.itemsSource[index] as BindingProperty;

            var observableObjectType = list.parent.Q<PopupField<Type>>("ObservableObjectSelector").value;
            //要绑定的属性选择
            var propertySelector = itemView.Q<PopupField<PropertyInfo>>("PropertySelector");
            var properties = GetHasCustomAttributeProperties<ObservablePropertyAttribute>(observableObjectType);
            SetChoices(propertySelector, properties);
            propertySelector.RegisterCallback<ChangeEvent<PropertyInfo>>((evt) =>
            {
                itemData.name = evt.newValue.Name;
                itemData.type = UGUI.TypeInfo.Create(evt.newValue.PropertyType);
            });
            var selectedProperty = properties.Find((p) => p.Name == itemData.name);
            if(selectedProperty != null)
            {
                propertySelector.value = selectedProperty;
            }

            //值转换器
            var valueConverterSelector = itemView.Q<PopupField<Type>>("ValueConverterSelector");
            var valueConverters = new List<Type>
            {
                null,
            };
            
            var converters = GetAssignableTypes<IValueConverter>();
            foreach (var converter in converters)
            {
                if (!converter.IsAbstract)
                {
                    valueConverters.Add(converter);
                }
            }
            SetChoices(valueConverterSelector, valueConverters);
            valueConverterSelector.RegisterCallback<ChangeEvent<Type>>((evt) =>
            {
                if(evt.newValue == null)
                {
                    itemData.converterType = null;
                    itemData.converterValueType = null;
                    itemData.converterTargetType = null;
                }
                else
                {
                    var (valueType, targetType) = GetConverterArgumentType(evt.newValue);
                    itemData.converterType = UGUI.TypeInfo.Create(evt.newValue);
                    itemData.converterValueType = UGUI.TypeInfo.Create(valueType);
                    itemData.converterTargetType = UGUI.TypeInfo.Create(targetType);
                }
            });
            var selectedType = valueConverters.Find((t) => t?.FullName == itemData?.converterType?.fullName);
            valueConverterSelector.value = selectedType; 

            //绑定的Element路径
            var objectSelector = itemView.Q<ObjectField>("ObjectSelector");
            objectSelector.RegisterCallback<ChangeEvent<UnityEngine.Object>>((evt) =>
            {
                var obj = evt.newValue;
                itemData.elementPath = GetChildPath(evt.newValue as GameObject);
                list.Rebuild();
            });
            var root = (target as MonoBehaviour).gameObject;
            if (!string.IsNullOrEmpty(itemData.elementPath))
            {
                var obj = root.transform.Find(itemData.elementPath);
                if(obj != null)
                {
                    objectSelector.value = obj.gameObject;
                }
            }

            //绑定的Element类型
            var elements = new List<Type>();
            if (!string.IsNullOrEmpty(itemData.elementPath) && root.transform.Find(itemData.elementPath) != null)
            {
                foreach (var childElement in root.transform.Find(itemData.elementPath).GetComponents<IElement>())
                {
                    elements.Add(childElement.GetType());
                }
            }
            var elementSelector = itemView.Q<PopupField<Type>>("ElementSelector");
            SetChoices(elementSelector, elements);
            elementSelector.SetEnabled(!string.IsNullOrEmpty(itemData.elementPath));
            elementSelector.RegisterCallback<ChangeEvent<Type>>((evt) =>
            {
                itemData.elementType = UGUI.TypeInfo.Create(evt.newValue);
                itemData.elementValueType = UGUI.TypeInfo.Create(GetElementValueType(evt.newValue));
            });
            var selectedElement = elements.Find((t) => t.FullName == itemData.elementType.fullName);
            if (selectedElement != null)
            {
                elementSelector.value = selectedElement;
            }

            //添加属性按钮
            var addPropertyBtn = itemView.Q<Button>("AddPropertyBtn");
            addPropertyBtn.clicked += () =>
            {
                if(index >= list.itemsSource.Count - 1)
                {
                    list.itemsSource.Add(new BindingProperty());
                }
                else
                {
                    list.itemsSource.Insert(index + 1, new BindingProperty());
                }
                list.style.height = list.itemsSource.Count * list.itemHeight;
                list.Rebuild();
            };

            //删除属性按钮
            var removePropertyBtn = itemView.Q<Button>("RemovePropertyBtn");
            removePropertyBtn.clicked += () =>
            {
                list.itemsSource.RemoveAt(index);
                list.style.height = list.itemsSource.Count * list.itemHeight;
                list.Rebuild();
            };
        }

        VisualElement CreatePropertyItem()
        {
            var element = new VisualElement();
            element.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Row);
            const float width = 150;

            //属性选择框
            var propertySelector = new PopupField<PropertyInfo>
            {
                name = "PropertySelector",
                formatListItemCallback = (e) =>
                {
                    return e == null ? string.Empty : e.Name;
                },
                formatSelectedValueCallback = (e) =>
                {
                    return e == null ? string.Empty : e.Name;
                }
            };
            propertySelector.style.width = width;
            element.Add(propertySelector);

            //值转换器选择框
            var valueConverterSelector = new PopupField<Type>
            {
                name = "ValueConverterSelector",
                formatListItemCallback = (e) =>
                {
                    return e == null ? "None" : e.FullName;
                },
                formatSelectedValueCallback = (e) =>
                {
                    return e == null ? "None" : e.Name.Replace("Converter", string.Empty);
                },
            };
            valueConverterSelector.style.width = width;
            element.Add(valueConverterSelector);

            //Element选择框
            var objectSelector = new ObjectField
            {
                name = "ObjectSelector",
                objectType = typeof(GameObject)
            };
            objectSelector.style.width = width;
            element.Add(objectSelector);

            //Element类型选择框
            var elementSelector = new PopupField<Type>
            {
                name = "ElementSelector",
                formatSelectedValueCallback = (e) =>
                {
                    return e == null ? string.Empty : e.Name;
                },
                formatListItemCallback = (e) =>
                {
                    return e == null ? string.Empty : e.FullName;
                }
            };
            elementSelector.style.width = width;
            element.Add(elementSelector);

            //添加属性按钮
            var addPropertyBtn = new Button
            {
                text = "+",
                name = "AddPropertyBtn"
            };
            element.Add(addPropertyBtn);

            //删除属性按钮
            var removePropertyBtn = new Button
            {
                text = "-",
                name = "RemovePropertyBtn"
            };
            element.Add(removePropertyBtn);
            return element;
        }

        string GetChildPath(GameObject child)
        {
            var root = (target as MonoBehaviour).gameObject;
            var result = string.Empty;
            result += child.name;
            var current = child.transform;

            while (true)
            {
                current = current.transform.parent;
                if(current == null || current.gameObject == root)
                {
                    break;
                }

                result = $"{current.name}/{result}";
            }
            return result;
        }

        /// <summary>
        /// 获取一个Element的值类型
        /// </summary>
        /// <param name="elementType"></param>
        /// <returns></returns>
        Type GetElementValueType(Type elementType)
        {
            foreach(var @interface in elementType.GetInterfaces())
            {
                if(@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IElement))
                {
                    return @interface.GetGenericArguments()[0];
                }
            }
            return typeof(object);
        }

        /// <summary>
        /// 获取值转换器的参数类型
        /// </summary>
        /// <param name="converterType"></param>
        /// <returns></returns>
        (Type valueType, Type targetType) GetConverterArgumentType(Type converterType)
        {
            foreach(var @interface in converterType.GetInterfaces())
            {
                if(@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValueConverter<,,>))
                {
                    return (@interface.GetGenericArguments()[0], @interface.GetGenericArguments()[1]);
                }
            }
            return (typeof(object), typeof(object));
        }

        /// <summary>
        /// 获取派生自某个类型的所有类型
        /// </summary>
        List<Type> GetAssignableTypes<T>()
        {
            var t = typeof(T);
            var result = new List<Type>();  

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                result.AddRange(assembly.GetTypes().Where(x => t.IsAssignableFrom(x)));
            }

            return result;
        }

        /// <summary>
        /// 获取拥有某个自定义特性的所有类型
        /// </summary>
        List<Type> GetHasCustomAttributeTypes<T>() where T : Attribute
        {
            var result = new List<Type>();

            foreach(var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                result.AddRange(assembly.GetTypes().Where(x => x.GetCustomAttribute<T>(false) != null));
            }
            return result;
        }

        /// <summary>
        /// 获取一个类型拥有某个自定义特性的所有属性
        /// </summary>
        List<PropertyInfo> GetHasCustomAttributeProperties<T>(Type type) where T : Attribute
        {
            return type.GetProperties().Where(x => x.GetCustomAttribute<T>() != null).ToList();
        }
    }
}
