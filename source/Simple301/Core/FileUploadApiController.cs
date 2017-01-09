using Simple301.Core.FileUpload;
using Simple301.Core.Models;
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

            // Get full file path from disk
            var filename = result.FileData.FirstOrDefault().LocalFileName;

            // Process the file for redirects
            var importResponse = ProcessFile(filename);

            // Return the feedback to the response
            return Request.CreateResponse(HttpStatusCode.OK, importResponse);
        }

        private ImportRedirectsResponse ProcessFile(string filename)
        {
            var failedRedirects = new List<AddRedirectResponse>();
            var successfulRedirects = new List<AddRedirectResponse>();

            StreamReader sr = new StreamReader(filename);

            do
            {
                string row = sr.ReadLine();

                // Attempt to add each redirect in turn, build up an object to return to the response
                var redirect = AddRedirectFromFile(row);
                if (redirect.Success)
                {
                    successfulRedirects.Add(redirect);
                }
                else
                {
                    failedRedirects.Add(redirect);
                }

            } while (sr.Peek() != -1);

            // Tidy Streameader up
            sr.Close();
            sr.Dispose();

            // Rebuild redirects dictionary
            //if (successfulRedirects.Count > 0)
            //{
            //    RedirectRepository.ReloadRedirects();
            //}

            return new ImportRedirectsResponse
            {
                Success = true,
                Message = $"{successfulRedirects.Count} records imported.  Failed to import {failedRedirects.Count} records.",
                FailedRedirects = failedRedirects
            };
        }

        private AddRedirectResponse AddRedirectFromFile(string row)
        {
            var cells = row.Split(',');

            var oldUrl = cells[0];
            var newUrl = cells[1];
            var notes = cells.Length > 2 ? cells[2] : null;

            if (oldUrl.ToLower().Replace(" ", "") == "oldurl" || newUrl.ToLower().Replace(" ", "") == "newurl")
            {
                // Skip header row
                return new AddRedirectResponse { Success = false, Message = "Skipped header row from CSV file" };
            }

            return RedirectRepository.AddRedirectFromCsv(oldUrl, newUrl, notes);
        }

    }
}
