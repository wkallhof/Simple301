using System.Collections.Generic;

namespace Simple301.Core.Models
{
    public class ImportRedirectsResponse
    {
        public IEnumerable<AddRedirectResponse> FailedRedirects { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
