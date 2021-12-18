using System;
using System.Collections.Generic;
using System.Text;

namespace VirtoCommerce.AlgoliaSearchModule.Data
{
    public class AlgoliaIndexDocument : Dictionary<string, object>
    {
        public string ObjectID {get;set;}

    }
}
