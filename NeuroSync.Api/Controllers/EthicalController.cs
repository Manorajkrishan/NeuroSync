using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;
using NeuroSync.Core;
using Microsoft.Extensions.Logging;

namespace NeuroSync.Api.Controllers;

/// <summary>
/// Controller for ethical AI framework - consent and privacy management
/// </summary>
[ApiController]
[Route("api/ethical")]
public class EthicalController : ControllerBase
{
    private readonly EthicalAIFrameworkService _ethicalFramework;
    private readonly ILogger<EthicalController> _logger;

    public EthicalController(
        EthicalAIFrameworkService ethicalFramework,
        ILogger<EthicalController> logger)
    {
        _ethicalFramework = ethicalFramework;
        _logger = logger;
    }

    /// <summary>
    /// Set user consent
    /// </summary>
    [HttpPost("consent")]
    public IActionResult SetConsent([FromBody] ConsentRequest request)
    {
        try
        {
            var userId = request.UserId ?? "default";
            
            var consent = new EthicalAIConsent
            {
                UserId = userId,
                EmotionSensingConsent = request.EmotionSensingConsent,
                VisualLayerConsent = request.VisualLayerConsent,
                AudioLayerConsent = request.AudioLayerConsent,
                BiometricLayerConsent = request.BiometricLayerConsent,
                DataStorageConsent = request.DataStorageConsent,
                DataSharingConsent = request.DataSharingConsent ?? false,
                AnonymizationEnabled = request.AnonymizationEnabled ?? true
            };

            _ethicalFramework.SetConsent(userId, consent);

            return Ok(new { success = true, message = "Consent updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting consent: {Message}", ex.Message);
            return StatusCode(500, new { error = "An error occurred while setting consent" });
        }
    }

    /// <summary>
    /// Get user consent
    /// </summary>
    [HttpGet("consent")]
    public IActionResult GetConsent([FromQuery] string userId)
    {
        try
        {
            userId = userId ?? "default";
            var consent = _ethicalFramework.GetConsent(userId);
            
            if (consent == null)
            {
                return Ok(new { 
                    emotionSensingConsent = false,
                    visualLayerConsent = false,
                    audioLayerConsent = false,
                    biometricLayerConsent = false,
                    dataStorageConsent = false
                });
            }

            return Ok(new
            {
                emotionSensingConsent = consent.EmotionSensingConsent,
                visualLayerConsent = consent.VisualLayerConsent,
                audioLayerConsent = consent.AudioLayerConsent,
                biometricLayerConsent = consent.BiometricLayerConsent,
                dataStorageConsent = consent.DataStorageConsent,
                dataSharingConsent = consent.DataSharingConsent,
                anonymizationEnabled = consent.AnonymizationEnabled
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consent: {Message}", ex.Message);
            return StatusCode(500, new { error = "An error occurred while getting consent" });
        }
    }
}

/// <summary>
/// Request model for consent
/// </summary>
public class ConsentRequest
{
    public string? UserId { get; set; }
    public bool EmotionSensingConsent { get; set; }
    public bool VisualLayerConsent { get; set; }
    public bool AudioLayerConsent { get; set; }
    public bool BiometricLayerConsent { get; set; }
    public bool DataStorageConsent { get; set; }
    public bool? DataSharingConsent { get; set; }
    public bool? AnonymizationEnabled { get; set; }
}
