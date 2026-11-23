using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FUI.Editor.Inspection;
using FUI.Infrastructure.Binding;
using FUI.Rendering;

namespace FUI.Editor
{
    /// <summary>只读目标解析结果；不证明绑定订阅、转换器或用户交互已正确执行。</summary>
    public sealed class ViewBindingReport
    {
        public string ViewName { get; internal set; }
        public string ViewModelType { get; internal set; }
        public string BackendType { get; internal set; }
        public int TargetCount { get; internal set; }
        public IReadOnlyList<BindingTargetIssue> Issues { get; internal set; }
        public bool IsValid => Issues != null && Issues.Count == 0;
    }

    /// <summary>描述一个无法在真实视图中解析的绑定目标。</summary>
    public sealed class BindingTargetIssue
    {
        public string Code { get; internal set; }
        public string Path { get; internal set; }
        public string Capability { get; internal set; }
        public string Member { get; internal set; }
        public string SourceMember { get; internal set; }
        public string Message { get; internal set; }
    }

    /// <summary>基于生成清单检查真实视图的目标能力，不执行绑定数据或命令。</summary>
    public static class BindingInspection
    {
        /// <summary>只检查路径、接口与成员对象，不读取其Value或调用命令/转换器。</summary>
        public static ViewBindingReport Inspect(IView view, BindingManifest manifest)
        {
            var issues = new List<BindingTargetIssue>();
            var report = new ViewBindingReport
            {
                ViewName = view?.Name, ViewModelType = manifest?.viewModelType,
                BackendType = view?.GetType().FullName, Issues = issues.AsReadOnly(),
            };
            if (view == null || manifest == null)
            {
                issues.Add(new BindingTargetIssue { Code = view == null ? "view-missing" : "manifest-missing", Message = "The view or generated manifest is unavailable; binding completeness is unknown." });
                return report;
            }
            foreach (var binding in manifest.properties ?? new List<PropertyBindingDescriptor>())
            {
                var target = binding?.targetInfo;
                Check(target?.path, target?.type, target?.propertyName, binding?.propertyInfo?.name);
            }
            foreach (var binding in manifest.commands ?? new List<CommandBindingDescriptor>())
            {
                var target = binding?.targetInfo;
                Check(target?.path, target?.type, target?.propertyName, binding?.commandInfo?.name);
            }
            return report;

            void Check(string path, string typeName, string member, string source)
            {
                report.TargetCount++;
                if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(member))
                {
                    Add("metadata-invalid", "The target path or member metadata is empty.");
                    return;
                }
                var capability = ResolveType(typeName);
                if (capability == null)
                {
                    Add("type-unresolved", "The target type is unavailable or ambiguous; the binding cannot be validated.");
                    return;
                }
                if (!typeof(IElement).IsAssignableFrom(capability))
                {
                    Add("metadata-invalid", "The target type is not an IElement capability.");
                    return;
                }
                try
                {
                    var element = view.GetElement(path, capability);
                    if (element == null)
                    {
                        Add(view.GetElement(path, typeof(IElement)) == null ? "missing-node" : "missing-capability", "The live view cannot resolve this path and capability.");
                        return;
                    }
                    var property = capability.GetProperty(member) ?? capability.GetInterfaces()
                        .Select(type => type.GetProperty(member)).FirstOrDefault(candidate => candidate != null);
                    if (property == null || !property.CanRead || property.GetIndexParameters().Length != 0)
                    {
                        Add("missing-member", "The capability has no readable binding member with this name.");
                    }
                    else if (property.GetValue(element) == null)
                    {
                        Add("null-member", "The binding property or command instance is null.");
                    }
                }
                catch (Exception error)
                {
                    Add("inspection-error", (error is TargetInvocationException wrapped ? wrapped.InnerException : error)?.Message ?? error.Message);
                }

                void Add(string code, string message) => issues.Add(new BindingTargetIssue
                {
                    Code = code, Path = path, Capability = typeName, Member = member,
                    SourceMember = source, Message = message,
                });
            }
        }

