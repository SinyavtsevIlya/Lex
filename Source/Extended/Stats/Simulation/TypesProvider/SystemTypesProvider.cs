using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanory.Lex.Stats
{
    public class SystemTypesProvider : SystemTypesProviderBase
    {
        public override IEnumerable<Type> GetSystemTypes(EcsTypesScanner scanner)
        {
            return scanner.
                GetComponentTypes().Where(t => typeof(IStat).IsAssignableFrom(t))
                .Select(statType => typeof(CalculateStatSystem<>).MakeGenericType(statType)).ToList();
        }
    }
}
