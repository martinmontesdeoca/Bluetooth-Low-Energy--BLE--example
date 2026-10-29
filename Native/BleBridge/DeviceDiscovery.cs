namespace BleBridge;

/// <summary>Provides deadline-aware service discovery while each search owns its native resources.</summary>
public sealed class DeviceDiscovery
{
    /// <summary>Finds one matching peripheral and explains a timeout in terms of the advertised service.</summary>
    public async Task<ulong> FindAsync(Guid service, TimeSpan timeout, CancellationToken token)
    {
        using var search = new AdvertisementSearch(service);
        try
        {
            return await search.WaitAsync(timeout, token);
        }
        catch (TimeoutException error)
        {
            throw new TimeoutException("No se encontro el servicio. Revisa el anuncio de nRF Connect y su UUID.", error);
        }
    }
}
