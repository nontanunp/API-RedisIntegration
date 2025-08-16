namespace API_RedisIntegration
{
    public class Areas
    {
        public string? AreaID { get; set; }
        public int UrgencyLevel { get; set; }
        public Dictionary<string, int> RequiredResources { get; set; }

        public int TimeConstraint { get; set; }

    }

    public class AreasResponse
    {
        public int ErrorCode { get; set; }
        public string? ErrorDesc { get; set; }

    }
}
