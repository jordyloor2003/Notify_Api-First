using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NotifyApi.Application.DTOs;
using NotifyApi.Application.Interfaces;
using NotifyApi.Domain.Entities;

namespace NotifyApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Produces("application/json")]
[Authorize]
public class TemplatesController : ControllerBase
{
    private readonly ITemplateRepository _templateRepository;

    public TemplatesController(ITemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTemplates(CancellationToken ct)
    {
        var templates = await _templateRepository.GetAllAsync(ct);
        var dtos = templates.Select(t => new TemplateResponse
        {
            Id = t.Id,
            Code = t.Code,
            Name = t.Name,
            Channel = t.Channel.ToString().ToUpperInvariant(),
            Subject = t.Subject,
            Version = t.Version,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(typeof(TemplateResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTemplate([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        var appSub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? User.FindFirst("sub")?.Value
                     ?? Guid.NewGuid().ToString();

        var appId = Guid.TryParse(appSub, out var parsedAppId) ? parsedAppId : Guid.NewGuid();

        var template = new Template(
            applicationId: appId,
            code: request.Code,
            name: request.Name,
            channel: request.Channel,
            bodyTemplate: request.BodyTemplate,
            subject: request.Subject,
            requiredVariables: request.RequiredVariables
        );

        await _templateRepository.AddAsync(template, ct);

        var dto = new TemplateResponse
        {
            Id = template.Id,
            Code = template.Code,
            Name = template.Name,
            Channel = template.Channel.ToString().ToUpperInvariant(),
            Subject = template.Subject,
            Version = template.Version,
            UpdatedAt = template.UpdatedAt
        };

        return CreatedAtAction(nameof(ListTemplates), new { id = template.Id }, dto);
    }
}
