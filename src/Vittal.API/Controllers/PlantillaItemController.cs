using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Vittal.API.Authorization;
using Vittal.API.Extensions;
using Vittal.BLL.Interfaces;
using Vittal.DTO.Plantillas;
using Vittal.Utility;

namespace Vittal.API.Controllers;

/// <summary>
/// API REST para gestión de items individuales dentro de plantillas de especialidad.
/// Tabla global: plantilla_items — sin clinica_id.
/// Solo Super Admin puede gestionar items (restringido via RequirePermissionAttribute + SuperAdminModules).
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PlantillaItemController : ControllerBase
{
    private readonly IPlantillaItemService _service;

    public PlantillaItemController(IPlantillaItemService service)
    {
        _service = service;
    }

    /// <summary>Obtiene todos los items activos de una plantilla.</summary>
    [RequirePermission("plantillas_especialidad", PermissionType.Read)]
    [HttpGet("plantilla/{plantillaId:guid}")]
    public async Task<IActionResult> GetByPlantillaId(Guid plantillaId)
    {
        var result = await _service.GetByPlantillaIdAsync(plantillaId);
        return result.ToActionResult();
    }

    /// <summary>Obtiene un item por su ID.</summary>
    [RequirePermission("plantillas_especialidad", PermissionType.Read)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Create)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PlantillaItemDTOs.Request request)
    {
        var result = await _service.CreateAsync(request);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetById), new { id = result.Data }, new
            {
                success = true,
                message = result.Message,
                data = result.Data
            });
        }

        return result.ToActionResult();
    }

    [RequirePermission("plantillas_especialidad", PermissionType.Update)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] PlantillaItemDTOs.Request request)
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
