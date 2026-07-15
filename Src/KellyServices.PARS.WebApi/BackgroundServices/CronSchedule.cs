using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace KellyServices.PARS.WebApi.BackgroundServices
{
    internal sealed class CronSchedule
    {
        private readonly HashSet<int> minutes; private readonly HashSet<int> hours; private readonly HashSet<int> days; private readonly HashSet<int> months; private readonly HashSet<int> weekdays;
        private CronSchedule(HashSet<int> minutes, HashSet<int> hours, HashSet<int> days, HashSet<int> months, HashSet<int> weekdays)
        { this.minutes = minutes; this.hours = hours; this.days = days; this.months = months; this.weekdays = weekdays; }
        internal static CronSchedule Parse(string expression)
        {
            var fields = expression?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (fields.Length != 5) throw new FormatException("ArchiveIngestion CronExpression must use five fields: minute hour day-of-month month day-of-week.");
            return new(ParseField(fields[0], 0, 59), ParseField(fields[1], 0, 23), ParseField(fields[2], 1, 31), ParseField(fields[3], 1, 12), ParseField(fields[4], 0, 6));
        }
        internal DateTime GetNextOccurrence(DateTime utcNow)
        {
            var candidate = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, utcNow.Hour, utcNow.Minute, 0, DateTimeKind.Utc).AddMinutes(1);
            for (var index = 0; index < 527040; index++, candidate = candidate.AddMinutes(1))
                if (minutes.Contains(candidate.Minute) && hours.Contains(candidate.Hour) && days.Contains(candidate.Day) && months.Contains(candidate.Month) && weekdays.Contains((int)candidate.DayOfWeek)) return candidate;
            throw new InvalidOperationException("CronExpression did not produce an occurrence within one year.");
        }
        private static HashSet<int> ParseField(string value, int minimum, int maximum)
        {
            var result = new HashSet<int>();
            foreach (var segment in value.Split(','))
            {
                var stepParts = segment.Split('/'); var step = stepParts.Length == 2 ? int.Parse(stepParts[1], CultureInfo.InvariantCulture) : 1;
                if (step < 1) throw new FormatException("Cron step must be positive.");
                var range = stepParts[0]; var start = minimum; var end = maximum;
                if (range != "*") { var bounds = range.Split('-'); start = int.Parse(bounds[0], CultureInfo.InvariantCulture); end = bounds.Length == 2 ? int.Parse(bounds[1], CultureInfo.InvariantCulture) : start; }
                if (start < minimum || end > maximum || start > end) throw new FormatException($"Cron value {segment} is outside {minimum}-{maximum}.");
                for (var current = start; current <= end; current += step) result.Add(current);
            }
            return result;
        }
    }
}
