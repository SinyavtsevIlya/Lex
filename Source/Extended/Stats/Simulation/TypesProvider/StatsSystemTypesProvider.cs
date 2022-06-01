using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanory.Lex.Stats
{
    [UnityEngine.Scripting.Preserve]
    public class StatsSystemTypesProvider : SystemTypesProviderBase
    {
        public override IEnumerable<Type> GetSystemTypes(EcsTypesScanner scanner)
        {
            return GetCalculateSystems(scanner)
                .Union(GetClampSystems(scanner));
        }

        private static IEnumerable<Type> GetCalculateSystems(EcsTypesScanner scanner)
        {
            return scanner.
                GetComponentTypes().Where(t => typeof(IStat).IsAssignableFrom(t))
                .Select(statType => typeof(CalculateStatSystem<>).MakeGenericType(statType)).ToList();
        }

        private static IEnumerable<Type> GetClampSystems(EcsTypesScanner scanner)
        {
            return scanner.
                GetComponentTypes().Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClamp<>)))
                .Select(t => new { clampType = t, maxType = t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IClamp<>)).GetGenericArguments()[0] })
                .Select(typeData => typeof(ClampStatSystem<,>).MakeGenericType(typeData.clampType, typeData.maxType))
                .ToList();
        }
    }
}
