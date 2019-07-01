using System;
using IctBaden.Internet.Calendar;
using Xunit;

namespace IctBaden.Internet.Test.Calendar
{
    public class TimezoneTests
    {
        [Fact]
        public void CreateLocalTimezone()
        {
            var timezone = VTimezone.Local;

            var text = timezone.GetText();

            Assert.True(text.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Length == 16);
        }
    }
}