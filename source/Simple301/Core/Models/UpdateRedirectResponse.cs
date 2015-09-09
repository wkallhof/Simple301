namespace Simple301.Core.Models
{
    public class UpdateRedirectResponse
    {
        public Redirect UpdatedRedirect { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
