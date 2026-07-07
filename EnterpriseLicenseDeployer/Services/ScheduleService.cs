using System;

namespace EnterpriseLicenseDeployer.Services
{
    /// <summary>
    /// Pure helper for computing the next daily run time at a fixed hour/minute.
    /// </summary>
    public class ScheduleService
    {
        public DateTime GetNextRunTime(int hour, int minute, DateTime now)
        {
            var todayRun = new DateTime(now.Year, now.Month, now.Day, hour, minute, 0);
            return now <= todayRun ? todayRun : todayRun.AddDays(1);
        }
    }
}
