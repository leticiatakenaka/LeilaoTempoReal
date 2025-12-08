using System.ComponentModel.DataAnnotations;

namespace LeilaoTempoReal.API.DTOs;

public class LanceDto
{
    [Required]
    public decimal Valor { get; set; }

    [Required]
    public string Usuario { get; set; } = string.Empty;
}