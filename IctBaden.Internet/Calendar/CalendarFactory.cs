using System;
using System.Collections.Generic;

namespace IctBaden.Internet.Calendar
{
    public class CalendarFactory
    {
        /// <summary>
        /// Creates a simple ICS file content with default calendar settings.
        /// If the time of start and end is 00:00:00 the event is created as an all-day event.
        /// </summary>
        /// <param name="eventName">The title or subject of the event</param>
        /// <param name="start">The starting time</param>
        /// <param name="end">The ending time</param>
        /// <param name="organisator">The optional event organisator</param>
        /// <param name="description">An optional description</param>
        /// <param name="location">An optional event location</param>
        /// <returns></returns>
        public string CreateIcsFile(string eventName, DateTime start, DateTime end, 
            string organisator = null, string description = null, string location = null)
        {
            var allDay = start.Hour == 0
                          && start.Minute == 0
                          && start.Second == 0
                          && end.Hour == 0
                          && end.Minute == 0
                          && end.Second == 0;

            var calendar = new VCalendar()
            {
                Events = new List<VEvent>()
                {
                    new VEvent()
                    {
                        Summary = eventName,
                        Start = start,
                        End = end,
                        AllDay = allDay,
                        Organizer = organisator,
                        Description = description,
                        Location = location
                    }
                }
            };

            return calendar.GetText();
        }
    }
}