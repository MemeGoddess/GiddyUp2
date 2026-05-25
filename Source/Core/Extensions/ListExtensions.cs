using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiddyUpCore.Core.Extensions
{
    public static class ListExtensions
    {
        public static bool SplitIntoTwo<T>(
            this IEnumerable<T> collection, out List<T> trueList, out List<T> falseList,
            Func<T, bool> predicate)
        {
            trueList = [];
            falseList = [];
            foreach (var item in collection)
            {
                if (predicate(item))
                    trueList.Add(item);
                else
                    falseList.Add(item);
            }

            return trueList.Any();
        }
    }
}
