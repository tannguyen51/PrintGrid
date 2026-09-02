using System.Reflection;

namespace PrintGrid.Infrastructure.Shared.Persistence;

public static class ModuleAssemblyRegistry
{
    private static readonly List<Assembly> _persistenceAssemblies = new();

    public static IReadOnlyList<Assembly> PersistenceAssemblies => _persistenceAssemblies;

    public static void Register(Assembly assembly)
    {
        if (!_persistenceAssemblies.Contains(assembly))
        {
            _persistenceAssemblies.Add(assembly);
        }
    }
}
