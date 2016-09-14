using System;
using System.Threading.Tasks;
using System.Web.Http;
using ServerTrack.Infrastructure;
using ServerTrack.Logic;
using ServerTrack.Logic.Models;

namespace ServerTrack.Api.Controllers
{
    public class ServerTrackerController : ApiController
    {
        private readonly IServerTracker _serverTracker;
        public ServerTrackerController()
        {
            _serverTracker = ServerTracker.GetInstance(new DateTimeService());
        }



        //I'd prefer to use a post here, but for the ease of use / testing for one time, I'll keep it a get
        [HttpGet]
        public async Task<bool> SubmitServerLoad(string serverName, double cpuLoad, double ramLoad)
        {
            try
            {
                await _serverTracker.RecordLoad(serverName, cpuLoad, ramLoad);
                return true;
            }
            catch (Exception ex)
            {
                //Log ex somewhere internally
                return false;
            }
        }

        [HttpGet]
        public async Task<LoadByIncrement> GetServerLoadForLastHour(string serverName)
        {
            return await _serverTracker.AverageLoadForLastHour(serverName);
        }

        [HttpGet]
        public async Task<LoadByIncrement> GetServerLoadForLastDay(string serverName)
        {
            return await _serverTracker.AverageLoadForLastDay(serverName);
        }
    }
}
