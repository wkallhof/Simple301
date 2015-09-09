namespace Simple301.Core.Models
{
    public class AddRedirectResponse
    {
        public Redirect NewRedirect { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
