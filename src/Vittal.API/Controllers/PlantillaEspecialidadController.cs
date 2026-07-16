using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.API.Models;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Plantillas;
using Vittal.Utility;

namespace Vittal.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlantillaEspecialidadController : ControllerBase
{
    private readonly IPlantillaEspecialidadService _service;

    public PlantillaEspecialidadController(IPlantillaEspecialidadService service)
    {
        _service = service;
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Read)]
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlantillaEspecialidadDTOs.Request request)
    {
        var result = await _service.CreateAsync(request);

        if (result.IsSuccess)
        {
            var response = new ApiResponse<Guid>
            {
                Success = true,
                Message = result.Message,
                Data = result.Data
            };
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, response);
        }

        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlantillaEspecialidadDTOs.Request request)
    {
        var result = await _service.UpdateAsync(id, request);
        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Update)]
    [HttpPatch("{id:guid}/desactivar")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var result = await _service.DeactivateAsync(id);
        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Update)]
    [HttpPatch("{id:guid}/reactivar")]
    public async Task<IActionResult> Reactivar(Guid id)
    {
        var result = await _service.ReactivateAsync(id);
        return result.ToActionResult();
    }
}
