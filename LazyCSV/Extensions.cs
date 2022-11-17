using System.Text;

namespace LazyCSV
{
    public static class Extensions
    {
        public static string FlattenToString<T>(this IEnumerable<T> array)
        {
            StringBuilder stringBuilder = new StringBuilder();
            foreach (T item in array) {
                stringBuilder.Append(item.ToString());
                stringBuilder.Append(", ");
            }

            return stringBuilder.ToString();
        }
    }
}
