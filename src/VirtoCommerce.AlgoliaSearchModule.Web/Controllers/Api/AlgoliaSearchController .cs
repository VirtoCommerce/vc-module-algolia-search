using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtoCommerce.AlgoliaSearchModule.Core;

namespace VirtoCommerce.ElasticAppSearch.Web.Controllers.Api
{
    [Route("api/algolia-search")]
    public class AlgoliaSearchController : Controller
    {

        [HttpGet]
        [Route("redirect")]
        [Authorize(ModuleConstants.Security.Permissions.Access)]
        public ActionResult Redirect()
        {
            return Redirect("https://dashboard.algolia.com");
        }

    }
}
