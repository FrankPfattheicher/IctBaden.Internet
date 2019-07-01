using System;
using System.Linq;
using IctBaden.Internet.Calendar;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Internet.Test.Calendar
{
    public class CalendarTests
    {

        [Fact]
        public void CreateEmptyCalendar()
        {
            var calendar = new VCalendar();

            Assert.NotNull(calendar);

            var textLines = calendar.GetText().Split(new []{ Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(textLines.Length == 6);
        }

        [Fact]
        public void CreateCalendarWithUtcEventShouldHaveNoTimezone()
        {
            var calendar = new VCalendar();
            var start = DateTime.SpecifyKind(DateTime.Parse("1.5.2016 20:15"), DateTimeKind.Local);
            var end = DateTime.SpecifyKind(DateTime.Parse("1.5.2016 21:45"), DateTimeKind.Local);

            var vEvent = new VEvent
            {
                Start = start.ToUniversalTime(),
                End = end.ToUniversalTime(),
                Summary = "Tatort",
                Location = "ARD"
            };
            calendar.Events.Add(vEvent);

            var textLines = calendar.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(textLines.Length == 18);

            var tzLine = textLines.FirstOrDefault(line => line.Contains("VTIMEZONE"));
            Assert.Null(tzLine);
        }

        [Fact]
        public void CreateCalendarWithLocalEventAndNoTimezoneShouldHaveDefaultLocalZone()
        {
            var calendar = new VCalendar();
            var start = DateTime.SpecifyKind(DateTime.Parse("1.5.2016 20:15"), DateTimeKind.Local);
            var end = DateTime.SpecifyKind(DateTime.Parse("1.5.2016 21:45"), DateTimeKind.Local);

            var vEvent = new VEvent
            {
                Start = start,
                End = end,
                Summary = "Tatort",
                Location = "ARD"
            };
            calendar.Events.Add(vEvent);

            var textLines = calendar.GetText().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            Assert.True(textLines.Length == 34);

            var tzLine = textLines.FirstOrDefault(line => line.Contains("VTIMEZONE"));
            Assert.NotNull(tzLine);
        }

    }
}
