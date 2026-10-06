namespace TabularDB.Core.Validation
{
    public static class DomainValidation
    {
        public static void Ensure(bool condition, string message)
        {
            if (!condition)
            {
                throw new DomainValidationException(message);
            }
        }

        public static TValue Require<TValue>(TValue? value, string message)
            where TValue : class
        {
            Ensure(value is not null, message);
            return value!;
        }
    }
}
