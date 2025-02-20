# Virto Commerce Algolia Search Module

The Virto Commerce Algolia Search module implements ISearchProvider defined in the Virto Commerce Core module and uses [Algolia search cloud service](https://algolia.com).

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

## Documentation

* [Algolia Search module user documentation](https://docs.virtocommerce.org/platform/user-guide/algolia/overview/)
* [Algolia Search module developer documentation](https://docs.virtocommerce.org/platform/developer-guide/Fundamentals/Indexed-Search/integration/algolia/)
* [Algolia Search configuration](https://docs.virtocommerce.org/platform/developer-guide/Configuration-Reference/appsettingsjson/#algolia)
* [View on GitHub](https://github.com/VirtoCommerce/vc-module-algolia-search)


## References

* [Deployment](https://docs.virtocommerce.org/platform/developer-guide/Tutorials-and-How-tos/Tutorials/deploy-module-from-source-code/)
* [Installation](https://docs.virtocommerce.org/platform/user-guide/modules-installation/)
* [Home](https://virtocommerce.com)
* [Community](https://www.virtocommerce.org)
* [Download latest release](https://github.com/VirtoCommerce/vc-module-algolia-search/releases/latest)


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
