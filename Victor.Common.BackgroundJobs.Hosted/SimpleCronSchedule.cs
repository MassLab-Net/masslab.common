namespace Victor.Common.BackgroundJobs.Hosted;

internal sealed class SimpleCronSchedule
{
    private readonly CronField _minute;
    private readonly CronField _hour;

    private SimpleCronSchedule(CronField minute, CronField hour)
    {
        _minute = minute;
        _hour = hour;
    }

    public static SimpleCronSchedule Parse(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Cron expression is required.", nameof(expression));

        var parts = expression.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 5)
            throw new NotSupportedException("Hosted background jobs support 5-field cron expressions only.");

        if (parts[2] != "*" || parts[3] != "*" || parts[4] != "*")
            throw new NotSupportedException("Hosted background jobs support minute/hour cron schedules only.");

        return new SimpleCronSchedule(CronField.Parse(parts[0], 0, 59), CronField.Parse(parts[1], 0, 23));
    }

    public DateTimeOffset GetNextOccurrence(DateTimeOffset afterUtc)
    {
        var cursor = new DateTimeOffset(
            afterUtc.Year,
            afterUtc.Month,
            afterUtc.Day,
            afterUtc.Hour,
            afterUtc.Minute,
            0,
            TimeSpan.Zero).AddMinutes(1);

        for (var i = 0; i < 366 * 24 * 60; i++)
        {
            if (_minute.Matches(cursor.Minute) && _hour.Matches(cursor.Hour))
                return cursor;

            cursor = cursor.AddMinutes(1);
        }

        throw new InvalidOperationException("Could not calculate next cron occurrence.");
    }

    private sealed class CronField
    {
        private readonly bool[] _allowed;

        private CronField(bool[] allowed) => _allowed = allowed;

        public static CronField Parse(string value, int min, int max)
        {
            var allowed = new bool[max + 1];

            foreach (var part in value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (part == "*")
                {
                    Fill(allowed, min, max, 1);
                    continue;
                }

                if (part.StartsWith("*/", StringComparison.Ordinal))
                {
                    if (!int.TryParse(part[2..], out var step) || step <= 0)
                        throw new FormatException($"Invalid cron step '{part}'.");

                    Fill(allowed, min, max, step);
                    continue;
                }

                if (!int.TryParse(part, out var number) || number < min || number > max)
                    throw new FormatException($"Invalid cron value '{part}'.");

                allowed[number] = true;
            }

            if (!allowed.Any(x => x))
                throw new FormatException($"Invalid cron field '{value}'.");

            return new CronField(allowed);
        }

        public bool Matches(int value) => value >= 0 && value < _allowed.Length && _allowed[value];

        private static void Fill(bool[] allowed, int min, int max, int step)
        {
            for (var i = min; i <= max; i += step)
                allowed[i] = true;
        }
    }
}
