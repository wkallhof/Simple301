using Simple301.Core.FileUpload;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Umbraco.Web.Mvc;
using Umbraco.Web.WebApi;

namespace Simple301.Core
{
    // If you want this endpoint to only be accessible when the user is logged in, 
    // then use UmbracoAuthorizedApiController instead of UmbracoApiController
    [PluginController("Simple301")]
    public class FileUploadApiController : UmbracoAuthorizedApiController
    {
        public async Task<HttpResponseMessage> UploadFileToServer()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            // Make this directory whatever makes sense for your project.
            var root = HttpContext.Current.Server.MapPath("~/App_Data/Temp/Simple301");
            Directory.CreateDirectory(root);
            var provider = new CustomMultipartFormDataStreamProvider(root);
            var result = await Request.Content.ReadAsMultipartAsync(provider);

            if (result.FileData.FirstOrDefault() == null)
                return Request.CreateResponse(HttpStatusCode.InternalServerError, "Error uploading file");

            // Return the full file path to the response
            return Request.CreateResponse(HttpStatusCode.OK, result.FileData.FirstOrDefault().LocalFileName);
        }
    }
}
