using LeilaoTempoReal.API.DTOs;
using LeilaoTempoReal.Application.Services;
using LeilaoTempoReal.Dominio.Entidades;
using LeilaoTempoReal.Dominio.Interfaces;
using LeilaoTempoReal.Infraestrutura.Dados;
using Microsoft.AspNetCore.Mvc;

namespace LeilaoTempoReal.API.Controllers;

[ApiController]
[Route("api/leiloes")]
public class LeilaoController(ILeilaoRepository repository, LeilaoDbContext context, ILeilaoService service) : ControllerBase
{
    private readonly ILeilaoRepository _repository = repository;
    private readonly LeilaoDbContext _context = context;
    private readonly ILeilaoService _service = service;

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(Guid id)
    {
        var leilao = await _repository.ObterPorIdAsync(id);

        if (leilao == null) return NotFound();

        return Ok(leilao);
    }

    [HttpPost("seed")]
    public async Task<IActionResult> Seed()
    {
        var leilao = new Leilao("PlayStation 5 - Edição Limitada", 3000.00m, DateTime.Now.AddDays(2));

        _context.Leiloes.Add(leilao);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Leilão criado!", leilao.Id });
    }

    [HttpPost("{id}/lances")]
    public async Task<IActionResult> EnviarLance(Guid id, [FromBody] LanceDto dto)
    {
        var resultado = await _service.DarLanceAsync(id, dto.Valor, dto.Usuario);

        if (resultado.IsSuccess)
        {
            return Ok(new { Message = "Lance aceito com sucesso!" });
        }

        if (resultado.Message == "Leilão não encontrado.")
        {
            return NotFound(new { Error = resultado.Message });
        }

        return BadRequest(new { Error = resultado.Message });
    }

    [HttpPost("{id}/finalizar")]
    public async Task<IActionResult> FinalizarLance(Guid id)
    {
        var resultado = await _service.FinalizarLeilaoAsync(id);

        if (resultado.IsSuccess)
        {
            return Ok(new { Message = "Leilão finalizado com sucesso!" });
        }

        if (resultado.Message == "Leilão não encontrado.")
        {
            return NotFound(new { Error = resultado.Message });
        }

        return BadRequest(new { Error = resultado.Message });
    }
}