using Alertas.Data;
using Alertas.ViewModels.CargaMasiva;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace Alertas.Services.CargaMasiva
{
    public class PlantillaObligacionesService : IPlantillaObligacionesService
    {
        private readonly ApplicationDbContext _context;

        public PlantillaObligacionesService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ArchivoGeneradoViewModel> GenerarPlantillaAsync(
            int idProyecto,
            int idUsuarioActual)
        {
            var proyecto = await _context.Proyectos
                .FirstOrDefaultAsync(p =>
                    p.id_proyecto == idProyecto &&
                    p.activo &&
                    p.configuracion_completa);

            if (proyecto == null)
                throw new InvalidOperationException("El proyecto no existe, no está activo o no está completamente configurado.");

            var idArea = proyecto.id_area;

            var clientes = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Join(_context.Clientes,
                    e => e.id_cliente,
                    c => c.id_cliente,
                    (e, c) => c)
                .Where(c => c.activo)
                .Select(c => c.nit + " - " + c.nombre)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var empresas = await _context.AreasEmpresas
                .Where(ae => ae.id_area == idArea && ae.activo)
                .Join(_context.Empresas,
                    ae => ae.id_empresa,
                    e => e.id_empresa,
                    (ae, e) => e)
                .Where(e => e.activo)
                .Select(e => e.nit + " - " + e.nombre)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            var ciudades = await _context.Ciudades
                .OrderBy(c => c.nombre)
                .Select(c => c.nombre)
                .ToListAsync();

            var periodos = await _context.Periodos
                .OrderBy(p => p.nombre)
                .Select(p => p.nombre)
                .ToListAsync();

            var dominios = await _context.Dominios
                .OrderBy(d => d.nombre)
                .Select(d => d.nombre)
                .ToListAsync();

            var tiposObligacion = await _context.TipoObligaciones
                .Where(t => t.id_area == idArea && t.activo)
                .OrderBy(t => t.orden)
                .ThenBy(t => t.nombre)
                .Select(t => t.nombre)
                .ToListAsync();

            var usuarios = await _context.UsuarioArea
                .Where(ua =>
                    ua.id_area == idArea &&
                    ua.activo)
                .Join(_context.Usuarios,
                    ua => ua.id_usuario,
                    u => u.id_usuario,
                    (ua, u) => u)
                .Where(u => u.activo)
                .Select(u => u.email + " - " + u.nombre)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            using var workbook = new XLWorkbook();

            var ws = workbook.Worksheets.Add("Obligaciones");
            var listas = workbook.Worksheets.Add("Listas");
            var instrucciones = workbook.Worksheets.Add("Instrucciones");

            CrearHojaObligaciones(ws);
            CrearHojaListas(
                listas,
                clientes,
                empresas,
                ciudades,
                periodos,
                dominios,
                tiposObligacion,
                usuarios);

            CrearHojaInstrucciones(instrucciones);

            AplicarListasDesplegables(
                ws,
                listas,
                clientes,
                empresas,
                ciudades,
                periodos,
                dominios,
                tiposObligacion,
                usuarios);

            listas.Hide();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return new ArchivoGeneradoViewModel
            {
                Contenido = stream.ToArray(),
                NombreArchivo = $"Plantilla_Obligaciones_{proyecto.nombre}_{DateTime.Now:yyyyMMddHHmm}.xlsx"
            };
        }

        private static void CrearHojaObligaciones(IXLWorksheet ws)
        {
            string[] columnas =
            {
                "Nombre",
                "CodigoObligacion",
                "Cliente",
                "Empresa",
                "Ciudad",
                "Periodo",
                "Dominio",
                "TipoObligacion",
                "Vigencia",
                "FechaVencimientoObligacion",
                "FechaVencimientoSeguimiento",
                "ValorAproximado",
                "SaldoFavor",
                "CCEmpleador",
                "Empleador",
                "CCEmpleado",
                "Empleado",
                "Responsable",
                "Elaborador",
                "Autorizador",
                "Aprobador",
                "UsuarioVencimiento",
                "Observaciones"
            };

            for (int i = 0; i < columnas.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = columnas[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, 1, columnas.Length).SetAutoFilter();

            ws.Column(9).Style.NumberFormat.Format = "0";          // Vigencia
            ws.Column(10).Style.DateFormat.Format = "yyyy-mm-dd";  // FechaVencimientoObligacion
            ws.Column(11).Style.DateFormat.Format = "yyyy-mm-dd";  // FechaVencimientoSeguimiento
            ws.Column(12).Style.NumberFormat.Format = "#,##0.00";  // ValorAproximado
            ws.Column(13).Style.NumberFormat.Format = "#,##0.00";  // SaldoFavor

            ws.Columns().AdjustToContents();
        }

        private static void CrearHojaListas(
            IXLWorksheet ws,
            List<string> clientes,
            List<string> empresas,
            List<string> ciudades,
            List<string> periodos,
            List<string> dominios,
            List<string> tiposObligacion,
            List<string> usuarios)
        {
            EscribirLista(ws, "Clientes", clientes, 1);
            EscribirLista(ws, "Empresas", empresas, 2);
            EscribirLista(ws, "Ciudades", ciudades, 3);
            EscribirLista(ws, "Periodos", periodos, 4);
            EscribirLista(ws, "Dominios", dominios, 5);
            EscribirLista(ws, "TiposObligacion", tiposObligacion, 6);
            EscribirLista(ws, "Usuarios", usuarios, 7);

            ws.Columns().AdjustToContents();
        }

        private static void EscribirLista(
            IXLWorksheet ws,
            string titulo,
            List<string> valores,
            int columna)
        {
            ws.Cell(1, columna).Value = titulo;
            ws.Cell(1, columna).Style.Font.Bold = true;

            for (int i = 0; i < valores.Count; i++)
            {
                ws.Cell(i + 2, columna).Value = valores[i];
            }
        }

        private static void CrearHojaInstrucciones(IXLWorksheet ws)
        {
            ws.Cell("A1").Value = "Instrucciones para cargue masivo de obligaciones";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 14;

            ws.Cell("A3").Value = "1. Diligencie únicamente la hoja Obligaciones.";
            ws.Cell("A4").Value = "2. Use las listas desplegables disponibles.";
            ws.Cell("A5").Value = "3. No cambie los nombres de las columnas.";
            ws.Cell("A6").Value = "4. Las fechas deben estar en formato AAAA-MM-DD.";
            ws.Cell("A7").Value = "5. Si una fila tiene errores, el archivo completo será rechazado.";
            ws.Cell("A8").Value = "6. La obligación quedará en el estado inicial configurado del proyecto.";
            ws.Cell("A9").Value = "7. Cliente y Empresa deben corresponder entre sí.";
            ws.Cell("A10").Value = "8. Para asignar más de un usuario en un rol, sepárelos por punto y coma (;).";
            ws.Cell("A10").Value = "9. Puede seleccionar un usuario desde la lista o escribir varios separados por punto y coma (;).";

            ws.Columns().AdjustToContents();
        }

        private static void AplicarListasDesplegables(
            IXLWorksheet ws,
            IXLWorksheet listas,
            List<string> clientes,
            List<string> empresas,
            List<string> ciudades,
            List<string> periodos,
            List<string> dominios,
            List<string> tiposObligacion,
            List<string> usuarios)
        {
            const int filaInicial = 2;
            const int filaFinal = 1000;

            AplicarLista(ws, listas, filaInicial, filaFinal, 3, 1, clientes.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 4, 2, empresas.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 5, 3, ciudades.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 6, 4, periodos.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 7, 5, dominios.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 8, 6, tiposObligacion.Count);

            AplicarLista(ws, listas, filaInicial, filaFinal, 18, 7, usuarios.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 19, 7, usuarios.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 20, 7, usuarios.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 21, 7, usuarios.Count);
            AplicarLista(ws, listas, filaInicial, filaFinal, 22, 7, usuarios.Count);
        }

        private static void AplicarLista(
            IXLWorksheet ws,
            IXLWorksheet listas,
            int filaInicial,
            int filaFinal,
            int columnaDestino,
            int columnaLista,
            int cantidadItems)
        {
            if (cantidadItems <= 0)
                return;

            var rangoLista = listas.Range(
                2,
                columnaLista,
                cantidadItems + 1,
                columnaLista);

            var nombreRango = $"Lista_{columnaLista}_{Guid.NewGuid():N}";
            listas.Workbook.NamedRanges.Add(nombreRango, rangoLista);

            var rangoDestino = ws.Range(filaInicial, columnaDestino, filaFinal, columnaDestino);
            var validation = rangoDestino.CreateDataValidation();

            validation.List($"={nombreRango}");
            validation.IgnoreBlanks = true;
            validation.InCellDropdown = true;
            validation.ShowErrorMessage = false;
        }
    }
}