        /// <summary>读取指定 ViewModel 的生成绑定要求，供资源导出验证作者目标。</summary>
        public static IReadOnlyList<(string Path, Type Capability, string Source)> DescribeTargets(Type model, string contract)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            var manifest = BindingCatalog.Instance.GetManifest(model, contract)
                ?? throw new InvalidOperationException($"Missing binding manifest for {model.FullName} and {contract}.");
            var context = model.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic)
                .SingleOrDefault(type => type.GetCustomAttribute<ViewModelAttribute>(false)?.Type == model &&
                    type.GetCustomAttribute<ViewAttribute>(false)?.ViewName == manifest.viewName)
                ?? throw new InvalidOperationException($"Missing generated binding context for {model.FullName} and {manifest.viewName}.");
            var requirements = new List<(string Path, Type Capability, string Source)>();
            for (var current = context; current != null; current = current.BaseType)
            {
                var sourceModel = current.GetCustomAttribute<ViewModelAttribute>(false)?.Type;
                var sourceContract = current.GetCustomAttribute<ViewAttribute>(false)?.ViewName;
                if (sourceModel == null)
                {
                    break;
                }
                var source = BindingCatalog.Instance.GetManifest(sourceModel, sourceContract)
                    ?? throw new InvalidOperationException($"Missing inherited binding manifest for {sourceModel.FullName}.");
                foreach (var property in source.properties)
                {
                    Add(property.targetInfo.path, property.targetInfo.type, property.propertyInfo.name);
                }
                foreach (var command in source.commands)
                {
                    Add(command.targetInfo.path, command.targetInfo.type, command.commandInfo.name);
                }
            }
            return requirements.AsReadOnly();

            void Add(string path, string capability, string member)
            {
                var type = ResolveType(capability);
                if (type == null)
                {
                    throw new InvalidOperationException($"Cannot resolve {capability} for {model.FullName}.{member}.");
                }
                requirements.Add((path, type, member));
            }
        }

        static Type ResolveType(string name)
        {
            if (string.IsNullOrEmpty(name)) { return null; }
            var candidates = AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(name, false)).Where(type => type != null).Distinct().ToArray();
            return candidates.Length == 1 ? candidates[0] : null;
        }

        internal static ViewBindingReport InspectContext(IView view, Type contextType, Func<Type, string, BindingManifest> resolve)
        {
            var reports = new List<ViewBindingReport>();
            var seen = new HashSet<(Type model, string contract)>();
            string modelName = null;
            for (var current = contextType; current != null; current = current.BaseType)
            {
                var model = current.GetCustomAttribute<ViewModelAttribute>(false)?.Type;
                var contract = current.GetCustomAttribute<ViewAttribute>(false);
                if (model == null && contract == null) { continue; }
                modelName ??= model?.FullName;
                if (model == null || contract == null)
                {
                    reports.Add(Inspect(view, null));
                    continue;
                }
                if (!seen.Add((model, contract.ViewName ?? string.Empty))) { continue; }
                reports.Add(Inspect(view, resolve(model, contract.ViewName ?? string.Empty)));
            }
            if (reports.Count == 0) { return Inspect(view, null); }
            return new ViewBindingReport
            {
                ViewName = view?.Name, ViewModelType = modelName, BackendType = view?.GetType().FullName,
                TargetCount = reports.Sum(report => report.TargetCount),
                Issues = reports.SelectMany(report => report.Issues).ToList().AsReadOnly(),
            };
        }

        /// <summary>主线程调用；包括列表/动态子投影，只返回新的诊断对象，不暴露共享清单或内部实例。</summary>
        public static IReadOnlyList<ViewBindingReport> InspectEnabledViews(string viewName = null)
        {
            var reports = new List<ViewBindingReport>();
            foreach (var instance in ViewInstanceRegistry.EnabledInstances)
            {
                var view = instance.View;
                if (view == null || (viewName != null && view.Name != viewName)) { continue; }
                var report = InspectContext(view, instance.BindingContext?.GetType(), BindingCatalog.Instance.GetManifest);
                report.ViewModelType ??= instance.ViewModel?.GetType().FullName;
                reports.Add(report);
            }
            return reports.OrderBy(report => report.ViewName, StringComparer.Ordinal).ToList().AsReadOnly();
        }
    }
}
