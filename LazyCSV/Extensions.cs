using System.Text;

namespace LazyCSV
{
    public static class Extensions
    {
        public static string FlattenToString<T>(this IEnumerable<T> array)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendJoin(',', array);

            return stringBuilder.ToString();
        }
    }
}
