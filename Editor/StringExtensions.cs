using System.Collections.Generic;

namespace Uzi.Modeling.Editor
{
    static class StringExtensions
    {
        public static string ContentsToString <T> (this IEnumerable<T> collection, char delimeter)
        {
            string s = string.Empty;
            if(collection == null)
            {
                return s;
            }

            foreach (var value in collection)
            {
                if (s != string.Empty)
                {
                    s += delimeter;
                }
                if (value == null)
                {
                    s += "NULL";
                }
                else
                {
                    s += value.ToString();
                }
            }
            return s;
        }
    }
}