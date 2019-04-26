﻿using System;
using System.Text.RegularExpressions;
using Umbraco.Web.Routing;

namespace Simple301.Core
{
    /// <summary>
    /// Content finder to be injected at the start of the Umbraco pipeline that first
    /// looks for any redirects that path the path + query
    /// </summary>
    public class RedirectContentFinder : IContentFinder
    {
        public bool TryFindContent(PublishedContentRequest request)
        {
            //Get the requested URL path + query
            var path = request.Uri.PathAndQuery.ToLower();

            //Check the table
            var matchedRedirect = RedirectRepository.FindRedirect(path);
            if (matchedRedirect == null) return false;

            // Groups match replace
            string newUrl = matchedRedirect.NewUrl;

            if (matchedRedirect.IsRegex && matchedRedirect.OldUrl.Contains("(.*)"))
            {
                try
                {
                    var match = Regex.Match(path, matchedRedirect.OldUrl);

                    for (int iGrp = 1; iGrp < match.Groups.Count; iGrp++)
                    {
                        newUrl = newUrl.Replace($"${iGrp}", match.Groups[iGrp].Value);
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

            //Found one, set the 301 redirect on the request and return
            request.SetRedirectPermanent(newUrl);
            return true;
        }
    }
}
