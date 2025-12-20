using System;
using System.Collections.Generic;
using System.Text;

namespace LeilaoTempoReal.Dominio.Interfaces;

public interface INotificador
{
    Task NotificarNovoLance(Guid leilaoId, string usuario, decimal valor);
    Task NotificarLeilaoFinalizado(Guid leilaoId, string usuario, decimal valor);
}
