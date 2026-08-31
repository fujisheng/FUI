using System;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

namespace FUI.Editor.Installation
{
    /// <summary>
    /// 可选 Package 在当前 Editor 会话中的安装状态。
    /// </summary>
    internal enum OptionalPackageState
    {
        NotInstalled,
        Installing,
        Installed,
        Failed,
    }

    /// <summary>
    /// 提供安装器 UI 渲染所需的只读状态快照。
    /// </summary>
    internal readonly struct OptionalPackageStatus
    {
        public OptionalPackageState State { get; }
        public string InstalledVersion { get; }
        public string ErrorMessage { get; }

        public OptionalPackageStatus(
            OptionalPackageState state,
            string installedVersion = null,
            string errorMessage = null)
        {
            State = state;
            InstalledVersion = installedVersion;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 抽象单个异步 UPM 安装请求，便于串行调度和测试。
    /// </summary>
    internal interface IOptionalPackageRequest
    {
        bool IsCompleted { get; }
        bool Succeeded { get; }
        string ErrorMessage { get; }
    }

    /// <summary>
    /// 抽象已安装 Package 查询和 UPM Add 操作。
    /// </summary>
    internal interface IOptionalPackageClient
    {
        IReadOnlyDictionary<string, string> GetInstalledPackages();
        IOptionalPackageRequest Add(string identifier);
    }

    /// <summary>
    /// 解析可选 Package 依赖，并保证同一时间只运行一个 UPM 请求。
    /// </summary>
    internal sealed class OptionalPackageInstaller
    {
        readonly IOptionalPackageClient client;
        readonly Dictionary<string, string> failures = new Dictionary<string, string>();
        readonly Queue<OptionalPackageDescriptor> pendingPackages = new Queue<OptionalPackageDescriptor>();

        OptionalPackageDescriptor activePackage;
        OptionalPackageDescriptor requestedPackage;
        IOptionalPackageRequest activeRequest;

        public bool IsBusy => activeRequest != null || pendingPackages.Count > 0;

        public event Action Changed;

        public OptionalPackageInstaller(IOptionalPackageClient client = null)
        {
            this.client = client ?? new UnityOptionalPackageClient();
        }

        public OptionalPackageStatus GetStatus(OptionalPackageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (activePackage == descriptor || pendingPackages.Contains(descriptor))
            {
                return new OptionalPackageStatus(OptionalPackageState.Installing);
            }

            var installedPackages = client.GetInstalledPackages();
            if (installedPackages.TryGetValue(descriptor.PackageId, out var version))
            {
                return new OptionalPackageStatus(OptionalPackageState.Installed, version);
            }

            if (failures.TryGetValue(descriptor.PackageId, out var error))
            {
                return new OptionalPackageStatus(OptionalPackageState.Failed, errorMessage: error);
            }

            return new OptionalPackageStatus(OptionalPackageState.NotInstalled);
        }

        public bool TryInstall(OptionalPackageDescriptor descriptor)
        {
            if (descriptor == null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }
            if (IsBusy || GetStatus(descriptor).State == OptionalPackageState.Installed)
            {
                return false;
            }

            failures.Remove(descriptor.PackageId);
            requestedPackage = descriptor;
            var installedPackages = client.GetInstalledPackages();
            if (!TryQueueDependencies(descriptor, installedPackages, new HashSet<string>()))
            {
                requestedPackage = null;
                Changed?.Invoke();
                return false;
            }

            pendingPackages.Enqueue(descriptor);
            StartNext();
            Changed?.Invoke();
            return true;
        }

        bool TryQueueDependencies(
            OptionalPackageDescriptor descriptor,
            IReadOnlyDictionary<string, string> installedPackages,
            HashSet<string> visiting)
        {
            if (!visiting.Add(descriptor.PackageId))
            {
                failures[descriptor.PackageId] = $"Optional package dependency cycle includes {descriptor.PackageId}.";
                return false;
            }

            foreach (var dependencyId in descriptor.Dependencies)
            {
                if (installedPackages.ContainsKey(dependencyId) || IsQueued(dependencyId))
                {
                    continue;
                }

                var dependency = OptionalPackageCatalog.Find(dependencyId);
                if (dependency == null)
                {
                    failures[descriptor.PackageId] = $"Unknown optional package dependency: {dependencyId}.";
                    visiting.Remove(descriptor.PackageId);
                    return false;
                }
                if (!TryQueueDependencies(dependency, installedPackages, visiting))
                {
                    visiting.Remove(descriptor.PackageId);
                    return false;
                }
                pendingPackages.Enqueue(dependency);
            }

            visiting.Remove(descriptor.PackageId);
            return true;
        }

        bool IsQueued(string packageId)
        {
            if (activePackage != null && activePackage.PackageId == packageId)
            {
                return true;
            }

            foreach (var descriptor in pendingPackages)
            {
                if (descriptor.PackageId == packageId)
                {
                    return true;
                }
            }
            return false;
        }

        void StartNext()
        {
            if (activeRequest != null || pendingPackages.Count == 0)
            {
                return;
            }

            activePackage = pendingPackages.Dequeue();
            activeRequest = client.Add(activePackage.Identifier);
        }

        /// <summary>
        /// 由 Editor update 驱动请求状态提交，测试可以直接调用。
        /// </summary>
        public void Update()
        {
            if (activeRequest == null || !activeRequest.IsCompleted)
            {
                return;
            }

            var completedPackage = activePackage;
            var completedRequest = activeRequest;
            activePackage = null;
            activeRequest = null;

            if (!completedRequest.Succeeded)
            {
                failures[completedPackage.PackageId] = string.IsNullOrWhiteSpace(completedRequest.ErrorMessage)
                    ? $"Package Manager failed to install {completedPackage.Identifier}."
                    : completedRequest.ErrorMessage;
                if (requestedPackage != null && requestedPackage != completedPackage)
                {
                    failures[requestedPackage.PackageId] =
                        $"Dependency {completedPackage.PackageId} failed: {failures[completedPackage.PackageId]}";
                }
                pendingPackages.Clear();
                requestedPackage = null;
            }
            else
            {
                StartNext();
                if (activeRequest == null)
                {
                    requestedPackage = null;
                }
            }

            Changed?.Invoke();
        }
    }

    /// <summary>
    /// 使用 Unity Package Manager API 的安装客户端。
    /// </summary>
    internal sealed class UnityOptionalPackageClient : IOptionalPackageClient
    {
        public IReadOnlyDictionary<string, string> GetInstalledPackages()
        {
            var result = new Dictionary<string, string>();
            var packages = PackageInfo.GetAllRegisteredPackages();
            foreach (var package in packages)
            {
                result[package.name] = package.version;
            }
            return result;
        }

        public IOptionalPackageRequest Add(string identifier)
        {
            return new UnityOptionalPackageRequest(Client.Add(identifier));
        }
    }

    /// <summary>
    /// 将 Unity AddRequest 适配为安装器可轮询的请求。
    /// </summary>
    internal sealed class UnityOptionalPackageRequest : IOptionalPackageRequest
    {
        readonly AddRequest request;

        public bool IsCompleted => request.IsCompleted;
        public bool Succeeded => request.Status == StatusCode.Success;
        public string ErrorMessage => request.Error?.message;

        public UnityOptionalPackageRequest(AddRequest request)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
        }
    }
}
