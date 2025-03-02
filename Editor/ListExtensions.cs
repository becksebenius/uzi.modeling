using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    static class ListExtensions
    {
        public static T Pop<T> (this IList<T> list)
        {
            int end = list.Count - 1;
            var value = list[end];
            list.RemoveAt(end);
            return value;
        }
        
        public static List<T> Clone <T> (this List<T> list)
        {
            var clonedList = new List<T>(list.Capacity);
            for(int i = 0; i < list.Count; ++i)
            {
                clonedList.Add(list[i]);
            }
            return clonedList;
        }
    }
}