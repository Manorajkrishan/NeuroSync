using Microsoft.AspNetCore.Mvc;
using NeuroSync.Api.Services;

namespace NeuroSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PersonController : ControllerBase
{
    private readonly PersonMemory _personMemory;
    private readonly ILogger<PersonController> _logger;

    public PersonController(PersonMemory personMemory, ILogger<PersonController> logger)
    {
        _personMemory = personMemory;
        _logger = logger;
    }

    [HttpPost("remember")]
    public IActionResult RememberPerson(
        [FromBody] RememberPersonRequest request)
    {
        var person = _personMemory.RememberPerson(
            request.UserId,
            request.Name,
            request.Relationship,
            request.Notes);

        return Ok(new
        {
            success = true,
            person = new
            {
                person.Id,
                person.Name,
                person.Relationship,
                person.Notes
            }
        });
    }

    [HttpGet("list")]
    public IActionResult ListPeople([FromQuery] string userId)
    {
        var people = _personMemory.GetAllPeople(userId);
        return Ok(new
        {
            people = people.Select(p => new
            {
                p.Id,
                p.Name,
                p.Relationship,
                p.Notes,
                p.VoiceNoteIds.Count,
                p.LastMentioned
            })
        });
    }

    [HttpGet("search")]
    public IActionResult SearchPeople([FromQuery] string userId, [FromQuery] string searchTerm)
    {
        var people = _personMemory.SearchPeople(userId, searchTerm);
        return Ok(new
        {
            people = people.Select(p => new
            {
                p.Id,
                p.Name,
                p.Relationship,
                p.Notes
            })
        });
    }
}

public class RememberPersonRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Relationship { get; set; }
    public string? Notes { get; set; }
}

