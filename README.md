# Algolia Search Module

The Virto Commerce Algolia Search module implements ISearchProvider defined in the Virto Commerce Core module and uses [Algolia search cloud service](https://algolia.com).

## Key features

1. Connects Virto Commerce with Algolia’s powerful search and filtering capabilities.
1. Ensures real-time synchronization of product data for accurate and up-to-date search results.
1. AI-driven search with typo tolerance, synonyms, and relevance tuning.
1. Enables advanced filtering options (e.g., price, category, attributes).

## Known limitations

1. Need to configure replicas settings for sorting results.
1. Doesn't support Swap indexes.
1. DateTime fields are indexed as long.
1. Algolia sets some upper limits to its services to ensure stability and performance for all users. [Read more](https://www.algolia.com/doc/guides/scaling/algolia-service-limits/).


## Configuration

Algolia Search provider are configurable by these configuration keys:

* **Search.Provider** is the name of the search provider and must be **AlgoliaSearch**.
* **Search.AlgoliaSearch.ApiId** is a Application ID for algolia server.
* **Search.AlgoliaSearch.ApiKey** is a Write API Key for either algolia server.

```
"AlgoliaSearch": {
    "AppId": "API_ID",
    "ApiKey": "API_KEY"
}
```

## Sorting results

Algolia uses one ranking strategy per index. If you want to use different rankings for the same data, you can use replica indices.

To configure available replicas, go to Settings > Algolia search > Sorting.

To help you identify your replica indices, adopt a naming pattern such as {documentType}:{sortingAttribute}-{asc or desc}.

For example, product:price-desc is a replica index of your products index, where the results are sorted by the price attribute in descending order.

If replica indices are not configured, the default index is used and a warning message is added to the log.

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
