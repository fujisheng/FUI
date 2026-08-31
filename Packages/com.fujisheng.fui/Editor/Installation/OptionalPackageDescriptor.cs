using System;

namespace FUI.Editor.Installation
{
    /// <summary>
    /// 安装器中一个可选 Package 的不可变描述。
    /// </summary>
    internal sealed class OptionalPackageDescriptor
    {
        public string PackageId { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string RecommendedVersion { get; }
        public string DocumentationUrl { get; }
        public string[] Dependencies { get; }
        public OptionalPackageCategory Category { get; }
        public bool IsAvailableForInstallation { get; }

        public string Identifier => $"{PackageId}@{RecommendedVersion}";

        public OptionalPackageDescriptor(
            string packageId,
            string displayName,
            string description,
            string recommendedVersion,
            string documentationUrl,
            OptionalPackageCategory category,
            bool isAvailableForInstallation,
            params string[] dependencies)
        {
            PackageId = string.IsNullOrWhiteSpace(packageId)
                ? throw new ArgumentException("Package ID cannot be empty.", nameof(packageId))
                : packageId;
            DisplayName = string.IsNullOrWhiteSpace(displayName)
                ? throw new ArgumentException("Display name cannot be empty.", nameof(displayName))
                : displayName;
            Description = description ?? string.Empty;
            RecommendedVersion = string.IsNullOrWhiteSpace(recommendedVersion)
                ? throw new ArgumentException("Recommended version cannot be empty.", nameof(recommendedVersion))
                : recommendedVersion;
            DocumentationUrl = documentationUrl ?? string.Empty;
            Category = category;
            IsAvailableForInstallation = isAvailableForInstallation;
            Dependencies = dependencies ?? Array.Empty<string>();
        }
    }

    /// <summary>
    /// 安装器可选项的展示分组。
    /// </summary>
    internal enum OptionalPackageCategory
    {
        Tools,
        Extensions,
        Dependency,
    }

    /// <summary>
    /// 维护已经发布并通过兼容性验证的可选 Package 清单。
    /// </summary>
    internal static class OptionalPackageCatalog
    {
        internal static readonly OptionalPackageDescriptor UnityCli = new OptionalPackageDescriptor(
            "com.fujisheng.unitycli",
            "UnityCli",
            "Unity Editor automation bridge used by external tools.",
            "0.4.0",
            "https://github.com/fujisheng/unitycli",
            OptionalPackageCategory.Tools,
            false);

        internal static readonly OptionalPackageDescriptor FuiCli = new OptionalPackageDescriptor(
            "com.fujisheng.fui.cli",
            "FUI CLI",
            "Optional AI-oriented inspection, interaction, and UI workflow tools.",
            "1.0.0",
            "https://github.com/fujisheng/fui-cli",
            OptionalPackageCategory.Tools,
            false,
            "com.fujisheng.unitycli");

        internal static readonly OptionalPackageDescriptor Cecil = new OptionalPackageDescriptor(
            "com.unity.nuget.mono-cecil",
            "Unity Mono Cecil",
            "Unity's official Cecil package required only by Mixed property mode.",
            "1.11.6",
            "https://docs.unity3d.com/Packages/com.unity.nuget.mono-cecil@1.11/manual/index.html",
            OptionalPackageCategory.Dependency,
            true);

        internal static readonly OptionalPackageDescriptor[] PublicPackages =
        {
            UnityCli,
            FuiCli,
        };

        internal static OptionalPackageDescriptor Find(string packageId)
        {
            if (UnityCli.PackageId == packageId)
            {
                return UnityCli;
            }
            if (FuiCli.PackageId == packageId)
            {
                return FuiCli;
            }
            if (Cecil.PackageId == packageId)
            {
                return Cecil;
            }
            return null;
        }
    }
}
