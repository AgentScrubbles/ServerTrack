using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ServerTrack.Infrastructure;
using ServerTrack.Logic.Models;

namespace ServerTrack.Logic
{
    public interface IServerTracker
    {
        Task RecordLoad(string serverName, double cpuLoad, double ramLoad);
        Task<LoadByIncrement> AverageLoadForLastHour(string serverName);
        Task<LoadByIncrement> AverageLoadForLastDay(string serverName);

    }
    public class ServerTracker : IServerTracker
    {
        private readonly IDateTimeService _dateTimeService;
        private static readonly object _loq = new object();
        private ConcurrentDictionary<string, ConcurrentDictionary<DateTime, ConcurrentBag<Load>>> _loads;

        private static IServerTracker _instance;

        // Usually I'd prever dependency injection, but for the sake of time I'll use a singleton
        public static IServerTracker GetInstance(IDateTimeService dateTimeService)
        {
            lock (_loq)
            {
                return _instance ?? (_instance = new ServerTracker(dateTimeService));
            }
        }

        //Used only for testing
        public static void DestroyInstance()
        {
            lock (_loq)
            {
                _instance = null;
            }
        }

        private ServerTracker(IDateTimeService dateTimeService)
        {
            _dateTimeService = dateTimeService;
            _loads = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, ConcurrentBag<Load>>>();
        }

        public async Task RecordLoad(string serverName, double cpuLoad, double ramLoad)
        {
            await Task.Run(() =>
            {
                var now = _dateTimeService.GetCurrent();
                var load = new Load {CpuLoad = cpuLoad, LoggedTime = now, RamLoad = ramLoad, ServerName = serverName};
                lock (_loads)
                {
                    if (!_loads.ContainsKey(serverName))
                    {
                        _loads[serverName] = new ConcurrentDictionary<DateTime, ConcurrentBag<Load>>();
                    }
                    lock (_loads[serverName])
                    {
                        if (!_loads[serverName].ContainsKey(now))
                        {
                            _loads[serverName][now] = new ConcurrentBag<Load>();
                        }
                        _loads[serverName][now].Add(load);
                    }
                }
            });
        }

        public async Task<LoadByIncrement> AverageLoadForLastHour(string serverName)
        {
            return await Task.Run(() =>
            {
                lock (_loads)
                {

                    var loads = _loads.Get(serverName);
                    if (loads == null) return null;
                    lock (loads)
                    {
                        var oneHourAgo = _dateTimeService.GetCurrent().Subtract(TimeSpan.FromHours(1));
                        var spannedLoads =
                            loads.SelectMany(k => k.Value).Where(k => k != null).Where(k => k.LoggedTime >= oneHourAgo).ToList();
                        var loadByIncrement = new LoadByIncrement
                        {
                            Loads = spannedLoads.ToIndexedArray(k => k.LoggedTime.Minute, k => AverageLoads(k.ToList()), 60),
                            Average = AverageLoads(spannedLoads)
                        };
                        return loadByIncrement;
                    }
                }
            });
        }

        public static Load AverageLoads(ICollection<Load> loads)
        {
            if (loads == null || !loads.Any())
            {
                return null;
            }
            if (loads.Count == 1)
            {
                return loads.First();
            }
            return new Load()
            {
                CpuLoad = loads.Average(k => k.CpuLoad),
                RamLoad = loads.Average(k => k.RamLoad),
                LoggedTime = loads.Min(k => k.LoggedTime),
                ServerName = loads.First().ServerName
            };
        } 

        public async Task<LoadByIncrement> AverageLoadForLastDay(string serverName)
        {
            return await Task.Run(() =>
            {
                lock (_loads)
                {

                    var loads = _loads.Get(serverName);
                    if (loads == null) return null;
                    lock (loads)
                    {
                        var oneDayAgo = _dateTimeService.GetCurrent().Subtract(TimeSpan.FromDays(1));
                        var spannedLoads =
                            loads.SelectMany(k => k.Value).Where(k => k != null).Where(k => k.LoggedTime >= oneDayAgo).ToList();
                        var loadByIncrement = new LoadByIncrement
                        {
                            Loads = spannedLoads.ToIndexedArray(k => k.LoggedTime.Hour, k => AverageLoads(k.ToList()), 24),
                            Average = AverageLoads(spannedLoads)
                        };
                        return loadByIncrement;
                    }
                }
            });
        }
    }
}
