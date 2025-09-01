using Azure;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Newtonsoft.Json;
using Pipelines.Sockets.Unofficial.Arenas;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace API_RedisIntegration.Controllers
{


    [ApiController]
    [Route("api/[action]")]
    public class AssignmentsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public AssignmentsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        [HttpDelete]
        public async Task<IActionResult> DeleteAssignments()
        {
            AssignmentsResponse result = new AssignmentsResponse();
            try
            {
                string connStr = Environment.GetEnvironmentVariable("CONNECTIONSTRINGSREDIS");
                string host = Environment.GetEnvironmentVariable("HOST");
                int port = int.Parse(Environment.GetEnvironmentVariable("PORT"));

                var con = ConnectionMultiplexer.Connect(connStr);
                IDatabase db = con.GetDatabase();
                //var server = con.GetServer(host, port);

                db.KeyDelete("ANS_INDEX");

                /***** แบบเดิมใช้ pattern: "ans*" วนลบเอา*******
                //foreach (var key in server.Keys(pattern: "ans*"))
                //{
                //    db.KeyDelete(key);
                //}
                *********************************************/
                result.ErrorCode = 200;
                result.ErrorDesc = "All assignments deleted successfully.";
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.ErrorDesc = "Internal server error: " + ex.Message;
            }
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignments()
        {

            AssignmentsResponse result = new AssignmentsResponse();
            try
            {
                var connStr = Environment.GetEnvironmentVariable("CONNECTIONSTRINGSREDIS");
                var host = Environment.GetEnvironmentVariable("HOST");
                int port = int.Parse(Environment.GetEnvironmentVariable("PORT"));

                var con = ConnectionMultiplexer.Connect(connStr);
                IDatabase db = con.GetDatabase();
                var keys = db.SetMembers("ANS_INDEX");

                if (keys.Count() == 0)
                {
                    result.ErrorCode = 404;
                    result.ErrorDesc = "No assignments found.";
                    return Ok(result);
                }

                List<string> jsonANS = new List<string>();
                foreach (var key in keys)
                {
                    jsonANS.Add(key.ToString());
                }

                jsonANS = jsonANS.OrderBy(j => j.Substring(3)).ToList();
                List<AssignmentsDataResponse> assignmentsList = new List<AssignmentsDataResponse>();
                foreach (var item in jsonANS)
                {
                    AssignmentsDataResponse assignment = JsonConvert.DeserializeObject<AssignmentsDataResponse>(item);
                    assignmentsList.Add(assignment);

                }
                /**************  แบบเดิมใช้ pattern: "ans*" ในการหา ****************
                var keys = con.GetServer(host, port).Keys(pattern: "ans*").ToList();

                //var keys = server.Keys(pattern: "ans*").ToList();

                keys = keys.OrderBy(k => k.ToString().Substring(3)).ToList();
                if (keys.Count == 0)
                {
                    result.ErrorCode = 404;
                    result.ErrorDesc = "No assignments found.";
                    return Ok(result);
                }
                List<AssignmentsDataResponse> assignmentsList = new List<AssignmentsDataResponse>();
                foreach (var key in keys)
                {
                    string jsonData = db.StringGet(key);
                    if (!string.IsNullOrEmpty(jsonData))
                    {
                        AssignmentsDataResponse assignment = JsonConvert.DeserializeObject<AssignmentsDataResponse>(jsonData);
                        assignmentsList.Add(assignment);
                    }
                }
                ***************************************************************/
                return Ok(assignmentsList);
            }
            catch (Exception ex)
            {
                result.ErrorCode = 500;
                result.ErrorDesc = "Internal server error: " + ex.Message;
                return Ok(result);
            }
        }


        [HttpPost]
        public async Task<IActionResult> Assignments()
        {
            AssignmentsDataResponse responseData = new AssignmentsDataResponse();
            AssignmentsResponse response = new AssignmentsResponse();
            int reuslt = int.MinValue;

            string connStr = Environment.GetEnvironmentVariable("CONNECTIONSTRINGSREDIS");
            string host = Environment.GetEnvironmentVariable("HOST");
            int port = int.Parse(Environment.GetEnvironmentVariable("PORT"));



            ConnectionMultiplexer con = ConnectionMultiplexer.Connect(connStr);
            IDatabase db = con.GetDatabase();

            List<string> totalTruckisMathList = new List<string>();


            try
            {
                string jsonAreas = db.StringGet("Areas").ToString();
                string jsonTruck = db.StringGet("Trucks").ToString();



                if (String.IsNullOrEmpty(jsonAreas) || String.IsNullOrEmpty(jsonTruck))
                {
                    response.ErrorCode = 404;
                    response.ErrorDesc = "No areas or truck found in Redis.";
                }
                else
                {
                    List<Areas> areas = JsonConvert.DeserializeObject<List<Areas>>(jsonAreas);
                    List<Trucks> truck = JsonConvert.DeserializeObject<List<Trucks>>(jsonTruck);


                    List<Areas> areasOrder = areas.OrderByDescending(o => o.UrgencyLevel).ToList();

                    string _truckID = String.Empty;
                    string _areaID = String.Empty;
                    int total = int.MinValue;


                    //พื้นที่เก็บข้อมูลทรัพยากรที่ต้องการ 
                    for (int i = 0; i < areasOrder.Count; i++)
                    {
                        totalTruckisMathList = new List<string>();

                        _areaID = areasOrder[i].AreaID;
                        Dictionary<string, int> requiredResources = new Dictionary<string, int>();
                        requiredResources = areasOrder[i].RequiredResources; // มีอะไรบ้าง
                        total = areasOrder[i].RequiredResources.Count; // จำนวนของที่มี food water

                        List<string> RequiredResourcesKey = new List<string>();
                        List<string> RequiredResourcesValuse = new List<string>();

                        // เช็คของแต่ละพื้นที่
                        foreach (var itemRequiredResources in areasOrder[i].RequiredResources)
                        {
                            RequiredResourcesKey.Add(itemRequiredResources.Key);
                            RequiredResourcesValuse.Add(itemRequiredResources.Value.ToString());
                        }

                        //จำนวนของที่ต้องการทั้งหมด
                        int _totalRequiredResources = RequiredResourcesKey.Count;

                        // เช็คของในรถบรรทุก แต่ละคัน
                        for (int j = 0; j < truck.Count; j++)
                        {
                            _truckID = truck[j].TruckID;

                            bool hasAll = RequiredResourcesKey.All(k => truck[j].AvailableResources.ContainsKey(k));

                            if (hasAll)
                            {
                                // ถ้ามี ทั้งหมด ให้เช็คจำนวนของว่าพอหรือไม่
                                bool hasEnough = true;
                                for (int k = 0; k < _totalRequiredResources; k++)
                                {
                                    string key = RequiredResourcesKey[k];
                                    int requiredValue = int.Parse(RequiredResourcesValuse[k]);
                                    if (truck[j].AvailableResources[key] < requiredValue)
                                    {
                                        // ถ้าไม่พอ ให้เปลี่ยน hasEnough เป็น false
                                        hasEnough = false;
                                        break;
                                    }
                                }

                                // ถ้าเข้าเงื่อนไขว่ามีของทั้งหมดและมีจำนวนของที่ต้องการ
                                if (hasEnough)
                                {
                                    // เช็คเวลาที่ที่พื้นที่ต้องการรับของ
                                    int timeConstraint = areasOrder[i].TimeConstraint;

                                    // เช็คว่ารถคันนี้มีเข้าไปยังพื้นที่นี้หรือไม่
                                    if (truck[j].TravelTimeToArea.ContainsKey(_areaID))
                                    {
                                        // ถ้ามีเวลาเดินทางไปยังพื้นที่นี้ ให้เช็คว่าเวลาที่ใช้เดินทางน้อยกว่าหรือเท่ากับเวลาที่พื้นที่ต้องการหรือไม่
                                        int travelTime = truck[j].TravelTimeToArea[_areaID];
                                        if (travelTime <= timeConstraint)
                                        {
                                            // ถ้ามีรถหลายคันที่ตรงตามเงื่อนไข เก็บข้อมูลไว้ใน List
                                            TruckisMathList info = new TruckisMathList();
                                            info.TruckID = _truckID;
                                            info.TravelTime = travelTime;

                                            string jsonInfo = JsonConvert.SerializeObject(info, Formatting.None);
                                            totalTruckisMathList.Add(jsonInfo);
                                        }
                                    }
                                } //ถ้าไม่เข้าเงื่อนไข้ก็ไม่มีอะไรไม่บันทึกลง totalTruckisMathList 
                            }
                        }
                        // หลังจากเช็คทุกคันรถแล้ว ถ้ามีรถที่ตรงตามเงื่อนไข
                        string ErrorDesc = String.Empty;
                        if (totalTruckisMathList.Count > 0)
                        {
                            // ถ้ามีเลือกรถที่จำนวนการเดินทางน้อยที่สุด
                            TruckisMathList truckisMathListOrderBy = new TruckisMathList();
                            truckisMathListOrderBy = totalTruckisMathList.Select(x => JsonConvert.DeserializeObject<TruckisMathList>(x)).OrderBy(o => o.TravelTime).FirstOrDefault();
                            _truckID = truckisMathListOrderBy.TruckID;
                            ErrorDesc = "Assignment created successfully for area: " + _areaID + " with truck: " + _truckID;
                        }
                        else // ถ้าไม่พบรถที่ตรงตามเงื่อนไข
                        {
                            _truckID = "Not found truck resources for area: " + _areaID;
                            ErrorDesc = "Not enough resources available for area: " + _areaID;
                        }
                        //////set ค่าไว้  บันทึกข้อมูลลง Redis
                        responseData.AreaID = _areaID;
                        responseData.TruckID = _truckID;
                        responseData.ResourcesDelivered = requiredResources;
                        string answer = JsonConvert.SerializeObject(responseData, Formatting.None);

                        db.SetAdd("ANS_INDEX", answer);
                        db.KeyExpire("ANS_INDEX", TimeSpan.FromSeconds(1800)); // 30 นาที

                        reuslt = 0;
                        response.ErrorCode = 200;
                        response.ErrorDesc = ErrorDesc;

                        /*********** แบบเดิม sonic *********************
                        //////////// หลังจากเช็คทุกคันรถแล้ว ถ้ามีรถที่ตรงตามเงื่อนไข
                        //////////if (totalTruckisMathList.Count > 0)
                        //////////{
                        //////////    // ถ้ามีเลือกรถที่จำนวนการเดินทางน้อยที่สุด
                        //////////    TruckisMathList truckisMathListOrderBy = new TruckisMathList();
                        //////////    truckisMathListOrderBy = totalTruckisMathList.Select(x => JsonConvert.DeserializeObject<TruckisMathList>(x)).OrderBy(o => o.TravelTime).FirstOrDefault();
                        //////////    _truckID = truckisMathListOrderBy.TruckID;


                        //////////    // set ค่าไว้  บันทึกข้อมูลลง Redis
                        //////////    responseData.AreaID = _areaID;
                        //////////    responseData.TruckID = _truckID;
                        //////////    responseData.ResourcesDelivered = requiredResources;
                        //////////    string answer = JsonConvert.SerializeObject(responseData, Formatting.None);

                        //////////    //db.StringSet("ans" + i, answer, expiry);

                        //////////    db.SetAdd("ANS_INDEX", answer);
                        //////////    db.KeyExpire("ANS_INDEX", TimeSpan.FromSeconds(1800)); // 30 นาที


                        //////////    reuslt = 0;
                        //////////    response.ErrorCode = 200;
                        //////////    response.ErrorDesc = "Assignment created successfully for area: " + _areaID + " with truck: " + _truckID;

                        //////////}
                        //////////else // ถ้าไม่พบรถที่ตรงตามเงื่อนไข
                        //////////{

                        //////////    // set ค่าไว้  บันทึกข้อมูลลง Redis
                        //////////    responseData.AreaID = _areaID;
                        //////////    responseData.TruckID = "Not found truck resources for area: " + _areaID;
                        //////////    responseData.ResourcesDelivered = requiredResources;
                        //////////    string answer = JsonConvert.SerializeObject(responseData, Formatting.None);

                        //////////    // db.StringSet("ans" + i, answer, expiry);

                        //////////    db.SetAdd("ANS_INDEX", answer);
                        //////////    db.KeyExpire("ANS_INDEX", TimeSpan.FromSeconds(1800)); // 30 นาที



                        //////////    reuslt = 0;
                        //////////    response.ErrorCode = 200;
                        //////////    response.ErrorDesc = "Not enough resources available for area: " + _areaID;

                        //////////}
                        ************************************************/
                    }

                    if (reuslt == 0)
                    {
                        // ดึงข้อมูลทั้งหมดที่บันทึกไว้ใน Redis

                        var keys = db.SetMembers("ANS_INDEX");

                        if (keys.Count() == 0)
                        {
                            response.ErrorCode = 404;
                            response.ErrorDesc = "No assignments found.";
                            return Ok(response);
                        }

                        List<string> jsonANS = new List<string>();
                        foreach (var key in keys)
                        {
                            jsonANS.Add(key.ToString());
                        }

                        jsonANS = jsonANS.OrderBy(j => j.Substring(3)).ToList();
                        List<AssignmentsDataResponse> assignmentsList = new List<AssignmentsDataResponse>();
                        foreach (var item in jsonANS)
                        {
                            AssignmentsDataResponse assignment = JsonConvert.DeserializeObject<AssignmentsDataResponse>(item);
                            assignmentsList.Add(assignment);

                        }

                        /**************  แบบเดิมใช้ pattern: "ans*" ในการหา ****************

                        //var server = con.GetServer(host, port);
                        //var keys = server.Keys(pattern: "ans*").ToList();

                        //if (keys.Count == 0)
                        //{
                        //    response.ErrorCode = 404;
                        //    response.ErrorDesc = "No assignments found.";
                        //    return Ok(response);
                        //}

                        // สร้าง List สำหรับเก็บข้อมูล AssignmentsDataResponse เรียงตามลำดับ
                        //List<AssignmentsDataResponse> assignmentsList = new List<AssignmentsDataResponse>();
                        //keys = keys.OrderBy(k => k.ToString().Substring(3)).ToList();

                        //foreach (var key in keys)
                        //{
                        //    string jsonData = db.StringGet(key);
                        //    if (!string.IsNullOrEmpty(jsonData))
                        //    {
                        //        AssignmentsDataResponse assignment = JsonConvert.DeserializeObject<AssignmentsDataResponse>(jsonData);
                        //        assignmentsList.Add(assignment);
                        //    }
                        //}
                        /***************************************************************/
                        return Ok(assignmentsList);
                    }
                }
            }
            catch (Exception ex)
            {
                response.ErrorCode = 500;
                response.ErrorDesc = "Internal server error: " + ex.Message;
            }
            return Ok(response);
        }



        //[HttpPost]
        //public async Task<IActionResult> AssignmentsTestV1()
        //{
        //    AssignmentsDataResponse responseData = new AssignmentsDataResponse();
        //    AssignmentsResponse response = new AssignmentsResponse();
        //    int reuslt = int.MinValue;

        //    ConnectionMultiplexer con = ConnectionMultiplexer.Connect("localhost:6379");
        //    IDatabase db = con.GetDatabase();



        //    try
        //    {
        //        //if (req == null)
        //        //{
        //        //    return BadRequest("Invalid request data.");
        //        //}



        //        //Areas areas = new Areas();

        //        // step get areas and trucks from Redis
        //        string jsonAreas = db.StringGet("Areas");
        //        string jsonTruck = db.StringGet("Trucks");



        //        if (String.IsNullOrEmpty(jsonAreas) || String.IsNullOrEmpty(jsonTruck))
        //        {
        //            response.ErrorCode = 404;
        //            response.ErrorDesc = "No areas or truck found in Redis.";
        //        }
        //        else
        //        {
        //            List<Areas> areas = JsonConvert.DeserializeObject<List<Areas>>(jsonAreas);
        //            List<Trucks> truck = JsonConvert.DeserializeObject<List<Trucks>>(jsonTruck);


        //            //Areas[] areas = JsonConvert.DeserializeObject<Areas[]>(jsonAreas);
        //            //Trucks[] truck = JsonConvert.DeserializeObject<Trucks[]>(jsonTruck);





        //            //List<Areas> areasOrder = areas.OrderBy(o => o.UrgencyLevel).ToList();
        //            List<Areas> areasOrder = areas.OrderByDescending(o => o.UrgencyLevel).ToList();

        //            // Replace this line:
        //            // Dictionary keyValuePairs = new Dictionary<string, int>();

        //            // With this line:
        //            Dictionary<string, int> key = new Dictionary<string, int>();

        //            //keyValuePairs = areasOrder[0].RequiredResources;

        //            //foreach (var area in areasOrder)
        //            //{

        //            //    keyValuePairs = areasOrder.RequiredResources;

        //            //}


        //            string _truckID = String.Empty;
        //            string _areaID = String.Empty;
        //            int total = int.MinValue;


        //            //พื้นที่เก็บข้อมูลทรัพยากรที่ต้องการ 
        //            for (int i = 0; i < areasOrder.Count; i++)
        //            {
        //                //key = areasOrder[i].RequiredResources;

        //                //var keys = areasOrder[i].RequiredResources.Keys;
        //                //var values = areasOrder[i].RequiredResources.Values;

        //                _areaID = areasOrder[i].AreaID;
        //                Dictionary<string, int> requiredResources = new Dictionary<string, int>();
        //                requiredResources = areasOrder[i].RequiredResources;
        //                total = areasOrder[i].RequiredResources.Count;
        //                int countCheck = 0;
        //                List<string> totalTruckisMathList = new List<string>();


        //                // ของแต่ละพื้นที่
        //                foreach (var itemRequiredResources in areasOrder[i].RequiredResources)
        //                {

        //                    string _keyRequiredResources = itemRequiredResources.Key;
        //                    int _valuesRequiredResources = itemRequiredResources.Value;

        //                    //เช็คของในรถบรรทุก แต่ละคัน

        //                    for (int j = 0; j < truck.Count; j++)
        //                    {
        //                        _truckID = truck[j].TruckID;
        //                        //var itemAvailableResources = truck[j].AvailableResources;
        //                        // เช็คว่ามีของที่ต้องการหรือไม่
        //                        if (truck[j].AvailableResources.ContainsKey(_keyRequiredResources))
        //                        {
        //                            int _valuesAvailableResources = truck[j].AvailableResources[_keyRequiredResources];
        //                            // เช็คว่ามีของเพียงพอหรือไม่
        //                            if (_valuesAvailableResources >= _valuesRequiredResources)
        //                            {

        //                                // เช็คเวลาที่ที่พื้นที่ต้องการรับของ
        //                                int timeConstraint = areasOrder[i].TimeConstraint;

        //                                // เช็คว่ารถคันนี้มีเข้าไปยังพื้นที่นี้หรือไม่


        //                                if (truck[j].TravelTimeToArea.ContainsKey(_areaID))
        //                                {
        //                                    // ถ้ามีเวลาเดินทางไปยังพื้นที่นี้ ให้เช็คว่าเวลาที่ใช้เดินทางน้อยกว่าหรือเท่ากับเวลาที่พื้นที่ต้องการหรือไม่
        //                                    int travelTime = truck[j].TravelTimeToArea[_areaID];
        //                                    if (travelTime <= timeConstraint)
        //                                    {
        //                                        TruckisMathList info = new TruckisMathList();
        //                                        info.TruckID = _truckID;
        //                                        info.TravelTime = travelTime;

        //                                        string jsonInfo = JsonConvert.SerializeObject(info, Formatting.None);

        //                                        totalTruckisMathList.Add(jsonInfo);

        //                                        // ถ้าตรง
        //                                        //countCheck += 1;
        //                                        //break;

        //                                    }
        //                                    else
        //                                    {
        //                                        // ถ้าไม่ตรง ค่อยไปหาวนรถคันอื่น
        //                                    }
        //                                }
        //                            }
        //                            else
        //                            {
        //                                break;
        //                            }
        //                        }
        //                    }

        //                    if (totalTruckisMathList.Count > 0)
        //                    {
        //                        countCheck += 1;
        //                        TruckisMathList truckisMathListOrderBy = new TruckisMathList();
        //                        truckisMathListOrderBy = totalTruckisMathList.Select(x => JsonConvert.DeserializeObject<TruckisMathList>(x)).OrderBy(o => o.TravelTime).FirstOrDefault();
        //                        _truckID = truckisMathListOrderBy.TruckID;

        //                    }

        //                    //foreach (var itemAvailableResources in truck[i].AvailableResources)
        //                    //{
        //                    //    string _truckID = truck[i].TruckID;
        //                    //    string _keyAvailableResources = itemAvailableResources.Key;
        //                    //    int _valuesAvailableResources = itemAvailableResources.Value;


        //                    //    if (_keyRequiredResources.Contains(_keyAvailableResources))
        //                    //    {
        //                    //        if (_valuesAvailableResources >= _valuesRequiredResources)
        //                    //        {
        //                    //            countCheck += 1;
        //                    //        }
        //                    //    }
        //                    //}
        //                }
        //                if (total == countCheck)
        //                {




        //                    responseData.AreaID = _areaID;
        //                    responseData.TruckID = _truckID;
        //                    responseData.ResourcesDelivered = requiredResources;
        //                    string answer = JsonConvert.SerializeObject(responseData, Formatting.None);

        //                    //TimeSpan expiry = TimeSpan.FromSeconds(60);
        //                    //db.StringSet("ans" + i, answer, expiry);
        //                    db.StringSet("ans" + i, answer);

        //                    //return Ok(responseData);
        //                    // แสดงว่ามีครบ
        //                    reuslt = 0;
        //                    response.ErrorCode = 200;
        //                    response.ErrorDesc = "Assignment created successfully for area: " + _areaID + " with truck: " + _truckID;
        //                }
        //                else
        //                {
        //                    responseData.AreaID = _areaID;
        //                    responseData.TruckID = "not found truck resources for area: " + _areaID;
        //                    responseData.ResourcesDelivered = requiredResources;
        //                    string answer = JsonConvert.SerializeObject(responseData, Formatting.None);

        //                    db.StringSet("ans" + i, answer);

        //                    reuslt = 0;
        //                    response.ErrorCode = 200;
        //                    response.ErrorDesc = "Not enough resources available for area: " + _areaID;
        //                    //return NotFound("Not enough resources available for area: " + areaID);

        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        response.ErrorCode = 500;
        //        response.ErrorDesc = "Internal server error: " + ex.Message;
        //    }

        //    if (reuslt == 0)
        //    {

        //        var server = con.GetServer("localhost", 6379);
        //        var keys = server.Keys(pattern: "ans*").ToList();
        //        if (keys.Count == 0)
        //        {
        //            return NotFound(new { message = "No assignments found." });
        //        }
        //        List<AssignmentsDataResponse> assignmentsList = new List<AssignmentsDataResponse>();


        //        keys = keys.OrderBy(k => k.ToString().Substring(3)).ToList();


        //        foreach (var key in keys)
        //        {
        //            string jsonData = db.StringGet(key);
        //            if (!string.IsNullOrEmpty(jsonData))
        //            {
        //                AssignmentsDataResponse assignment = JsonConvert.DeserializeObject<AssignmentsDataResponse>(jsonData);
        //                assignmentsList.Add(assignment);
        //            }
        //        }
        //        return Ok(assignmentsList);
        //    }

        //    return Ok(response);
        //}


    }
}
