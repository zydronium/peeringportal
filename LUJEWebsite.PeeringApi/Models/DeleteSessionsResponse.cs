namespace LUJEWebsite.PeeringApi.Models
{
    public class DeleteSessionsResponse
    {
        public List<Session>? Sessions { get; set; }

        public Error? Errors { get; set; }
    }
}
