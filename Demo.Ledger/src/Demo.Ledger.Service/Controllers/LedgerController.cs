

using Demo.Ledger.Service;
using Demo.Ledger.Service.Services;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("ledger")]
public class LedgerController : ControllerBase
{
    private readonly LedgerService ledgerService;

    public LedgerController(LedgerService service)
    {
        this.ledgerService = service;
    }

    [HttpPost("reserve")]
    public async Task<IActionResult> Reserve(ReserveRequest request)
    {
        Console.WriteLine("Request received");
        try
        {
            var result = await ledgerService.ReserveAsync(request.UserId, request.Amount);
            return Ok(result);
        }
        catch (Exception ex)
        {

            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release(ReleaseRequest request)
    {
        try
        {
            var result = await ledgerService.ReleaseAsync(request.ReservationId);
            return Ok(result);
        }
        catch (Exception ex)
        {

            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("settle")]
    public async Task<IActionResult> Settle(SettleRequest request)
    {
        try
        {
            var result = await ledgerService.SettleAsync(request.ReservationId);
            return Ok(result);
        }
        catch (Exception ex)
        {

            return BadRequest(new { error = ex.Message });
        }
    }

}