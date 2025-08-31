namespace LUJEWebsite.PeeringApi.Models
{
    public class ListLocationsResponse
    {
        public string? NextToken { get; set; }

        public List<Location>? Locations { get; set; }
    }
}
