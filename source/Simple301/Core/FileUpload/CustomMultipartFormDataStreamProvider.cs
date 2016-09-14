using System.Net.Http;

namespace Simple301.Core.FileUpload
{
    /// <summary>
    /// Overrides MultipartFormDataStreamProvider so we can derive the original filename fro,m the upload
    /// See http://www.strathweb.com/2012/08/a-guide-to-asynchronous-file-uploads-in-asp-net-web-api-rtm/
    /// </summary>
    public class CustomMultipartFormDataStreamProvider : MultipartFormDataStreamProvider
    {
        public CustomMultipartFormDataStreamProvider(string path) : base(path)
        { }

        public override string GetLocalFileName(System.Net.Http.Headers.HttpContentHeaders headers)
        {
            var name = !string.IsNullOrWhiteSpace(headers.ContentDisposition.FileName) ? headers.ContentDisposition.FileName : "NoName";
            return name.Replace("\"", string.Empty); //this is here because Chrome submits files in quotation marks which get treated as part of the filename and get escaped
        }
    }
}
