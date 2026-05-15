using Alertas.ViewModels.CargaMasiva;
using ClosedXML.Excel;

namespace Alertas.Services.CargaMasiva
{
    public class ExcelObligacionesReader : IExcelObligacionesReader
    {
        public async Task<List<CargaObligacionesFilaViewModel>> LeerArchivoAsync(IFormFile archivo)
        {
            var filas = new List<CargaObligacionesFilaViewModel>();

            using var stream = new MemoryStream();
            await archivo.CopyToAsync(stream);
            stream.Position = 0;

            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet("Obligaciones");

            var ultimaFila = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= ultimaFila; row++)
            {
                if (FilaVacia(ws, row))
                    continue;

                filas.Add(new CargaObligacionesFilaViewModel
                {
                    numero_fila = row,

                    nombre = LeerTexto(ws, row, 1),
                    codigo_obligacion = LeerTexto(ws, row, 2),
                    cliente = LeerTexto(ws, row, 3),
                    empresa = LeerTexto(ws, row, 4),
                    ciudad = LeerTexto(ws, row, 5),
                    periodo = LeerTexto(ws, row, 6),
                    dominio = LeerTexto(ws, row, 7),
                    tipo_obligacion = LeerTexto(ws, row, 8),

                    vigencia = LeerEntero(ws, row, 9),

                    fecha_vencimiento_obligacion = LeerFecha(ws, row, 10),
                    fecha_vencimiento_seguimiento = LeerFecha(ws, row, 11),

                    valor_aproximado = LeerDecimal(ws, row, 12),
                    saldo_favor = LeerDecimal(ws, row, 13),

                    cc_empleador = LeerTexto(ws, row, 14),
                    empleador = LeerTexto(ws, row, 15),
                    cc_empleado = LeerTexto(ws, row, 16),
                    empleado = LeerTexto(ws, row, 17),

                    responsable = LeerTexto(ws, row, 18),
                    elaborador = LeerTexto(ws, row, 19),
                    autorizador = LeerTexto(ws, row, 20),
                    aprobador = LeerTexto(ws, row, 21),
                    usuario_vencimiento = LeerTexto(ws, row, 22),

                    observaciones = LeerTexto(ws, row, 23)
                });
            }

            return filas;
        }

        private static bool FilaVacia(IXLWorksheet ws, int row)
        {
            for (int col = 1; col <= 23; col++)
            {
                if (!string.IsNullOrWhiteSpace(ws.Cell(row, col).GetString()))
                    return false;
            }

            return true;
        }

        private static string? LeerTexto(IXLWorksheet ws, int row, int col)
        {
            var valor = ws.Cell(row, col).GetString()?.Trim();
            return string.IsNullOrWhiteSpace(valor) ? null : valor;
        }

        private static DateTime? LeerFecha(IXLWorksheet ws, int row, int col)
        {
            var cell = ws.Cell(row, col);

            if (cell.IsEmpty())
                return null;

            if (cell.DataType == XLDataType.DateTime)
                return cell.GetDateTime();

            if (DateTime.TryParse(cell.GetString(), out var fecha))
                return fecha;

            return null;
        }

        private static decimal? LeerDecimal(IXLWorksheet ws, int row, int col)
        {
            var cell = ws.Cell(row, col);

            if (cell.IsEmpty())
                return null;

            if (cell.TryGetValue<decimal>(out var valorDecimal))
                return valorDecimal;

            var texto = cell.GetString()
                .Replace("$", "")
                .Replace(".", "")
                .Replace(",", ".")
                .Trim();

            if (decimal.TryParse(
                    texto,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var valor))
                return valor;

            return null;
        }

        private static int? LeerEntero(IXLWorksheet ws, int row, int col)
        {
            var cell = ws.Cell(row, col);

            if (cell.IsEmpty())
                return null;

            if (cell.TryGetValue<int>(out var valorEntero))
                return valorEntero;

            var texto = cell.GetString().Trim();

            if (int.TryParse(texto, out var valor))
                return valor;

            return null;
        }
    }
}