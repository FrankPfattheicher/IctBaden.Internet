using System;
using System.Text;

namespace IctBaden.Internet.Calendar
{
    public class VAlarm
    {
        /// <summary>
        ///  AUDIO, DISPLAY, EMAIL, PROCEDURE
        /// </summary>
        public string Action { get; set; }
        public string Description { get; set; }
        public TimeSpan Trigger { get; set; }

        public VAlarm()
        {
            Action = "DISPLAY";
        }

        public string GetText()
        {
            var text = new StringBuilder();
            text.AppendLine("BEGIN:VALARM");
            text.AppendLine($"ACTION:{Action}");
            if (!string.IsNullOrEmpty(Description)) text.AppendLine($"DESCRIPTION:{Description}");
            // TRIGGER:-P0DT0H10M0S
            text.AppendLine($"TRIGGER:-P{Trigger.TotalDays}T{Trigger.TotalHours}H{Trigger.Minutes}M{Trigger.Seconds}S");
            text.AppendLine("END:VALARM");
            return text.ToString();
        }

    }
}