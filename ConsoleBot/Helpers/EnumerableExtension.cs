namespace ConsoleBot.Helpers
{
    public static class EnumerableExtension
    {
        public static IEnumerable<T> GetBatchByNumber<T>(this IEnumerable<T> source,
            int batchSize,
            int batchNumber)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (batchSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(batchSize));
            if (batchNumber < 0)
                throw new ArgumentOutOfRangeException(nameof(batchNumber));
            return source.Skip(batchSize * batchNumber).Take(batchSize);
        }
    }
}
