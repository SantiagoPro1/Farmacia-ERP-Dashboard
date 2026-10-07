using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace IpecolFarmacia.Models;

public partial class PartidaReceta : ObservableObject
{
    public int IdMedicamento { get; set; }
    public int? IdLote { get; set; }
    public string Clave { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string Dosis { get; set; } = "";
    public int Prescrita { get; set; }
    public int Existencia { get; set; }
    public string Lote { get; set; } = "—";
    public string Caducidad { get; set; } = "";
    public decimal Precio { get; set; }
    public bool EsControlado { get; set; }

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(AccionTexto))]
    [NotifyPropertyChangedFor(nameof(EsBotonVale))]
    [NotifyPropertyChangedFor(nameof(EsBotonSurtir))]
    private bool surtida;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccionTexto))]
    private bool valeGenerado;

    public bool HayExistencia => Existencia >= Prescrita;
    public bool RequiereVale => Existencia < Prescrita;
    public bool TieneStockParcial => Existencia > 0 && Existencia < Prescrita;
    public decimal Importe => Prescrita * Precio;

    public string AccionTexto
    {
        get
        {
            if (Surtida) return "Surtido";
            if (ValeGenerado) return "Vale emitido";
            if (RequiereVale) return "Generar vale";
            return "Surtir";
        }
    }

    public bool EsBotonVale => !Surtida && RequiereVale;
    public bool EsBotonSurtir => !Surtida && !RequiereVale;
}

public class MedicamentoInventarioItem
{
    public int ID_Medicamento { get; set; }
    public string Clave { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Presentacion { get; set; } = "";
    public string SustanciaActiva { get; set; } = "";
    public string Laboratorio { get; set; } = "";
    public int StockActual { get; set; }
    public int StockMinimo { get; set; }
    public int StockMaximo { get; set; }
    public int Disponibles { get; set; }
    public int Surtidos { get; set; }
    public decimal Precio { get; set; }
    public string LoteFEFO { get; set; } = "";
    public string CaducidadFEFO { get; set; } = "";
    public bool EsControlado { get; set; }
    public bool EsCuadroBasico { get; set; }
    public string SemaforoColor { get; set; } = "#38A169";
    public string SemaforoTexto { get; set; } = "🟢 Vigente";
}

public partial class ValePartidaItem : ObservableObject
{
    public long IdValeDetalle { get; set; }
    public long IdVale { get; set; }
    public int IdMedicamento { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int CantidadAutorizada { get; set; } = 1;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSurtir))]
    [NotifyPropertyChangedFor(nameof(EstatusBadgeColor))]
    private int cantidadSurtida = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSurtir))]
    [NotifyPropertyChangedFor(nameof(EstatusBadgeColor))]
    private int cantidadRestante = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeSurtir))]
    [NotifyPropertyChangedFor(nameof(EstatusBadgeColor))]
    private string estatus = "POR SURTIR";

    [ObservableProperty] private bool seleccionadoParaEntrega = true;
    [ObservableProperty] private int cantidadAEntregar = 1;

    public bool PuedeSurtir => CantidadRestante > 0 && Estatus != "SURTIDO";
    public string EstatusBadgeColor => Estatus == "SURTIDO" ? "#38A169" : (CantidadSurtida > 0 ? "#D69E2E" : "#E53E3E");
}

public partial class ValeItem : ObservableObject
{
    public long IdVale { get; set; }
    public string NumeroVale { get; set; } = "";
    public string Carnet { get; set; } = "";
    public string PacienteNombre { get; set; } = "";
    public string NumeroReceta { get; set; } = "";
    public string MedicoNombre { get; set; } = "";
    public string CodigoBarras { get; set; } = "";
    public string Medicamento { get; set; } = "";
    public int CantidadAutorizada { get; set; } = 1;
    public int CantidadSurtida { get; set; } = 0;
    public int CantidadRestante { get; set; } = 1;
    public string FarmaciaSubrogada { get; set; } = "";
    public string FechaEmision { get; set; } = "";
    public string FechaVencimiento { get; set; } = "";
    public int DiasRestantes { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TienePendientes))]
    [NotifyPropertyChangedFor(nameof(PuedeSurtir))]
    [NotifyPropertyChangedFor(nameof(EstatusBadgeColor))]
    private string estatus = "ACTIVO";

    public System.Collections.ObjectModel.ObservableCollection<ValePartidaItem> Medicamentos { get; set; } = new();

    public int TotalMedicamentos => Medicamentos.Count;
    public int TotalPiezasAutorizadas => Medicamentos.Sum(m => m.CantidadAutorizada);
    public int TotalPiezasSurtidas => Medicamentos.Sum(m => m.CantidadSurtida);
    public int TotalPiezasPendientes => Medicamentos.Sum(m => m.CantidadRestante);
    public bool TienePendientes => TotalPiezasPendientes > 0 || Estatus != "SURTIDO";
    public bool PuedeSurtir => TienePendientes && Estatus != "CANCELADO";
    public string ResumenMedicamentos => Medicamentos.Count > 0 ? string.Join(", ", Medicamentos.Select(m => m.Descripcion)) : Medicamento;
    public string EstatusBadgeColor => Estatus == "SURTIDO" ? "#38A169" : (Estatus == "CANCELADO" ? "#718096" : "#E53E3E");

    public void ActualizarMetricas()
    {
        OnPropertyChanged(nameof(TotalMedicamentos));
        OnPropertyChanged(nameof(TotalPiezasAutorizadas));
        OnPropertyChanged(nameof(TotalPiezasSurtidas));
        OnPropertyChanged(nameof(TotalPiezasPendientes));
        OnPropertyChanged(nameof(TienePendientes));
        OnPropertyChanged(nameof(PuedeSurtir));
        OnPropertyChanged(nameof(ResumenMedicamentos));
        OnPropertyChanged(nameof(EstatusBadgeColor));
    }
}

