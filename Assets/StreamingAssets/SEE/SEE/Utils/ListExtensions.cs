namespace System.Collections.Generic
{
    public static class ListExtensions
    {
        public static void Resize<T>(this List<T> list, int count)
        {
            if (list.Count < count)
            {
                if (list.Capacity < count)
                {
                    list.Capacity = count;
                }

                int end = count - list.Count;
                for (int i = 0; i < end; i++)
                {
                    list.Add(default);
                }
            }
        }
    }
}
