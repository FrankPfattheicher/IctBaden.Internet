using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IctBaden.Internet.Calendar
{
    public class VCalendar
    {
        public string ProductId { get; set; }
        public string Version { get; set; }
        public string Calscale { get; set; }
        /// <summary>
        /// PUBLISH, REQUEST, REPLY, ADD, CANCEL, REFRESH, COUNTER, DECLINECOUNTER
        /// </summary>
        public string Method { get; set; }

        // optional properties
        public string Name { get; set; }
        public string TimezoneName { get; set; }
        public string Description { get; set; }

        public VTimezone Timezone { get; set; }
        public List<VEvent> Events { get; set; }

        public VCalendar()
        {
            ProductId = "-//ICT Baden GmbH//Framework Library 2016//DE";
            Version = "2.0";
            Calscale = "GREGORIAN";
            Method = "PUBLISH";
            Events = new List<VEvent>();
        }

        public string GetText()
        {
            var usedTimeZone = Timezone;

            var text = new StringBuilder();
            text.AppendLine("BEGIN:VCALENDAR");
            // header
            text.AppendLine($"PRODID:{ProductId}");
            text.AppendLine($"VERSION:{Version}");
            text.AppendLine($"CALSCALE:{Calscale}");
            text.AppendLine($"METHOD:{Method}");
            if(!string.IsNullOrEmpty(Name)) text.AppendLine($"X-WR-CALNAME:{Name}");
            if(!string.IsNullOrEmpty(TimezoneName)) text.AppendLine($"X-WR-TIMEZONE:{TimezoneName}");
            if(!string.IsNullOrEmpty(Description)) text.AppendLine($"X-WR-CALDESC:{Description}");

            var eventWithLocalTime = Events.FirstOrDefault(ev => ev.Start.Kind != DateTimeKind.Utc || ev.End.Kind != DateTimeKind.Utc);
            if (eventWithLocalTime != null && usedTimeZone == null)
            {
                usedTimeZone = VTimezone.Local;
            }

            // timezone definition
            if (usedTimeZone != null)
            {
                text.Append(usedTimeZone.GetText());
            }
            // events
            foreach (var vEvent in Events)
            {
                text.Append(vEvent.GetText());
            }

            // end
            text.AppendLine("END:VCALENDAR");
            return text.ToString();
        }

    }
}
