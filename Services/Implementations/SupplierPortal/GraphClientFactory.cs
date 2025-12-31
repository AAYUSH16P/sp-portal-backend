using Azure.Identity;
using Microsoft.Graph;

public static class GraphClientFactory
{
    public static GraphServiceClient Create(
        string tenantId,
        string clientId,
        string clientSecret)
    {
        var credential = new ClientSecretCredential(
            tenantId,
            clientId,
            clientSecret
        );

        return new GraphServiceClient(credential);
    }
}