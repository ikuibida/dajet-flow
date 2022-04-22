using System.Reflection;

namespace DaJet.Flow
{
    public static class ReflectionUtilities
    {
        public static Type GetTypeByName(string name)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                //TODO: load assemblies which are not referenced or not loaded yet

                Type? type = assembly.GetType(name);

                if (type is not null)
                {
                    return type;
                }
            }

            return null!;
        }
    }
}