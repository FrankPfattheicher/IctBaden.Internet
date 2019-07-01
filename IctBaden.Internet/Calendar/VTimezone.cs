using System;
using System.Linq;
using System.Text;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global

namespace IctBaden.Internet.Calendar
{
    public class VTimezone
    {
        public class DaylightSaving
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public int OffsetFromHours { get; set; }
            public int OffsetFromMinutes { get; set; }
            public int OffsetToHours{ get; set; }
            public int OffsetToMinutes { get; set; }
            public DateTime Start { get; set; }

            public string RuleFrequency { get; set; }
            public int RuleMonth { get; set; }
            public int RuleDay { get; set; }
            public DayOfWeek RuleDayOfWeek { get; set; }

            public DaylightSaving(string id)
            {
                Id = id;
                RuleFrequency = "YEARLY";
                RuleDay = -1;
            }

            public string GetText()
            {
                var text = new StringBuilder();
                text.AppendLine($"BEGIN:{Id}");

                text.AppendLine($"TZOFFSETFROM:{OffsetFromHours:+00;-00}{OffsetFromMinutes:D2}");
                text.AppendLine($"TZOFFSETTO:{OffsetToHours:+00;-00}{OffsetToMinutes:D2}");

                if (!string.IsNullOrEmpty(Name)) text.AppendLine($"TZNAME:{Name}");

                var dayOfWeek = RuleDayOfWeek.ToString().Substring(0, 2).ToUpper();
                text.AppendLine($"RRULE:FREQ={RuleFrequency};BYMONTH={RuleMonth};BYDAY={RuleDay}{dayOfWeek}");

                text.AppendLine($"END:{Id}");
                return text.ToString();
            }
        }

        public string Id { get; set; }

        // optional properties
        public string Location { get; set; }

        public DaylightSaving Daylight { get; set; }
        public DaylightSaving Standard { get; set; }

        public static VTimezone Local => FromTimeZoneInfo(TimeZoneInfo.Local);
        public static VTimezone Utc => FromTimeZoneInfo(TimeZoneInfo.Utc);

        public static VTimezone FromTimeZoneInfo(TimeZoneInfo info)
        {
            var timezone = new VTimezone
            {
                Id = info.Id,
                Location = info.DisplayName
            };

            if (!info.SupportsDaylightSavingTime)
                return timezone;

            var rule = info.GetAdjustmentRules().First();
            var toOffset = info.BaseUtcOffset + rule.DaylightDelta;

            timezone.Daylight = new DaylightSaving("DAYLIGHT")
            {
                Name = info.DaylightName,
                OffsetFromHours = rule.DaylightDelta.Hours,
                OffsetFromMinutes = rule.DaylightDelta.Minutes,
                OffsetToHours = toOffset.Hours,
                OffsetToMinutes = toOffset.Minutes,
                RuleMonth = rule.DaylightTransitionStart.Month,
                RuleDay = rule.DaylightTransitionStart.Day,
                RuleDayOfWeek = rule.DaylightTransitionStart.DayOfWeek,
                Start = rule.DateStart
            };

            timezone.Standard = new DaylightSaving("STANDARD")
            {
                Name = info.StandardName,
                OffsetFromHours = toOffset.Hours,
                OffsetFromMinutes = toOffset.Minutes,
                OffsetToHours = rule.DaylightDelta.Hours,
                OffsetToMinutes = rule.DaylightDelta.Minutes,
                RuleMonth = rule.DaylightTransitionEnd.Month,
                RuleDay = rule.DaylightTransitionEnd.Day,
                RuleDayOfWeek = rule.DaylightTransitionEnd.DayOfWeek,
                Start = rule.DateStart
            };

            return timezone;
        }

        public string GetText()
        {
            var text = new StringBuilder();
            text.AppendLine("BEGIN:VTIMEZONE");
            // header
            text.AppendLine($"TZID:{Id}");
            if (!string.IsNullOrEmpty(Location)) text.AppendLine($"X-LIC-LOCATION:{Location}");

            // definition
            text.Append(Daylight.GetText());
            text.Append(Standard.GetText());

            // end
            text.AppendLine("END:VTIMEZONE");
            return text.ToString();
        }

    }
}