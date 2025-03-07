using System.Reflection;

namespace NetworkGameEngine.DependencyInjection
{
    internal static class ReflectionHelper
    {
        internal static IEnumerable<MethodInfo> GetAllMethods(Type type)
        {
            var methods = new List<MethodInfo>();

            while (type != null)
            {
                methods.AddRange(type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
                type = type.BaseType;
            }

            return methods;
        }
    }
}
