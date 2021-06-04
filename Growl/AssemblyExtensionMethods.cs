namespace Growl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    public static class AssemblyExtensionMethods
    {
        public static IEnumerable<Type> GetImplementationsOf<T>(this Assembly assembly) =>
            assembly.GetTypes()
                .Where(x => !x.IsInterface && x.IsAssignableTo(typeof(T)));
    }
}