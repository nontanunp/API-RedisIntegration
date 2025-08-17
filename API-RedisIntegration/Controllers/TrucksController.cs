using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace API_RedisIntegration.Controllers
{

    [ApiController]
    [Route("api/[action]")]
    public class TrucksController : ControllerBase
    {

        private readonly IConfiguration _configuration;
        public TrucksController(IConfiguration configuration)
        {
            _configuration = configuration;

        }

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
                    string connStr = _configuration["ConnectionStrings:ConnectionStringsRedis"];
                    ConnectionMultiplexer con = ConnectionMultiplexer.Connect(connStr);

                    IDatabase db = con.GetDatabase();
                    var jsonsTruck = JsonConvert.SerializeObject(req, Formatting.None);

                    // เช็ค jsonsTruck มี TruckID ซ้ำกันหรือไม่ก่อน
                    var truckID = req.Select(a => a.TruckID).ToList();
                    if (truckID.Distinct().Count() != truckID.Count)
                    {
                        result.ErrorCode = 401;
                        result.ErrorDesc = "TruckID not unique, please check again.";
                        return Ok(result);
                    }

                    db.StringSet("Trucks", jsonsTruck);

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


        [HttpGet]
        public async Task<IActionResult> GetTrucksList()
        {

            try
            {
                string connStr = _configuration["ConnectionStrings:ConnectionStringsRedis"];
                var con = ConnectionMultiplexer.Connect(connStr);
                IDatabase db = con.GetDatabase();
                string value = db.StringGet("Trucks");

                List<Trucks> json = string.IsNullOrEmpty(value) ? new List<Trucks>() : JsonConvert.DeserializeObject<List<Trucks>>(value);
                return Ok(json);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }
    }
}
