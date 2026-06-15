namespace Demo.Settlement.Services;


public class BankService
{
    private readonly Random random = new();

    public async Task<bool> ProcessPaymentAsync(Guid paymentId, decimal amount)
    {
        await Task.Delay(random.Next(1000, 5000));

        var success = random.Next(1, 100) <= 90;

        if (!success)
        {
            Console.WriteLine($"Bank processing FAILED for payment {paymentId}");
            return false;
        }

        Console.WriteLine($"Bank processing SUCCESS for payment {paymentId}");

        return true;

    }
}