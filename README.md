# VirtoCommerce.AlgoliaSearch

VirtoCommerce.AlgoliaSearch module implements ISearchProvider defined in the VirtoCommerce.Core module and uses Elasticsearch engine which stores indexed documents on a standalone <a href="https://www.elastic.co/products/elasticsearch" target="_blank">Elasticsearch</a> or <a href="https://cloud.elastic.co" target="_blank">Elastic Cloud</a> server.

# Version History
## 1.0.0
* Initial release

# Installation
Installing the module:
* Automatically: in VC Manager go to **Modules > Available**, select the **Elasticsearch module** and click **Install**.
* Manually: download module ZIP package from https://github.com/VirtoCommerce/vc-module-algolia-search/releases. In VC Manager go to **Modules > Advanced**, upload module package and click **Install**.

# Configuration
## VirtoCommerce.Search.SearchConnectionString
The search configuration string is a text string consisting of name/value pairs seaprated by semicolon (;). Name and value are separated by equal sign (=).

* **provider** should be **AlgoliaSearch**
* **scope** is a common name (prefix) of all indexes. Each document type is stored in a separate index. Full index name is `scope-documenttype`. One server can serve multiple indexes.
* **ApiId** is a api id for algolia server.
* **ApiKey** is a api key for either algolia server.

You can configure the search configuration string either in the VC Manager UI or in VC Manager web.config. Web.config has higher priority.
* VC Manager: **Settings > Search > General > Search configuration string**

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
