using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nanory.Lex.Stats
{
    public interface IStatView
    {
        IStatView SetMaxValue(int maxValue);
        IStatView SetValue(int value);
    }
}
