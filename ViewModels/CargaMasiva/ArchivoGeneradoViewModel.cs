namespace Alertas.ViewModels.CargaMasiva
{
    public class ArchivoGeneradoViewModel
    {
        public byte[] Contenido { get; set; } = Array.Empty<byte>();

        public string ContentType { get; set; } =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public string NombreArchivo { get; set; } = string.Empty;
    }
}