public class MovimientoKardexItem
{
    public string Fecha { get; set; } = "";
    public string TipoMovimiento { get; set; } = "";
    public string Medicamento { get; set; } = "";
    public string Lote { get; set; } = "";
    public string CantidadTexto { get; set; } = "";
    public bool EsEntrada { get; set; }
    public string DocumentoReferencia { get; set; } = "";
    public string Usuario { get; set; } = "";
}

public class FacturaCompraItem
{
    public string NumeroFactura { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string FechaRecepcion { get; set; } = "";
    public int PiezasTotales { get; set; }
    public decimal Total { get; set; }
    public string Estatus { get; set; } = "RECIBIDA";
    public string Observaciones { get; set; } = "";
}

public class DerechohabienteItem
{
    public int IdDerechohabiente { get; set; }
    public string Carnet { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Curp { get; set; } = "";
    public string TipoAfiliacion { get; set; } = "";
    public string Dependencia { get; set; } = "";
    public string Vigencia { get; set; } = "";
    public string Alergias { get; set; } = "";
    public string DiagnosticoFrecuente { get; set; } = "";
    public bool EsVigente { get; set; } = true;
    public string Telefono { get; set; } = "";
}

public class HistorialEntregaItem
{
    public string Fecha { get; set; } = "";
    public string NumeroReceta { get; set; } = "";
    public string Medicamento { get; set; } = "";
    public int Cantidad { get; set; }
    public string Medico { get; set; } = "";
    public string FolioTicket { get; set; } = "";
}

public partial class PestanaTabItem : ObservableObject
{
    public string ClaveVista { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Icono { get; set; } = "";
    public bool EsCerrable { get; set; } = true;

    [ObservableProperty]
    private bool esActiva;
}

public class RecetaVentanillaData
{
    public long IdReceta { get; set; }
    public int IdDerechohabiente { get; set; }
    public string FolioReceta { get; set; } = "";
    public string PacienteNombre { get; set; } = "";
    public string Carnet { get; set; } = "";
    public string Curp { get; set; } = "";
    public string Dependencia { get; set; } = "";
    public string TipoAfiliacion { get; set; } = "";
    public int IdMedico { get; set; }
    public string MedicoNombre { get; set; } = "";
    public string CedulaMedico { get; set; } = "";
    public string EspecialidadMedico { get; set; } = "";
    public string Diagnostico { get; set; } = "";
    public string FechaEmision { get; set; } = "";
    public string FolioTicket { get; set; } = "";
    public System.Collections.Generic.List<PartidaReceta> Partidas { get; set; } = new();
}

public class MetricasInventarioData
{
    public int TotalClaves { get; set; }
    public decimal ValorTotal { get; set; }
    public int StockCriticoCount { get; set; }
    public int AgotadosCount { get; set; }
}

public class MetricasReporteData
{
    public decimal TotalCobrado { get; set; }
    public decimal TotalAbsorcion { get; set; }
    public int RecetasSurtidas { get; set; }
    public int ValesEmitidos { get; set; }
}

public class MedicoItem
{
    public int IdMedico { get; set; }
    public string NombreCompleto { get; set; } = "";
    public string Cedula { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string DisplayText => string.IsNullOrWhiteSpace(Especialidad) ? NombreCompleto : $"{NombreCompleto} · {Especialidad}";
}

public class BeneficiarioItem
{
    public int IdBeneficiario { get; set; }
    public string Carnet { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Parentesco { get; set; } = "Familiar";
    public string Curp { get; set; } = "";
    public string DisplayText => $"{NombreCompleto} ({Parentesco})";
}

public class ProveedorItem
{
    public int IdProveedor { get; set; }
    public string RazonSocial { get; set; } = "";
    public string Rfc { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string DisplayText => $"{RazonSocial} ({Rfc})";
}

public class TicketImpresionData
{
    public string FolioTicket { get; set; } = "";
    public string FechaHora { get; set; } = "";
    public string TerminalCaja { get; set; } = "";
    public string Sucursal { get; set; } = "FARMACIA INSTITUCIONAL IPECOL";
    public string UsuarioCajero { get; set; } = "";
    public string FolioReceta { get; set; } = "";
    public string PacienteNombre { get; set; } = "";
    public string Carnet { get; set; } = "";
    public string TipoAfiliacion { get; set; } = "";
    public string Dependencia { get; set; } = "";
    public string MedicoNombre { get; set; } = "";
    public string CedulaMedico { get; set; } = "";
    public decimal Subtotal { get; set; }
    public decimal Absorcion { get; set; }
    public decimal AportacionPaciente { get; set; }
    public string MetodoPago { get; set; } = "Efectivo";
    public System.Collections.Generic.List<PartidaReceta> PartidasEntregadas { get; set; } = new();
    public System.Collections.Generic.List<ValeItem> ValesEmitidos { get; set; } = new();
}





public class FacturaArticuloItem
{
    public string CodigoBarras { get; set; } = "";
    public string Clave { get; set; } = "";
    public int IdMedicamento { get; set; }
    public string Descripcion { get; set; } = "";
    public string FechaCaducidad { get; set; } = "";
    public DateTime? FechaCaducidadDate { get; set; }
    public int Cantidad { get; set; }
    public string Lote { get; set; } = "";
    public decimal Precio { get; set; }
    public decimal IVA { get; set; }
    public decimal Subtotal => Cantidad * Precio;
    public decimal Total => Subtotal + IVA;
    public string TipoCompra { get; set; } = "COMPRA DIRECTA";
}

public class DocumentoReimpresionItem
{
    public string ID { get; set; } = "";
    public string NumeroReceta { get; set; } = "";
    public string FechaReceta { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string Paciente { get; set; } = "";
    public string FolioTicket { get; set; } = "";
}

public class RecetaDetalleInfoItem
{
    public string Ticket_ID { get; set; } = "";
    public int Cantidad { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string EstatusString { get; set; } = "SURTIDO";
}

public class RecetaValeInfoItem
{
    public string ID_Vale { get; set; } = "";
    public int Cantidad { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Dias_Surtir { get; set; } = 30;
    public int Surtido { get; set; }
    public int Restan { get; set; }
}

public class ReporteCompraRowItem
{
    public string NumeroFactura { get; set; } = "";
    public string NombreProveedor { get; set; } = "";
    public string FechaCompra { get; set; } = "";
    public decimal IVAFactura { get; set; }
    public decimal SubtotalFactura { get; set; }
    public decimal TotalFactura { get; set; }
    public string TipoCompra { get; set; } = "DIRECTA";
    public string Medicamento { get; set; } = "";
    public string Lote { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
}

public class ReporteEntregaRowItem
{
    public string NumeroTicket { get; set; } = "";
    public string Fecha { get; set; } = "";
    public int Cantidad { get; set; }
    public int ID_Medicamento { get; set; }
    public string Codigo { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string ID_Derechohabiente { get; set; } = "";
    public string Nombre { get; set; } = "";
    public string Receta { get; set; } = "";
    public bool EsCuadroBasico { get; set; }
}

public class ReporteEntradaRowItem
{
    public string NumeroFactura { get; set; } = "";
    public string Proveedor { get; set; } = "";
    public string FechaCompra { get; set; } = "";
    public int ID_Medicamento { get; set; }
    public string CodigoBarras { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Renglon { get; set; }
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal IVA { get; set; }
    public decimal Subtotal { get; set; }
    public string FechaCaducidad { get; set; } = "";
    public string Lote { get; set; } = "";
}

public partial class UsuarioSistemaItem : ObservableObject
{
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = "";
    [ObservableProperty] private string nombre = "";
    [ObservableProperty] private string email = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PerfilColor))]
    [NotifyPropertyChangedFor(nameof(DisplayText))]
    private string perfil = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EsActivo))]
    [NotifyPropertyChangedFor(nameof(EstatusTexto))]
    [NotifyPropertyChangedFor(nameof(EstatusColor))]
    private int idEstatus = 1;

    public bool EsActivo => IdEstatus == 1;
    public string EstatusTexto => EsActivo ? "Activo" : "Inactivo";
    public string EstatusColor => EsActivo ? "#38A169" : "#E53E3E";
    public string PerfilColor => Perfil switch
    {
        "ADMINISTRADOR" => "#D4AF37",
        "FARMACIA_ADMIN" => "#3182CE",
        "FARMACIA_ENTREGA" => "#38A169",
        "CAPTURA" => "#DD6B20",
        _ => "#718096"
    };
    public string DisplayText => $"{Nombre} ({Perfil})";
}
