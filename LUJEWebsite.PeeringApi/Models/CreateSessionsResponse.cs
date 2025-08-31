namespace LUJEWebsite.PeeringApi.Models
{
    public class CreateSessionsResponse
    {
        public List<Session>? Sessions { get; set; }

        public Error? Errors { get; set; }
    }
}
