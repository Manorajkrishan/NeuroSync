using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController : ControllerBase
{
    private readonly DeviceSyncService _sync;
    private readonly ILogger<DevicesController> _logger;

    public DevicesController(DeviceSyncService sync, ILogger<DevicesController> logger)
    {
        _sync = sync;
        _logger = logger;
    }

    /// <summary>Register / refresh this phone or PC under an owner userId.</summary>
    [HttpPost("register")]
    public IActionResult Register([FromQuery] string userId, [FromBody] DeviceRegisterRequest? body)
    {
        userId = string.IsNullOrWhiteSpace(userId) ? "default" : userId;
        body ??= new DeviceRegisterRequest();
        body.UserAgent ??= Request.Headers.UserAgent.ToString();
        var device = _sync.RegisterDevice(userId, body);
        var payload = _sync.GetSyncPayload(userId, device.DeviceId);
        return Ok(new { device, sync = payload });
    }

    /// <summary>Create a 6-digit code so another device can join this owner.</summary>
    [HttpPost("pair/create")]
    public IActionResult CreatePair([FromQuery] string userId)
    {
        userId = string.IsNullOrWhiteSpace(userId) ? "default" : userId;
        return Ok(_sync.CreatePairingCode(userId));
    }

    /// <summary>Join with pairing code — links this device to the owner's memory.</summary>
    [HttpPost("pair/join")]
    public IActionResult JoinPair([FromBody] JoinPairRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.PairingCode))
            return BadRequest(new { error = "pairingCode is required" });

        var deviceReq = new DeviceRegisterRequest
        {
            DeviceId = req.DeviceId,
            DeviceName = req.DeviceName,
            Platform = req.Platform,
            UserAgent = req.UserAgent ?? Request.Headers.UserAgent.ToString()
        };

        var payload = _sync.JoinWithPairingCode(req.PairingCode, deviceReq);
        if (payload == null)
            return BadRequest(new { error = "Invalid or expired pairing code" });

        return Ok(payload);
    }

    /// <summary>Full sync snapshot for this owner (profile + devices + recent emotions).</summary>
    [HttpGet("sync")]
    public IActionResult Sync([FromQuery] string userId, [FromQuery] string? deviceId = null)
    {
        userId = string.IsNullOrWhiteSpace(userId) ? "default" : userId;
        return Ok(_sync.GetSyncPayload(userId, deviceId));
    }

    [HttpGet]
    public IActionResult List([FromQuery] string userId)
    {
        userId = string.IsNullOrWhiteSpace(userId) ? "default" : userId;
        return Ok(new { userId, devices = _sync.ListDevices(userId) });
    }

    [HttpPost("heartbeat")]
    public IActionResult Heartbeat([FromQuery] string userId, [FromQuery] string deviceId)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(deviceId))
            return BadRequest(new { error = "userId and deviceId required" });
        _sync.Heartbeat(userId, deviceId);
        return Ok(new { ok = true });
    }
}

public class JoinPairRequest
{
    public string PairingCode { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string? UserAgent { get; set; }
}
