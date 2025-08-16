namespace API_RedisIntegration
{
    public class AssignmentsDataResponse
    {
        public string? AreaID { get; set; }
        public string? TruckID { get; set; }

        public Dictionary<string, int> ResourcesDelivered { get; set; }
    }
    public class AssignmentsResponse
    {
        public int ErrorCode { get; set; }
        public string? ErrorDesc { get; set; }

    }

    public class TruckisMathList
    {
        public string? TruckID { get; set; }
        public int TravelTime { get; set; }

    }
}
