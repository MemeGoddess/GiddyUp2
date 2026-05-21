using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiddyUpCore.Core.Extensions
{
    public static class ListExtensions
    {
        public static (List<T>, List<T>) SplitIntoTwo<T>(
            this IEnumerable<T> collection,
            Func<T, bool> predicate)
        {
            var truthyValues = new List<T>();
            var falsyValues = new List<T>();
            foreach (var item in collection)
            {
                if (predicate(item))
                    truthyValues.Add(item);
                else
                    falsyValues.Add(item);
            }
            return (truthyValues, falsyValues);
        }
    }
}
