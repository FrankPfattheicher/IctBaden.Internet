using System;
using IctBaden.Internet.Calendar;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Internet.Test.Calendar
{
    public class IcsTests
    {
        [Fact]
        public void CreateSimpleEventIcsFileLocalTime()
        {
            var factory = new CalendarFactory();

            var ics = factory.CreateIcsFile("Tatort", DateTime.Parse("1.5.2016 20:15"), DateTime.Parse("1.5.2016 21:45"), "ARD");

            Assert.True(ics.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries).Length == 34);
        }

        [Fact]
        public void CreateSimpleEventIcsFileUtc()
        {
            var factory = new CalendarFactory();

            var ics = factory.CreateIcsFile("Tatort", DateTime.Parse("1.5.2016 20:15").ToUniversalTime(), DateTime.Parse("1.5.2016 21:45").ToUniversalTime(), "ARD");

            Assert.True(ics.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length == 18);
        }
    }
}