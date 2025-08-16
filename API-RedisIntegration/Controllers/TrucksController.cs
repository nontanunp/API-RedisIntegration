using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace API_RedisIntegration.Controllers
{

    [ApiController]
    [Route("api/[action]")]
    public class TrucksController : ControllerBase
    {
        [HttpPost]
        //public async Task<IActionResult> Trucks(Trucks req)
        public async Task<IActionResult> Trucks(List<Trucks> req)
        {
            TrucksResponse result = new TrucksResponse();
            try
            {
                if (req == null)
                {
                    result.ErrorCode = 400;
                    result.ErrorDesc = "Invalid request data.";
                }
                else
                {
                    ConnectionMultiplexer con = ConnectionMultiplexer.Connect("localhost:6379");
                    IDatabase db = con.GetDatabase();
                    var jsonsAreas = JsonConvert.SerializeObject(req, Formatting.None);
                    db.StringSet("Trucks", jsonsAreas);
                    
                    result.ErrorCode = 200;
                    result.ErrorDesc = "Trucks data add success.";
                }
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.ErrorDesc = "Internal server error: " + ex.Message;
            }
            return Ok(result);
        }
    }
}
