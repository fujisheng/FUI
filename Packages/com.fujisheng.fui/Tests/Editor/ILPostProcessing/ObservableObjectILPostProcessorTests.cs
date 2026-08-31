using FUI.Editor.ILPostProcessing;
using Mono.Cecil;
using Mono.Cecil.Cil;
using NUnit.Framework;
using System;
using System.Linq;

namespace FUI.Tests.ILPostProcessing
{
    public sealed class ObservableObjectILPostProcessorTests
    {
        [Test]
        public void MixedModeInstrumentsBindableAutoPropertyExactlyOnce()
        {
            using (var assembly = CreateAssembly(1, true, true))
            {
                var setter = FindSetter(assembly);

                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(assembly), Is.True);
                Assert.That(
                    setter.Body.Instructions.Count(instruction => IsSetHelperCall(instruction)),
                    Is.EqualTo(1));
                Assert.That(setter.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Ldfld), Is.True);
                Assert.That(setter.Body.Instructions.Any(instruction => instruction.OpCode == OpCodes.Stfld), Is.True);

                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(assembly), Is.False);
                Assert.That(
                    setter.Body.Instructions.Count(instruction => IsSetHelperCall(instruction)),
                    Is.EqualTo(1));
            }
        }

        [Test]
        public void PureAndUnconfiguredModesDoNotRewriteSetter()
        {
            using (var pureAssembly = CreateAssembly(0, true, true))
            using (var unconfiguredAssembly = CreateAssembly(null, true, true))
            {
                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(pureAssembly), Is.False);
                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(unconfiguredAssembly), Is.False);
                Assert.That(FindSetter(pureAssembly).Body.Instructions.Any(IsSetHelperCall), Is.False);
                Assert.That(FindSetter(unconfiguredAssembly).Body.Instructions.Any(IsSetHelperCall), Is.False);
            }
        }

        [Test]
        public void MixedModeSkipsUnboundAndNonAutomaticProperties()
        {
            using (var unboundAssembly = CreateAssembly(1, false, true))
            using (var manualAssembly = CreateAssembly(1, true, false))
            {
                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(unboundAssembly), Is.False);
                Assert.That(ObservableObjectILPostProcessor.ProcessAssembly(manualAssembly), Is.False);
                Assert.That(FindSetter(unboundAssembly).Body.Instructions.Any(IsSetHelperCall), Is.False);
                Assert.That(FindSetter(manualAssembly).Body.Instructions.Any(IsSetHelperCall), Is.False);
            }
        }

        static AssemblyDefinition CreateAssembly(int? propertyMode, bool bindProperty, bool automaticProperty)
        {
            var assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("FUI.ILPP.Test", new Version(1, 0, 0, 0)),
                "FUI.ILPP.Test",
                ModuleKind.Dll);
            var module = assembly.MainModule;
            var attributeBase = module.ImportReference(typeof(Attribute));

            var viewModuleAttribute = CreateAttributeType(module, "FUI.Binding", "ViewModuleAttribute", attributeBase);
            if (propertyMode.HasValue)
            {
                var moduleAttribute = new CustomAttribute(viewModuleAttribute.Methods[0]);
                moduleAttribute.Properties.Add(new CustomAttributeNamedArgument(
                    "PropertyMode",
                    new CustomAttributeArgument(module.TypeSystem.Int32, propertyMode.Value)));
                assembly.CustomAttributes.Add(moduleAttribute);
            }

            var bindAttribute = CreateAttributeType(module, "FUI.Binding", "BindAttribute", attributeBase);
            var observableObject = new TypeDefinition(
                "FUI.Binding",
                "ObservableObject",
                TypeAttributes.Public | TypeAttributes.Class,
                module.TypeSystem.Object);
            module.Types.Add(observableObject);

            var viewModel = new TypeDefinition(
                "FUI.Tests",
                "SampleViewModel",
                TypeAttributes.Public | TypeAttributes.Class,
                observableObject);
            module.Types.Add(viewModel);

            var backingField = automaticProperty
                ? new FieldDefinition("<Title>k__BackingField", FieldAttributes.Private, module.TypeSystem.String)
                : new FieldDefinition("title", FieldAttributes.Private, module.TypeSystem.String);
            viewModel.Fields.Add(backingField);

            var setter = new MethodDefinition(
                "set_Title",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                module.TypeSystem.Void);
            setter.Parameters.Add(new ParameterDefinition("value", ParameterAttributes.None, module.TypeSystem.String));
            var setterIl = setter.Body.GetILProcessor();
            setterIl.Append(setterIl.Create(OpCodes.Ldarg_0));
            setterIl.Append(setterIl.Create(OpCodes.Ldarg_1));
            setterIl.Append(setterIl.Create(OpCodes.Stfld, backingField));
            setterIl.Append(setterIl.Create(OpCodes.Ret));
            viewModel.Methods.Add(setter);

            var property = new PropertyDefinition("Title", PropertyAttributes.None, module.TypeSystem.String)
            {
                SetMethod = setter,
            };
            if (bindProperty)
            {
                property.CustomAttributes.Add(new CustomAttribute(bindAttribute.Methods[0]));
            }
            viewModel.Properties.Add(property);

            var helper = new MethodDefinition(
                "__Set_Title",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                module.TypeSystem.Void);
            helper.Parameters.Add(new ParameterDefinition("oldValue", ParameterAttributes.None, module.TypeSystem.String));
            helper.Parameters.Add(new ParameterDefinition("newValue", ParameterAttributes.None, module.TypeSystem.String));
            helper.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            viewModel.Methods.Add(helper);

            return assembly;
        }

        static TypeDefinition CreateAttributeType(
            ModuleDefinition module,
            string attributeNamespace,
            string name,
            TypeReference attributeBase)
        {
            var attribute = new TypeDefinition(
                attributeNamespace,
                name,
                TypeAttributes.Public | TypeAttributes.Class,
                attributeBase);
            var constructor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public
                    | MethodAttributes.HideBySig
                    | MethodAttributes.SpecialName
                    | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            constructor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
            attribute.Methods.Add(constructor);
            module.Types.Add(attribute);
            return attribute;
        }

        static MethodDefinition FindSetter(AssemblyDefinition assembly)
        {
            return assembly.MainModule.Types
                .Single(type => type.FullName == "FUI.Tests.SampleViewModel")
                .Methods
                .Single(method => method.Name == "set_Title");
        }

        static bool IsSetHelperCall(Instruction instruction)
        {
            return (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt)
                && instruction.Operand is MethodReference method
                && method.Name == "__Set_Title";
        }
    }
}
