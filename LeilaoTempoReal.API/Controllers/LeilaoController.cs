using LeilaoTempoReal.API.DTOs;
using LeilaoTempoReal.API.Services;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using LeilaoTempoReal.Infraestrutura.Dados;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoTempoReal.API.Controllers;

[ApiController]
[Route("api/leiloes")]
public class LeilaoController(ILeilaoRepository repository, LeilaoDbContext context, LeilaoService service) : ControllerBase
{
    private readonly ILeilaoRepository _repository = repository;
    private readonly LeilaoDbContext _context = context;
    private readonly LeilaoService _service = service;

    // GET api/leiloes/1
    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var leilao = await _repository.ObterPorIdAsync(id);

        if (leilao == null) return NotFound();

        return Ok(leilao);
    }

    // POST api/leiloes/seed
    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var leilao = new Leilao("PlayStation 5 - Edição Limitada", 3000.00m, DateTime.Now.AddDays(2));

        _context.Leiloes.Add(leilao);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Leilão criado!", Id = leilao.Id });
    }

    [HttpPost("{id}/lances")]
    public async Task<IActionResult> EnviarLance(Guid id, [FromBody] LanceDto dto)
    {
        var sucesso = await _service.DarLanceAsync(id, dto.Usuario, dto.Valor);

        if (!sucesso)
        {
            return BadRequest("Lance inválido. O valor deve ser maior que o atual ou o leilão encerrou.");
        }

        return Ok(new { Message = "Lance aceito!" });
    }
}