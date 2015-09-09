using Umbraco.Web.Routing;
using System.Linq;

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
            //Look for a redirect that matches the path + query
            var path = request.Uri.PathAndQuery;
            var matchedRedirect = RedirectRepository.GetAll().FirstOrDefault(x => x.OldUrl.Equals(path));
            if (matchedRedirect == null) return false;

            //Found one, set the 301 redirect on the request and return
            request.SetRedirectPermanent(matchedRedirect.NewUrl);
            return true;
        }
    }
}
