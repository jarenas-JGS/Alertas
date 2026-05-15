using Alertas.Services.Notificaciones.DTOs;
using System.Net;

namespace Alertas.Services.Notificaciones
{
    public class PlantillaCorreoAlertasService : IPlantillaCorreoAlertasService
    {
        public string GenerarHtml(GrupoAlertasUsuarioDto correo)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
</head>
<body style='font-family: Arial, sans-serif; color:#222;'>
    <h2>Alertas del Proyecto {WebUtility.HtmlEncode(correo.NombreProyecto)}</h2>

    <p>Sr(a). <strong>{WebUtility.HtmlEncode(correo.NombreUsuario)}</strong>,</p>

    <p>
        A continuación se relacionan las alertas del proyecto
        <strong>{WebUtility.HtmlEncode(correo.NombreProyecto)}</strong>
        para su seguimiento:
    </p>";

            foreach (var grupo in correo.Alertas
                .GroupBy(a => new { a.NombreAlerta, a.NombreMensaje, a.TextoMensaje, a.Prioridad })
                .OrderBy(g => g.Key.Prioridad)
                .ThenBy(g => g.Key.NombreAlerta))

            {
                var esPrioridadAlta = grupo.Key.Prioridad == 1;

                var colorTitulo = esPrioridadAlta ? "#dc3545" : "#0d6efd";
                var fondoTitulo = esPrioridadAlta ? "#f8d7da" : "#eef5ff";
                var borde = esPrioridadAlta ? "#dc3545" : "#0d6efd";
                var textoPrioridad = esPrioridadAlta ? "ALTA" : $"Prioridad {grupo.Key.Prioridad}";

                html += $@"
                <div style='margin-top:24px; border-left:5px solid {borde}; background:{fondoTitulo}; padding:12px 14px;'>
                    <h3 style='margin:0; color:{colorTitulo}; font-size:18px;'>
                        {WebUtility.HtmlEncode(grupo.Key.NombreAlerta)}
                    </h3>
                    <div style='font-size:13px; color:#555; margin-top:4px;'>
                        {WebUtility.HtmlEncode(grupo.Key.NombreMensaje)} · {textoPrioridad}
                    </div>
                </div>

                <p style='margin-top:10px; font-size:14px;'>
                    {WebUtility.HtmlEncode(grupo.Key.TextoMensaje)}
                </p>";

                html += $@"
                <table width='100%' cellpadding='7' cellspacing='0' border='1'
                       style='border-collapse:collapse; font-size:13px; border-color:#ddd; margin-top:10px;'>
                    <thead>
                        <tr style='background:#f2f2f2;'>
                            <th align='left'>Obligación</th>
                            <th align='left'>Empresa</th>
                            <th align='left'>Autorizador(es)</th>
                            <th align='left'>F. Venc Obligación</th>
                            <th align='right'>Días</th>
                            <th align='left'>F. Seguimiento</th>
                            <th align='right'>Días</th>
                        </tr>
                    </thead>
                    <tbody>";

                foreach (var item in grupo
                    .OrderBy(a => a.FechaVencimientoObligacion)
                    .ThenBy(a => a.NombreEmpresa)
                    .ThenBy(a => a.NombreObligacion))
                {
                    var estiloFila = esPrioridadAlta
                        ? "background:#fff5f5;"
                        : "";

                    html += $@"
                    <tr style='{estiloFila}'>
                        <td>{WebUtility.HtmlEncode(item.NombreObligacion)}</td>
                        <td>{WebUtility.HtmlEncode(item.NombreEmpresa)}</td>
                        <td>{WebUtility.HtmlEncode(item.Autorizadores)}</td>
                        <td>{item.FechaVencimientoObligacion:dd/MM/yyyy}</td>
                        <td align='right'><strong>{item.DiasVencimientoObligacion}</strong></td>
                        <td>{item.FechaVencimientoSeguimiento:dd/MM/yyyy}</td>
                        <td align='right'><strong>{item.DiasVencimientoSeguimiento}</strong></td>
                    </tr>";
                }

                html += @"
                </tbody>
            </table>";
                    }

                    html += @"
            <p style='margin-top:30px; font-size:12px; color:#666;'>
                Este correo fue generado automáticamente por el Sistema de Alertas.
            </p>
        </body>
        </html>";

            return html;
        }
    }
}