namespace TutorFlow.Domain.Common;

// Generic argument-precondition helpers only. These carry no business
// meaning — a business rule (e.g. "a Session must have exactly one Tutor")
// is enforced by the owning aggregate itself, never by this shared utility.
public static class Guard
{
    public static class Against
    {
        public static T Null<T>(T? value, string parameterName) where T : class
        {
            if (value is null)
            {
                throw new ArgumentNullException(parameterName);
            }

            return value;
        }

        public static string NullOrWhiteSpace(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
            }

            return value;
        }

        public static int NegativeOrZero(int value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        public static TimeSpan NegativeOrZero(TimeSpan value, string parameterName)
        {
            if (value <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        public static decimal NegativeOrZero(decimal value, string parameterName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, value, "Value must be greater than zero.");
            }

            return value;
        }

        public static Guid Default(Guid value, string parameterName)
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Value cannot be an empty Guid.", parameterName);
            }

            return value;
        }

        public static DateTime Default(DateTime value, string parameterName)
        {
            if (value == default)
            {
                throw new ArgumentException("Value cannot be an unset DateTime.", parameterName);
            }

            return value;
        }
    }
}
