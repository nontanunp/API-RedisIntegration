using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace API_RedisIntegration.Controllers
{

    [ApiController]
    [Route("api/[action]")]
    public class AreasController : ControllerBase
    {
        [HttpPost]
        //public async Task<IActionResult> Areas(Areas req)
        public async Task<IActionResult> Areas(List<Areas> req)
        {
            AreasResponse result = new AreasResponse();
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
                    db.StringSet("Areas", jsonsAreas);
                    
                    result.ErrorCode = 200;
                    result.ErrorDesc = "Area data add success.";
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
