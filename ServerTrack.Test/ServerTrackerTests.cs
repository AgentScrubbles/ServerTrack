using System;
using System.Threading.Tasks;
using AutoMoq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServerTrack.Infrastructure;

namespace ServerTrack.Test
{
    [TestClass]
    public class ServerTrackerTests
    {
        private AutoMoqer _moq;

        private IServerTracker _serverTracker;
        private Mock<IDateTimeService> _dateTimeServiceMock;
        private IDateTimeService _dateTimeService;
            
        [TestInitialize]
        public void Setup()
        {
            _moq = new AutoMoqer();

            
            _dateTimeServiceMock = _moq.GetMock<IDateTimeService>();
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13));
            _dateTimeService = _dateTimeServiceMock.Object;
            _serverTracker = new ServerTracker(_dateTimeService);
        }

        [TestMethod]
        public async Task TestZeros()
        {
            await _serverTracker.RecordLoad("test1", 0, 0);

            var load = await _serverTracker.AverageLoadForLastHour("test1");

            Assert.AreEqual(0, load.Average.CpuLoad);
            Assert.AreEqual(0, load.Average.RamLoad);
        }

        [TestMethod]
        public async Task TestSimpleAverageHour()
        {
            //Log one time at 6pm exactly
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 18, 0, 0));
            await _serverTracker.RecordLoad("test1", 0, 0);

            //Log another time at 6:30pm
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 18, 30, 0));
            await _serverTracker.RecordLoad("test1", 100, 10);

            //Get the average of the previous 60 minutes at exactly 6:59:59pm
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 18, 59, 59));
            var load = await _serverTracker.AverageLoadForLastHour("test1");

            //Average should be half
            Assert.AreEqual(50, load.Average.CpuLoad);
            Assert.AreEqual(5, load.Average.RamLoad);

            //Times at 30 minutes should be the same as 6:30pm
            Assert.AreEqual(100, load.Loads[30].CpuLoad);
            Assert.AreEqual(10, load.Loads[30].RamLoad);
        }

        [TestMethod]
        public async Task TestSimpleAverageDaily()
        {
            //Log one time at 6pm exactly
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 1, 0, 0));
            await _serverTracker.RecordLoad("test1", 0, 0);

            //Log another time at 6:30pm
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 9, 0, 0));
            await _serverTracker.RecordLoad("test1", 100, 10);

            //Get the average of the previous 60 minutes at exactly 6:59:59pm
            _dateTimeServiceMock.Setup(k => k.GetCurrent()).Returns(new DateTime(2016, 9, 13, 23, 59, 59));
            var load = await _serverTracker.AverageLoadForLastDay("test1");

            //Average should be half
            Assert.AreEqual(50, load.Average.CpuLoad);
            Assert.AreEqual(5, load.Average.RamLoad);

            //Times at 30 minutes should be the same as 6:30pm
            Assert.AreEqual(100, load.Loads[9].CpuLoad);
            Assert.AreEqual(10, load.Loads[9].RamLoad);
        }


    }
}
