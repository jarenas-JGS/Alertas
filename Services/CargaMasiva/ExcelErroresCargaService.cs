using Alertas.ViewModels.CargaMasiva;
using ClosedXML.Excel;

namespace Alertas.Services.CargaMasiva
{
    public class ExcelErroresCargaService : IExcelErroresCargaService
    {
        public ArchivoGeneradoViewModel GenerarExcelErrores(
            CargaObligacionesPreviewViewModel preview)
        {
            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Errores");

            ws.Cell(1, 1).Value = "Fila";
            ws.Cell(1, 2).Value = "Columna";
            ws.Cell(1, 3).Value = "Error";

            ws.Range(1, 1, 1, 3).Style.Font.Bold = true;
            ws.Range(1, 1, 1, 3).Style.Fill.BackgroundColor = XLColor.LightGray;

            var row = 2;

            foreach (var error in preview.errores.OrderBy(e => e.numero_fila))
            {
                ws.Cell(row, 1).Value = error.numero_fila;
                ws.Cell(row, 2).Value = error.columna;
                ws.Cell(row, 3).Value = error.mensaje;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ArchivoGeneradoViewModel
            {
                Contenido = stream.ToArray(),
                NombreArchivo = $"Errores_Cargue_Obligaciones_{DateTime.Now:yyyyMMddHHmm}.xlsx"
            };
        }
    }
}