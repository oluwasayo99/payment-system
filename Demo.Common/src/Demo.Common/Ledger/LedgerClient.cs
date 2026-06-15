using System.Net.Http.Json;

namespace Demo.Common.Ledger;

public class LedgerClient
{
    private readonly HttpClient httpClient;

    public LedgerClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<ReserveResponse> ReserveAsync(Guid userId, decimal amount)
    {
        var request = new ReserveRequest(userId, amount);

        var response = await httpClient.PostAsJsonAsync("/ledger/reserve", request);
        if(!response.IsSuccessStatusCode)
        {
            throw new Exception($"Ledger reservation failed: {response.StatusCode}");
        }
        var result = await response.Content.ReadFromJsonAsync<ReserveResponse>();
        return result!;
    }

    public async Task<ReleaseResponse> ReleaseAsync(Guid reservationId)
    {
        var response = await httpClient.PostAsJsonAsync("/ledger/release", new ReleaseRequest(reservationId));
        if(!response.IsSuccessStatusCode)
        {
            var error  = await response.Content.ReadAsStringAsync();
            throw new Exception($"Release failed: {response.StatusCode} - {error}");

        }
        return (await response.Content.ReadFromJsonAsync<ReleaseResponse>())!;
    }

    public async Task<SettleResponse> SettleAsync(Guid reservationId)
    {
        var response = await httpClient.PostAsJsonAsync("/ledger/settle", new SettleRequest(reservationId));
        if(!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Settlement failed: {response.StatusCode}");
        }

        return (await response.Content.ReadFromJsonAsync<SettleResponse>())!;
    }
}