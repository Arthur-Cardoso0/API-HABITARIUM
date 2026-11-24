using Habitarium.Api.Data;
using Habitarium.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Habitarium.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
[Produces("application/json")]

public class MaterialsController : ControllerBase

{
    private readonly AppDbContext _context;

    public MaterialsController(AppDbContext context)
    {
        _context = context;
    }

    /// Retorna a lista completa de materiais cadastrados no catálogo.
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<Material>))]
    public async Task<ActionResult<IEnumerable<Material>>> GetMaterials()
    {
        return await _context.Materials.ToListAsync();
    }

    /// Busca um material específico pelo seu identificador único (GUID).
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Material))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Material>> GetMaterial(Guid id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null) return NotFound();
        return material;
    }

    /// Cadastra um único material no catálogo.
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(Material))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Material>> CreateMaterial([FromBody] Material material)
    {
        if (material == null)
        {
            return BadRequest("O objeto material não pode ser nulo.");
        }

        _context.Materials.Add(material);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetMaterial), new { id = material.Id }, material);
    }

    /// Cadastra uma lista de materiais em lote (Batch) no catálogo.
    [HttpPost("batch")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(IEnumerable<Material>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<Material>>> CreateMaterialsBatch([FromBody] List<Material> materials)
    {
        if (materials == null || !materials.Any())
        {
            return BadRequest("A lista de materiais não pode estar vazia.");
        }

        _context.Materials.AddRange(materials);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetMaterials), materials);
    }

    /// Remove um único material do catálogo pelo seu identificador único (GUID).
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMaterial(Guid id)
    {
        var material = await _context.Materials.FindAsync(id);
        if (material == null)
        {
            return NotFound();
        }

        _context.Materials.Remove(material);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// Remove múltiplos materiais do catálogo em lote (Bulk Delete).
    [HttpDelete("bulk")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMaterialsBatch([FromBody] List<Guid> ids)
    {
        if (ids == null || !ids.Any())
        {
            return BadRequest("A lista de identificadores não pode estar vazia.");
        }

        var materialsToDelete = await _context.Materials
            .Where(m => ids.Contains(m.Id))
            .ToListAsync();

        if (!materialsToDelete.Any())
        {
            return NotFound("Nenhum dos materiais informados foi encontrado no catálogo.");
        }

        _context.Materials.RemoveRange(materialsToDelete);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            Message = $"{materialsToDelete.Count} material(ais) removido(s) com sucesso.",
            DeletedCount = materialsToDelete.Count,
            DeletedIds = materialsToDelete.Select(m => m.Id)
        });
    }
}