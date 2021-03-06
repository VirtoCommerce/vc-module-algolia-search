# VirtoCommerce.AlgoliaSearch - Preview

THIS A PREVIEW VERSION - NOT TESTED IN PRODUCTION

VirtoCommerce.AlgoliaSearch module implements ISearchProvider defined in the VirtoCommerce.Core module and uses Algolia search cloud service <a href="https://algolia.com" target="_blank">Algolia Search</a>.

# Version History
## 1.0.0
* Initial release

# Installation
Installing the module:
* Automatically: in VC Manager go to **Modules > Available**, select the **Elasticsearch module** and click **Install**.
* Manually: download module ZIP package from https://github.com/VirtoCommerce/vc-module-algolia-search/releases. In VC Manager go to **Modules > Advanced**, upload module package and click **Install**.

# Configuration

## Configuration
Azure Search provider are configurable by these configuration keys:

* **Search.Provider** is the name of the search provider and must be **AlgoliaSearch**
* **Search.AlgoliaSearch.ApiId** is a api id for algolia server.
* **Search.AlgoliaSearch.ApiKey** is a api key for either algolia server.

```
"AlgoliaSearch": {
    "AppId": "API_ID",
    "ApiKey": "API_KEY"
}
```

# License
Copyright (c) Virto Solutions LTD. All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
