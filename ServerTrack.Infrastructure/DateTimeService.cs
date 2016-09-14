using System;

namespace ServerTrack.Infrastructure
{
    public interface IDateTimeService
    {
        DateTime GetCurrent();
    }
    public class DateTimeService : IDateTimeService
    {
        public DateTime GetCurrent()
        {
            return DateTime.Now;
        }
    }
}
