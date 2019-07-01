using System;
using System.Collections.Generic;
using System.Text;

namespace IctBaden.Internet.Calendar
{
    public class VEvent
    {
        public string Uid { get; set; }

        /// <summary>
        /// Sets the event as an all-day event.
        /// Hour, minute and second prats of start end end are ignored.
        /// </summary>
        public bool AllDay { get; set; }
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public DateTime Stamp { get; set; }
        public DateTime Created { get; set; }

        // optional properties
        /// <summary>
        /// CN="(domain)":MAILTO:(mail address)
        /// </summary>
        public string Organizer { get; set; }
        public List<string> Attendees { get; set; }
        /// <summary>
        /// The events title
        /// </summary>
        public string Summary { get; set; }
        public string Description { get; set; }
        public DateTime LastModified { get; set; }
        public string Location { get; set; }
        
        /// <summary>
        /// "OPAQUE"      Blocks or opaque on busy time searches.
        /// "TRANSPARENT" Transparent on busy time searches.
        /// Default value is OPAQUE
        /// </summary>
        public string Transparency { get; set; }
        /// <summary>
        /// "CONFIRMED"           Indicates event is definite
        /// "CANCELLED"           Indicates event was cancelled
        /// </summary>
        public string Status { get; set; }

        public VAlarm Alarm { get; set; }


        public VEvent()
        {
            Uid = Guid.NewGuid().ToString("N");
            Stamp = DateTime.UtcNow;
            Created = DateTime.UtcNow;
            LastModified = DateTime.UtcNow;
            Transparency = "OPAQUE";
            Status = "CONFIRMED";
        }

        public string GetText()
        {
            var text = new StringBuilder();
            text.AppendLine("BEGIN:VEVENT");
            // header
            text.AppendLine($"UID:{Uid}");

            // definition
            if (AllDay)
            {
                text.AppendLine($"DTSTART;VALUE=DATE:{Start.ToString("yyyyMMdd")}");
                text.AppendLine($"DTEND;VALUE=DATE:{End.ToString("yyyyMMdd")}");
            }
            else
            {
                text.AppendLine($"DTSTART:{Start.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")}");
                text.AppendLine($"DTEND:{End.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")}");
            }

            text.AppendLine($"DTSTAMP:{Stamp.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")}");
            text.AppendLine($"CREATED:{Created.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")}");
            text.AppendLine($"LAST-MODIFIED:{LastModified.ToUniversalTime().ToString("yyyyMMddTHHmmssZ")}");
            text.AppendLine($"TRANSP:{Transparency}");
            text.AppendLine($"STATUS:{Status}");

            if (!string.IsNullOrEmpty(Organizer)) text.AppendLine($"ORGANIZER:{Organizer}");
            if (!string.IsNullOrEmpty(Location)) text.AppendLine($"X-LIC-LOCATION:{Location}");
            if (Attendees != null)
            {
                foreach (var attendee in Attendees)
                {
                    text.AppendLine($"ATTENDEE:{attendee}");
                }
            }
            if (!string.IsNullOrEmpty(Description)) text.AppendLine($"DESCRIPTION:{Description}");
            if (!string.IsNullOrEmpty(Summary)) text.AppendLine($"SUMMARY:{Summary}");

            // alarm
            if (Alarm != null)
            {
                text.AppendLine(Alarm.GetText());
            }

            // end
            text.AppendLine("END:VEVENT");
            return text.ToString();
        }
    }
}