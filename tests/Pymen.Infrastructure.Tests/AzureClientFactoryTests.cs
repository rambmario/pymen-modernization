using FluentAssertions;
using Pymen.Infrastructure.Messaging;
using Pymen.Infrastructure.Storage;

namespace Pymen.Infrastructure.Tests;

public sealed class AzureClientFactoryTests
{
    [Fact]
    public void ServiceBus_IsConfigured_when_connection_string_present()
    {
        ServiceBusClientFactory.IsConfigured(new ServiceBusOptions { ConnectionString = "Endpoint=sb://x" })
            .Should().BeTrue();
    }

    [Fact]
    public void ServiceBus_IsConfigured_when_fully_qualified_namespace_present()
    {
        ServiceBusClientFactory.IsConfigured(new ServiceBusOptions
        {
            FullyQualifiedNamespace = "sb-demo.servicebus.windows.net",
        }).Should().BeTrue();
    }

    [Fact]
    public void ServiceBus_not_configured_when_empty()
    {
        ServiceBusClientFactory.IsConfigured(new ServiceBusOptions()).Should().BeFalse();
    }

    [Fact]
    public async Task ServiceBus_Create_with_connection_string_does_not_require_credential()
    {
        var client = ServiceBusClientFactory.Create(new ServiceBusOptions
        {
            ConnectionString =
                "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fake;UseDevelopmentEmulator=true",
        });
        client.Should().NotBeNull();
        await client.DisposeAsync();
    }

    [Fact]
    public void Blob_IsConfigured_when_connection_string_or_uri_present()
    {
        BlobServiceClientFactory.IsConfigured(new AzureStorageOptions { ConnectionString = "UseDevelopmentStorage=true" })
            .Should().BeTrue();
        BlobServiceClientFactory.IsConfigured(new AzureStorageOptions
        {
            BlobServiceUri = "https://stpymen.blob.core.windows.net/",
        }).Should().BeTrue();
        BlobServiceClientFactory.IsConfigured(new AzureStorageOptions()).Should().BeFalse();
    }

    [Fact]
    public void Blob_Create_with_connection_string_uses_Azurite_style()
    {
        var client = BlobServiceClientFactory.Create(new AzureStorageOptions
        {
            ConnectionString =
                "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://127.0.0.1:10000/devstoreaccount1;",
        });
        client.Should().NotBeNull();
        client.Uri.Host.Should().BeOneOf("127.0.0.1", "localhost");
    }
}
