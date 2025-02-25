﻿using common;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace extractor;

internal static class ApiPolicy
{
    public static async ValueTask ExportAll(ApiDirectory apiDirectory, ApiUri apiUri, ListRestResources listRestResources, GetRestResource getRestResource, CancellationToken cancellationToken)
    {
        await List(apiUri, listRestResources, cancellationToken)
                .ForEachParallel(async policyName => await Export(apiDirectory,
                                                                  apiUri,
                                                                  policyName,
                                                                  getRestResource,
                                                                  cancellationToken),
                                 cancellationToken);
    }

    private static IAsyncEnumerable<ApiPolicyName> List(ApiUri apiUri, ListRestResources listRestResources, CancellationToken cancellationToken)
    {
        var policiesUri = new ApiPoliciesUri(apiUri);
        var policyJsonObjects = listRestResources(policiesUri.Uri, cancellationToken);
        return policyJsonObjects.Select(json => json.GetStringProperty("name"))
                                .Select(name => new ApiPolicyName(name));
    }

    private static async ValueTask Export(ApiDirectory apiDirectory, ApiUri apiUri, ApiPolicyName policyName, GetRestResource getRestResource, CancellationToken cancellationToken)
    {
        var policyFile = new ApiPolicyFile(policyName, apiDirectory);

        var policiesUri = new ApiPoliciesUri(apiUri);
        var policyUri = new ApiPolicyUri(policyName, policiesUri);
        var responseJson = await getRestResource(policyUri.Uri, cancellationToken);
        var policyContent = responseJson.GetJsonObjectProperty("properties")
                                        .GetStringProperty("value");

        await policyFile.OverwriteWithText(policyContent, cancellationToken);
    }
}
