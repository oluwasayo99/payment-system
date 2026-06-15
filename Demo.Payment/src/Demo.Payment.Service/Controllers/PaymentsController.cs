using Demo.Common.Contracts;
using Demo.Payments.Service.Filters;
using Demo.Payments.Service.Services;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Payments.Service.Controllers;

[ApiController]
[Route("/payments")]
public class PaymentsController: ControllerBase
{
    private readonly PaymentService paymentService;
    public PaymentsController(PaymentService paymentService, IPublishEndpoint publishEndpoint)
    {
        this.paymentService = paymentService;
    }

    [HttpPost("/authorize")]
    [ServiceFilter(typeof(IdempotencyFilter))]
    public async Task<IActionResult> Authorize(PaymentRequest request)
    {
        var key = HttpContext.Items["IdempotencyKey"]?.ToString();
        var result = await paymentService.AuthorizeAsync(request, key!);
        return Ok(result);
    }
}