using System.Security.Claims;
using DestinoPeruAPI.Application.DTOs;
using DestinoPeruAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DestinoPeruAPI.API.Controllers;

[ApiController][Route("api/tours")]
public class ToursController(TourService tourService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string UserRole => User.FindFirstValue(ClaimTypes.Role) ?? "";
    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] string? location,[FromQuery] DateTime? fromDate,[FromQuery] decimal? maxPrice) { if(location!=null||fromDate!=null||maxPrice!=null) return Ok(await tourService.SearchAsync(location,fromDate,maxPrice)); return Ok(await tourService.GetAllActiveAsync()); }
    [HttpGet("{id:int}")] public async Task<IActionResult> GetById(int id) { var r=await tourService.GetByIdAsync(id); return r.Success?Ok(r):NotFound(r); }
    [HttpPost][Authorize(Roles="Agencia")] public async Task<IActionResult> Create([FromBody] CreateTourRequest request) { var r=await tourService.CreateAsync(request,UserId); return r.Success?Created("",r):BadRequest(r); }
    [HttpDelete("{id:int}")][Authorize(Roles="Agencia,Admin")] public async Task<IActionResult> Delete(int id) { var r=await tourService.DeleteAsync(id,UserId,UserRole); return r.Success?Ok(r):BadRequest(r); }
}

[ApiController][Route("api/agencies")][Authorize]
public class AgenciesController(AgencyService agencyService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    [HttpGet][Authorize(Roles="Admin")] public async Task<IActionResult> GetAll() => Ok(await agencyService.GetAllAsync());
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateAgencyRequest request) { var r=await agencyService.CreateAsync(request,UserId); return r.Success?Created("",r):BadRequest(r); }
    [HttpPut("approve/{id:int}")][Authorize(Roles="Admin")] public async Task<IActionResult> Approve(int id) { var r=await agencyService.ApproveAsync(id); return r.Success?Ok(r):NotFound(r); }
}

[ApiController][Route("api/reservations")][Authorize]
public class ReservationsController(ReservationService reservationService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateReservationRequest request) { var r=await reservationService.CreateAsync(request,UserId); return r.Success?Created("",r):BadRequest(r); }
    [HttpGet("user")] public async Task<IActionResult> GetByUser() => Ok(await reservationService.GetByUserAsync(UserId));
    [HttpPut("cancel/{id:int}")] public async Task<IActionResult> Cancel(int id) { var r=await reservationService.CancelAsync(id,UserId); return r.Success?Ok(r):BadRequest(r); }
}

[ApiController][Route("api/payments")][Authorize]
public class PaymentsController(PaymentService paymentService) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    [HttpPost] public async Task<IActionResult> Process([FromBody] CreatePaymentRequest request) { var r=await paymentService.ProcessAsync(request,UserId); return r.Success?Ok(r):BadRequest(r); }
}