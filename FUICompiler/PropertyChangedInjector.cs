using Mono.Cecil;
using Mono.Cecil.Cil;

namespace FUICompiler
{
    /// <summary>
    /// 属性赋值委托注入器
    /// </summary>
    public class PropertyChangedInjector : ITypeDefinationInjector
    {
        void ITypeDefinationInjector.Inject(Mono.Cecil.ModuleDefinition moduleDefinition, Mono.Cecil.TypeDefinition typeDefinition)
        {
            if(!Utility.IsObservableObject(typeDefinition))
            {
                return;
            }

            Console.WriteLine("inject property changed for " + typeDefinition.FullName);
            foreach (var property in typeDefinition.Properties)
            {
                if (!Utility.IsObservableProperty(property) || property.SetMethod == null)
                {
                    continue;
                }

                InjectPropertyChangedMethod(moduleDefinition, typeDefinition, property);
            }
        }

        /// <summary>
        /// 向可绑定的属性Set方法注入属性更改委托
        /// </summary>
        /// <param name="module"></param>
        /// <param name="property"></param>
        /// <param name="propertyChangedMethodName"></param>
        void InjectPropertyChangedMethod(ModuleDefinition module, TypeDefinition type, PropertyDefinition property)
        {
            property.SetMethod.Body.Instructions.Clear();
            var processor = property.SetMethod.Body.GetILProcessor();

            var ret = processor.Create(OpCodes.Ret);
            var changedFirst = processor.Create(OpCodes.Ldarg_0);
            var setValueFirst = processor.Create(OpCodes.Ldarg_0);

            // _Name_Changed?.Invoke(this, _name, value);
            processor.Append(processor.Create(OpCodes.Nop));
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            var propertyChangedField = type.GetField($"_{property.Name}_Changed");
            processor.Append(processor.Create(OpCodes.Ldfld, propertyChangedField));
            processor.Append(processor.Create(OpCodes.Dup));
            processor.Append(processor.Create(OpCodes.Brtrue_S, changedFirst));
            processor.Append(processor.Create(OpCodes.Pop));
            processor.Append(processor.Create(OpCodes.Br_S, setValueFirst));

            processor.Append(changedFirst);
            processor.Append(processor.Create(OpCodes.Ldarg_0));
            var field = type.GetField($"<{property.Name}>k__BackingField");
            processor.Append(processor.Create(OpCodes.Ldfld, field));
            processor.Append(processor.Create(OpCodes.Ldarg_1));
            processor.Append(processor.Create(OpCodes.Callvirt, propertyChangedField.FieldType.Resolve().GetMethod("Invoke")));

            // _name = value;
            processor.Append(setValueFirst);
            processor.Append(processor.Create(OpCodes.Ldarg_1));
            processor.Append(processor.Create(OpCodes.Stfld, field));
            processor.Append(ret);
        }
    }
}
