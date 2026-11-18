using System;
using System.Linq;
using System.Reflection;
namespace FUI.Editor
{
    /// <summary>编辑器与生成器采用相同的接口继承、遮蔽和冲突规则。</summary>
    public static class ElementMemberResolver
    {
        public static MemberInfo Resolve(Type expectedType, string memberName)
        {
            if (expectedType == null) { throw new ArgumentNullException(nameof(expectedType)); }
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            if (!expectedType.IsInterface)
            {
                return expectedType.GetProperty(memberName, flags) as MemberInfo
                    ?? expectedType.GetField(memberName, flags)
                    ?? throw new MissingMemberException(expectedType.FullName, memberName);
            }
            var own = expectedType.GetMember(memberName, flags | BindingFlags.DeclaredOnly).Where(IsBindingMember).ToArray();
            var members = own.Length > 0 ? own : expectedType.GetInterfaces()
                .SelectMany(x => x.GetMember(memberName, flags | BindingFlags.DeclaredOnly)).Where(IsBindingMember).Distinct().ToArray();
            if (own.Length == 0)
            {
                members = members.Where(x => !members.Any(other => !Equals(x, other) && other.DeclaringType.GetInterfaces().Contains(x.DeclaringType))).ToArray();
            }
            if (members.Length == 0) { throw new MissingMemberException(expectedType.FullName, memberName); }
            if (members.Length != 1) { throw new AmbiguousMatchException($"Control interface '{expectedType.FullName}' has conflicting inherited declarations for '{memberName}'."); }
            return members[0];
        }
        static bool IsBindingMember(MemberInfo member) => member is PropertyInfo || member is FieldInfo;
    }
}
