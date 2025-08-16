namespace API_RedisIntegration
{
    public class Trucks
    {
        public string TruckID { get; set; }
        public Dictionary<string, int> AvailableResources { get; set; }
        public Dictionary<string, int> TravelTimeToArea { get; set; }
     
    }

    public class TrucksResponse
    {

        public int ErrorCode { get; set; }
        public string ErrorDesc { get; set; }

    }
}
