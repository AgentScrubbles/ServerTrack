using System;

namespace ServerTrack.Logic.Models
{
    public class Load
    {
        public string ServerName { get; set; }
        public double CpuLoad { get; set; }
        public double RamLoad { get; set; }
        public DateTime LoggedTime { get; set; }
    }
}
