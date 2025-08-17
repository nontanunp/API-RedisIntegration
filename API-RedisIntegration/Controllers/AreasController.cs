using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StackExchange.Redis;
using System.Collections.Generic;

namespace API_RedisIntegration.Controllers
{

    [ApiController]
    [Route("api/[action]")]
    public class AreasController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AreasController(IConfiguration configuration)
        {
            _configuration = configuration;

        }
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
                    string connStr = _configuration["ConnectionStrings:ConnectionStringsRedis"];
                    ConnectionMultiplexer con = ConnectionMultiplexer.Connect(connStr);

                    IDatabase db = con.GetDatabase();
                    var jsonsAreas = JsonConvert.SerializeObject(req, Formatting.None);


                    // เช็ค jsonsAreas มี AreaID ซ้ำกันหรือไม่ก่อน
                    var areaIds = req.Select(a => a.AreaID).ToList();
                    if (areaIds.Distinct().Count() != areaIds.Count)
                    {
                        result.ErrorCode = 401;
                        result.ErrorDesc = "AreaID not unique, please check again.";
                        return Ok(result);
                    }

                    // 
                    var urgency = req.Select(b => b.UrgencyLevel).ToList();
                    if (urgency.Any(u => u < 1 || u > 5))
                    {
                        result.ErrorCode = 401;
                        result.ErrorDesc = "UrgencyLevel Invalid (1-5) , please check again";
                        return Ok(result);
                    }

                    db.StringSet("Areas", jsonsAreas);

                    result.ErrorCode = 200;
                    result.ErrorDesc = "Area data add success.";

                    //ต้องเช็คใน redis ว่ามีข้อมูล Areas ที่ซ้ำกัน อยู่หรือไม่
                    ////var data = db.StringGet("Areas");

                    ////if (!data.IsNullOrEmpty)
                    ////{
                    ////    //ถ้ามีข้อมูลอยู่แล้ว ให้แปลงเป็น List<Areas> เพื่อเช็คซ้ำ
                    ////    var existingAreas = JsonConvert.DeserializeObject<List<Areas>>(data);
                    ////    foreach (var area in req)
                    ////    {
                    ////        if (existingAreas.Any(a => a.AreaID == area.AreaID)) // ใน redis => หาจาก req
                    ////        {
                    ////            result.ErrorCode = 409; // Dup
                    ////            result.ErrorDesc = "Area with ID " + area.AreaID + "already exists.";
                    ////            break;
                    ////        }
                    ////    }
                    ////}
                    ////if (result.ErrorCode != 409)
                    ////{
                    ////    db.StringSet("Areas", jsonsAreas);

                    ////    result.ErrorCode = 200;
                    ////    result.ErrorDesc = "Area data add success.";
                    ////}
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
        public async Task<IActionResult> GetAreasList()
        {

            try
            {
                string connStr = _configuration["ConnectionStrings:ConnectionStringsRedis"];
                var con = ConnectionMultiplexer.Connect(connStr);
                IDatabase db = con.GetDatabase();
                string value = db.StringGet("Areas");

                List<Areas> json = string.IsNullOrEmpty(value) ? new List<Areas>() : JsonConvert.DeserializeObject<List<Areas>>(value); 
                return Ok(json);
            }
            catch (Exception ex)
            {
                return Ok(ex.Message);
            }
        }
    }
}
