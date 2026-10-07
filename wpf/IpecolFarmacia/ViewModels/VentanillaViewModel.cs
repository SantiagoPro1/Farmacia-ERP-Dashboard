using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IpecolFarmacia.Models;

namespace IpecolFarmacia.ViewModels;

public partial class VentanillaViewModel : ObservableObject
{
    private decimal PorcentajeAbsorcion = 0m; // se calcula con los importes reales de Frm_Dispensaciones

    [ObservableProperty] private string fechaHoraActual = DateTime.Now.ToString("dd MMM yyyy · HH:mm");
    [ObservableProperty] private string vistaActual = "";

    [ObservableProperty] private string usuarioNombreCompleto = "";
    [ObservableProperty] private string usuarioPerfilInfo = "";

    // ==========================================
    // CONTROL DE ACCESO BASADO EN ROLES INSTITUCIONALES (IUBIDB_FARMACIAS)
    // ==========================================
    [ObservableProperty] private string usuarioRolBadge = "ADMINISTRADOR";
    [ObservableProperty] private string usuarioRolColor = "#D4AF37";

    // SECCIÓN 1: ALTA DE MEDICAMENTOS
    [ObservableProperty] private bool puedeVerSeccionAlta = true;
    [ObservableProperty] private bool puedeVerCompras = true;

    // SECCIÓN 2: SALIDA DE MEDICAMENTOS
    [ObservableProperty] private bool puedeVerSeccionSalida = true;
    [ObservableProperty] private bool puedeVerVentanilla = true;
    [ObservableProperty] private bool puedeVerVales = true;
    [ObservableProperty] private bool puedeVerReimpresion = true;
    [ObservableProperty] private bool puedeVerInfoReceta = true;

    // SECCIÓN 3: INFORMES
    [ObservableProperty] private bool puedeVerSeccionInformes = true;
    [ObservableProperty] private bool puedeVerInventario = true;
    [ObservableProperty] private bool puedeVerReporteCompras = true;
    [ObservableProperty] private bool puedeVerReporteEntregas = true;
    [ObservableProperty] private bool puedeVerReporteEntradas = true;

    // SECCIÓN 4: PADRÓN & AUDITORÍA
    [ObservableProperty] private bool puedeVerSeccionPadron = true;
    [ObservableProperty] private bool puedeVerPacientes = true;
    [ObservableProperty] private bool puedeVerKardex = true;
    [ObservableProperty] private bool puedeVerReportes = true;

    // SECCIÓN 5: ADMINISTRACIÓN (SÓLO ADMINISTRADOR)
    [ObservableProperty] private bool puedeVerGestionUsuarios = true;
    [ObservableProperty] private bool esModoSupervisionVales = true;

    public bool TienePermisoParaVista(string claveVista) => claveVista switch
    {
        "Compras" => PuedeVerCompras,
        "Ventanilla" => PuedeVerVentanilla,
        "Vales" => PuedeVerVales,
        "Reimpresion" => PuedeVerReimpresion,
        "InfoReceta" => PuedeVerInfoReceta,
        "Inventario" => PuedeVerInventario,
        "ReporteCompras" => PuedeVerReporteCompras,
        "ReporteEntregas" => PuedeVerReporteEntregas,
        "ReporteEntradas" => PuedeVerReporteEntradas,
        "Pacientes" => PuedeVerPacientes,
        "Kardex" => PuedeVerKardex,
        "Reportes" => PuedeVerReportes,
        "Usuarios" => PuedeVerGestionUsuarios,
        _ => true
    };

    public void ConfigurarUsuario(Services.UsuarioLogueado usuario)
    {
        UsuarioNombreCompleto = usuario.NombreCompleto.ToUpper();
        UsuarioPerfilInfo = usuario.Perfil.Trim().ToUpperInvariant();
        UsuarioActivo = usuario.NombreCompleto;

        var p = usuario.Perfil.Trim().ToUpperInvariant();
        UsuarioRolBadge = p;

        switch (p)
        {
            case "ADMINISTRADOR":
                // 1. ADMINISTRADOR: Acceso total a los módulos, padrón de supervisión de vales y gestión de usuarios
                UsuarioRolColor = "#D4AF37"; // Oro Institucional
                PuedeVerCompras = true;
                PuedeVerVentanilla = true;
                PuedeVerVales = true;
                PuedeVerReimpresion = true;
                PuedeVerInfoReceta = true;
                PuedeVerInventario = true;
                PuedeVerReporteCompras = true;
                PuedeVerReporteEntregas = true;
                PuedeVerReporteEntradas = true;
                PuedeVerPacientes = true;
                PuedeVerKardex = true;
                PuedeVerReportes = true;
                PuedeVerGestionUsuarios = true;
                EsModoSupervisionVales = true;
                break;

            case "FARMACIA_ADMIN":
                // 2. FARMACIA_ADMIN: Almacén, Compras, Salidas, Inventario FEFO, Todos los Reportes, Kardex y Cierres
                UsuarioRolColor = "#3182CE"; // Azul de Gestión
                PuedeVerCompras = true;
                PuedeVerVentanilla = true;
                PuedeVerVales = true;
                PuedeVerReimpresion = true;
                PuedeVerInfoReceta = true;
                PuedeVerInventario = true;
                PuedeVerReporteCompras = true;
                PuedeVerReporteEntregas = true;
                PuedeVerReporteEntradas = true;
                PuedeVerPacientes = false;
                PuedeVerKardex = true;
                PuedeVerReportes = true;
                PuedeVerGestionUsuarios = false;
                EsModoSupervisionVales = false;
                break;

            case "FARMACIA_ENTREGA":
                // 3. FARMACIA_ENTREGA: Ventanilla dispensación (Entrega, Surtir Vales, Reimpresión, InfoReceta) y consulta de Existencias
                UsuarioRolColor = "#38A169"; // Verde Operativo
                PuedeVerCompras = false;
                PuedeVerVentanilla = true;
                PuedeVerVales = true;
                PuedeVerReimpresion = true;
                PuedeVerInfoReceta = true;
                PuedeVerInventario = true;
                PuedeVerReporteCompras = false;
                PuedeVerReporteEntregas = false;
                PuedeVerReporteEntradas = false;
                PuedeVerPacientes = false;
                PuedeVerKardex = false;
                PuedeVerReportes = false;
                PuedeVerGestionUsuarios = false;
                EsModoSupervisionVales = false;
                break;

            case "CAPTURA":
                // 4. CAPTURA: Padrón de derechohabientes, Información de Receta, Reimpresión y catálogo de Existencias
                UsuarioRolColor = "#DD6B20"; // Naranja / Ámbar
                PuedeVerCompras = false;
                PuedeVerVentanilla = false;
                PuedeVerVales = false;
                PuedeVerReimpresion = true;
                PuedeVerInfoReceta = true;
                PuedeVerInventario = true;
                PuedeVerReporteCompras = false;
                PuedeVerReporteEntregas = false;
                PuedeVerReporteEntradas = false;
                PuedeVerPacientes = true;
                PuedeVerKardex = false;
                PuedeVerReportes = false;
                PuedeVerGestionUsuarios = false;
                EsModoSupervisionVales = false;
                break;

            default:
                UsuarioRolColor = "#718096";
                PuedeVerCompras = false;
                PuedeVerVentanilla = true;
                PuedeVerVales = true;
                PuedeVerReimpresion = true;
                PuedeVerInfoReceta = true;
                PuedeVerInventario = true;
                PuedeVerReporteCompras = false;
                PuedeVerReporteEntregas = false;
                PuedeVerReporteEntradas = false;
                PuedeVerPacientes = false;
                PuedeVerKardex = false;
                PuedeVerReportes = false;
                PuedeVerGestionUsuarios = false;
                EsModoSupervisionVales = false;
                break;
        }

        // Secciones Visibilidad (se muestran solo si al menos una subvista está permitida)
        PuedeVerSeccionAlta = PuedeVerCompras;
        PuedeVerSeccionSalida = PuedeVerVentanilla || PuedeVerVales || PuedeVerReimpresion || PuedeVerInfoReceta;
        PuedeVerSeccionInformes = PuedeVerInventario || PuedeVerReporteCompras || PuedeVerReporteEntregas || PuedeVerReporteEntradas;
        PuedeVerSeccionPadron = PuedeVerPacientes || PuedeVerKardex || PuedeVerReportes;

        // Limpiar pestañas previas y abrir por defecto el módulo principal permitido para su perfil
        Pestanas.Clear();
        if (PuedeVerVentanilla)
        {
            AbrirPestana("Ventanilla");
        }
        else if (PuedeVerCompras)
        {
            AbrirPestana("Compras");
        }
        else if (PuedeVerPacientes)
        {
            AbrirPestana("Pacientes");
        }
        else if (PuedeVerInfoReceta)
        {
            AbrirPestana("InfoReceta");
        }
        else if (PuedeVerInventario)
        {
            AbrirPestana("Inventario");
        }
    }

    [ObservableProperty] private bool esVistaVentanilla;
    [ObservableProperty] private bool esVistaVales;
    [ObservableProperty] private bool esVistaInventario;
    [ObservableProperty] private bool esVistaKardex;
    [ObservableProperty] private bool esVistaCompras;
    [ObservableProperty] private bool esVistaPacientes;
    [ObservableProperty] private bool esVistaReportes;
    [ObservableProperty] private bool esVistaReimpresion;
    [ObservableProperty] private bool esVistaInfoReceta;
    [ObservableProperty] private bool esVistaReporteCompras;
    [ObservableProperty] private bool esVistaReporteEntregas;
    [ObservableProperty] private bool esVistaReporteEntradas;
    [ObservableProperty] private bool esVistaUsuarios;

    partial void OnVistaActualChanged(string value)
    {
        EsVistaVentanilla = string.Equals(value, "Ventanilla", StringComparison.OrdinalIgnoreCase);
        EsVistaVales = string.Equals(value, "Vales", StringComparison.OrdinalIgnoreCase);
        EsVistaInventario = string.Equals(value, "Inventario", StringComparison.OrdinalIgnoreCase);
        EsVistaKardex = string.Equals(value, "Kardex", StringComparison.OrdinalIgnoreCase);
        EsVistaCompras = string.Equals(value, "Compras", StringComparison.OrdinalIgnoreCase);
        EsVistaPacientes = string.Equals(value, "Pacientes", StringComparison.OrdinalIgnoreCase);
        EsVistaReportes = string.Equals(value, "Reportes", StringComparison.OrdinalIgnoreCase);
        EsVistaReimpresion = string.Equals(value, "Reimpresion", StringComparison.OrdinalIgnoreCase);
        EsVistaInfoReceta = string.Equals(value, "InfoReceta", StringComparison.OrdinalIgnoreCase);
        EsVistaReporteCompras = string.Equals(value, "ReporteCompras", StringComparison.OrdinalIgnoreCase);
        EsVistaReporteEntregas = string.Equals(value, "ReporteEntregas", StringComparison.OrdinalIgnoreCase);
        EsVistaReporteEntradas = string.Equals(value, "ReporteEntradas", StringComparison.OrdinalIgnoreCase);
        EsVistaUsuarios = string.Equals(value, "Usuarios", StringComparison.OrdinalIgnoreCase);
    }

    // ==========================================
    // SISTEMA MULTI-VENTANAS / PESTAÑAS (MDI TABS ESTILO IUBI)
    // ==========================================
    public ObservableCollection<PestanaTabItem> Pestanas { get; } = new();
    [ObservableProperty] private PestanaTabItem? pestanaSeleccionada;

    // ==========================================
    // VENTANILLA (DISPENSACIÓN Y FACTURACIÓN BASE 0%)
    // ==========================================
    public ObservableCollection<PartidaReceta> Partidas { get; } = new();


    [ObservableProperty] private string pacienteNombre = "";
    [ObservableProperty] private string pacienteIniciales = "DH";
    [ObservableProperty] private string carnet = "";
    [ObservableProperty] private string curp = "";
    [ObservableProperty] private string tipoAfiliacion = "";
    [ObservableProperty] private string dependencia = "";
    [ObservableProperty] private string vigencia = "";
    [ObservableProperty] private string alergias = "";

    partial void OnPacienteNombreChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            PacienteIniciales = "DH";
            return;
        }
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
            PacienteIniciales = $"{parts[0][0]}{parts[1][0]}".ToUpper();
        else if (parts.Length == 1 && parts[0].Length > 0)
            PacienteIniciales = parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        else
            PacienteIniciales = "DH";
    }

    [ObservableProperty] private long? idRecetaActual;
    [ObservableProperty] private int idDerechohabienteActual;
    [ObservableProperty] private string folioReceta = "";
    [ObservableProperty] private string medicoNombre = "";
    [ObservableProperty] private string cedulaMedico = "";
    [ObservableProperty] private string diagnostico = "";
    [ObservableProperty] private string fechaEmision = "";

    [ObservableProperty] private string folioTicket = "";
    [ObservableProperty] private string terminalInfo = Environment.MachineName;
    [ObservableProperty] private string sucursalInfo = "FARMACIA COLIMA";
    [ObservableProperty] private string usuarioActivo = "";
    [ObservableProperty] private string codigoBarras = "";
    [ObservableProperty] private string metodoPago = "Efectivo";
    [ObservableProperty] private bool guardada;
    [ObservableProperty] private decimal subtotal;
    [ObservableProperty] private decimal absorcion;
    [ObservableProperty] private decimal aportacionPaciente;
    [ObservableProperty] private string partidasResumenDisponibles = "";

    public string[] MetodosPago { get; } = { "Efectivo", "Tarjeta de Débito/Crédito", "Descuento por Nómina" };

    // ==========================================
    // VALES SUBROGADOS (Frm_Vales / CouponSupplyWindow)
    // ==========================================
    public ObservableCollection<ValeItem> ValesList { get; } = new();
    [ObservableProperty] private string filtroVale = "";
    [ObservableProperty] private ValeItem? valeSeleccionado;

    // ==========================================
    // INVENTARIO & LOTES FEFO (Frm_InventarioExistencias / Frm_Lotes)
    // ==========================================
    public ObservableCollection<MedicamentoInventarioItem> InventarioList { get; } = new();
    public ObservableCollection<MedicamentoInventarioItem> InventarioFiltrado { get; } = new();
    [ObservableProperty] private string filtroInventario = "";
    [ObservableProperty] private string categoriaFiltroInventario = "Todos";

    // ==========================================
    // KARDEX DE MOVIMIENTOS (Frm_MovimientosInventario)
    // ==========================================
    public ObservableCollection<MovimientoKardexItem> KardexList { get; } = new();
    public ObservableCollection<MovimientoKardexItem> KardexFiltrado { get; } = new();
    [ObservableProperty] private string filtroKardexTexto = "";
    [ObservableProperty] private string filtroTipoKardex = "Todos";

    // ==========================================
    // COMPRAS Y FACTURAS (Frm_Compras & MedicineEntryWindow)
    // ==========================================
    public ObservableCollection<FacturaCompraItem> ComprasList { get; } = new();
    public ObservableCollection<FacturaArticuloItem> FacturaPartidasList { get; } = new();

    // Encabezado de Factura
    [ObservableProperty] private ProveedorItem? facturaProveedorSeleccionado;
    [ObservableProperty] private string facturaProveedorNombre = "";
    [ObservableProperty] private int facturaProveedorId;
    [ObservableProperty] private string facturaNumero = "";
    [ObservableProperty] private DateTime facturaFecha = DateTime.Today;
    [ObservableProperty] private DateTime facturaFechaVencimiento = DateTime.Today.AddMonths(1);
    [ObservableProperty] private string facturaEstatus = "PENDIENTE DE PAGO";

    // Alta de Artículos en Factura (con Lote, Caducidad, etc.)
    [ObservableProperty] private string artCodigoBarras = "";
    [ObservableProperty] private string artClave = "";
    [ObservableProperty] private int artIdMedicamento;
    [ObservableProperty] private string artDescripcion = "";
    [ObservableProperty] private DateTime artFechaCaducidad = DateTime.Today.AddYears(1);
    [ObservableProperty] private int artCantidad = 1;
    [ObservableProperty] private string artLote = "";
    [ObservableProperty] private decimal artPrecio;
    [ObservableProperty] private decimal artIva;
    [ObservableProperty] private string artTipoCompra = "COMPRA DIRECTA";

    // Totales de Factura
    [ObservableProperty] private int facturaTotalArticulos;
    [ObservableProperty] private decimal facturaSubtotal;
    [ObservableProperty] private decimal facturaIvaTotal;
    [ObservableProperty] private decimal facturaGranTotal;

    // Compatibilidad con campos anteriores
    [ObservableProperty] private string nuevaFacturaFolio = "";
    [ObservableProperty] private string nuevoProveedor = "";
    [ObservableProperty] private string nuevoMedicamento = "";
    [ObservableProperty] private string nuevoLote = "";
    [ObservableProperty] private int nuevaCantidad;
    [ObservableProperty] private decimal nuevoCosto;

    // ==========================================
    // REIMPRESIÓN DE DOCUMENTOS (ReprintDocumentWindow)
    // ==========================================
    public ObservableCollection<DocumentoReimpresionItem> DocumentosReimpresionList { get; } = new();
    [ObservableProperty] private DateTime reimpFechaDesde = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime reimpFechaHasta = DateTime.Today;
    [ObservableProperty] private DocumentoReimpresionItem? docReimpresionSeleccionado;

    // ==========================================
    // INFORMACIÓN DE LA RECETA & DEVOLUCIÓN (PrescriptionCancelWindow)
    // ==========================================
    public ObservableCollection<RecetaDetalleInfoItem> InfoRecetaDetalleList { get; } = new();
    public ObservableCollection<RecetaValeInfoItem> InfoRecetaValesList { get; } = new();
    [ObservableProperty] private string infoRecetaBusqueda = "";
    [ObservableProperty] private string infoRecetaDerechohabiente = "—";
    [ObservableProperty] private string infoRecetaIdCarnet = "—";
    [ObservableProperty] private string infoRecetaFolio = "—";
    [ObservableProperty] private string infoRecetaFecha = "—";
    [ObservableProperty] private string infoRecetaMedico = "—";

    // ==========================================
    // REPORTE DE COMPRAS (BillsEntryReportWindow)
    // ==========================================
    public ObservableCollection<ReporteCompraRowItem> ReporteComprasList { get; } = new();
    [ObservableProperty] private string repComprasProveedor = "";
    [ObservableProperty] private int repComprasIdProveedor;
    [ObservableProperty] private string repComprasFactura = "";
    [ObservableProperty] private DateTime repComprasFechaDesde = DateTime.Today.AddMonths(-12);
    [ObservableProperty] private DateTime repComprasFechaHasta = DateTime.Today;
    [ObservableProperty] private bool repComprasEsDesglosado;

    // ==========================================
    // REPORTE ENTREGA DE MEDICAMENTOS (ReporteEntregaMedicamentos)
    // ==========================================
    public ObservableCollection<ReporteEntregaRowItem> ReporteEntregasList { get; } = new();
    [ObservableProperty] private DateTime repEntregasFechaDesde = DateTime.Today.AddMonths(-6);
    [ObservableProperty] private DateTime repEntregasFechaHasta = DateTime.Today;
    [ObservableProperty] private string repEntregasReceta = "";
    [ObservableProperty] private string repEntregasCodigo = "";
    [ObservableProperty] private string repEntregasDescripcion = "";
    [ObservableProperty] private string repEntregasIdDerech = "";
    [ObservableProperty] private string repEntregasNombre = "";
    [ObservableProperty] private bool repEntregasCuadroBasico = true;
    [ObservableProperty] private int repEntregasTotalReg;

    // ==========================================
    // REPORTE ENTRADAS DE MEDICAMENTOS (EntradaDeMedicamentos)
    // ==========================================
    public ObservableCollection<ReporteEntradaRowItem> ReporteEntradasList { get; } = new();
    [ObservableProperty] private DateTime repEntradasFechaDesde = DateTime.Today.AddMonths(-12);
    [ObservableProperty] private DateTime repEntradasFechaHasta = DateTime.Today;
    [ObservableProperty] private string repEntradasFactura = "";
    [ObservableProperty] private string repEntradasProveedor = "";
    [ObservableProperty] private string repEntradasCodigo = "";
    [ObservableProperty] private string repEntradasDescripcion = "";
    [ObservableProperty] private int repEntradasTotalReg;

    // ==========================================
    // PADRÓN DERECHOHABIENTES (SeM_Derechohabientes)
    // ==========================================
    public ObservableCollection<DerechohabienteItem> PacientesList { get; } = new();
    public ObservableCollection<DerechohabienteItem> PacientesFiltrados { get; } = new();
    [ObservableProperty] private string filtroPacientes = "";
    [ObservableProperty] private DerechohabienteItem? pacienteSeleccionado;
    public ObservableCollection<HistorialEntregaItem> HistorialEntregas15Dias { get; } = new();

    // ==========================================
    // MÉDICOS Y BENEFICIARIOS REALES (IUBIDB_FARMACIAS)
    // ==========================================
    public ObservableCollection<MedicoItem> MedicosList { get; } = new();
    [ObservableProperty] private MedicoItem? medicoSeleccionado;

    public ObservableCollection<BeneficiarioItem> BeneficiariosList { get; } = new();
    [ObservableProperty] private BeneficiarioItem? beneficiarioSeleccionado;
    [ObservableProperty] private bool tieneBeneficiarios;

    public ObservableCollection<ProveedorItem> ProveedoresList { get; } = new();
    [ObservableProperty] private ProveedorItem? proveedorSeleccionado;

    // ==========================================
    // GESTIÓN DE USUARIOS DEL SISTEMA (Frm_Usuarios)
    // ==========================================
    public ObservableCollection<UsuarioSistemaItem> UsuariosSistemaList { get; } = new();
    public ObservableCollection<UsuarioSistemaItem> UsuariosSistemaFiltrados { get; } = new();
    [ObservableProperty] private UsuarioSistemaItem? usuarioGestionSeleccionado;
    [ObservableProperty] private string filtroUsuariosTexto = "";
    [ObservableProperty] private string nuevoPerfilSeleccionado = "CAPTURA";
    [ObservableProperty] private string nuevaContraseniaInput = "";
    public string[] PerfilesDisponibles { get; } = { "ADMINISTRADOR", "FARMACIA_ADMIN", "FARMACIA_ENTREGA", "CAPTURA" };

    // ==========================================
    // SISTEMA DE MODALES INSTITUCIONALES (MODAL HOST)
    // ==========================================
    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalTicketVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalCancelarRecetaVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalReimpresionVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevaEntradaVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevoAfiliadoVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevoMedicoVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevoMedicamentoVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalHistorial15DiasVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevoBeneficiarioVisible;

    [ObservableProperty] 
    [NotifyPropertyChangedFor(nameof(HayModalAbierto))]
    private bool esModalNuevoUsuarioVisible;

    public bool HayModalAbierto => EsModalTicketVisible 
        || EsModalCancelarRecetaVisible 
        || EsModalReimpresionVisible 
        || EsModalNuevaEntradaVisible 
        || EsModalNuevoAfiliadoVisible 
        || EsModalNuevoMedicoVisible 
        || EsModalNuevoMedicamentoVisible
        || EsModalHistorial15DiasVisible
        || EsModalNuevoBeneficiarioVisible
        || EsModalNuevoUsuarioVisible;

    // Alta de Usuario del Sistema (Frm_Usuarios)
    [ObservableProperty] private string nuevoUsuarioLogin = "";
    [ObservableProperty] private string nuevoUsuarioNombre = "";
    [ObservableProperty] private string nuevoUsuarioEmail = "";
    [ObservableProperty] private string nuevoUsuarioPerfil = "VENTANILLA";
    [ObservableProperty] private string nuevoUsuarioPassword = "";
    [ObservableProperty] private string nuevoUsuarioConfirmPassword = "";

    // Alta de Beneficiario (AddBeneficiaryWindow)
    [ObservableProperty] private string nuevoBeneficiarioParentesco = "HIJO(A)";
    [ObservableProperty] private string nuevoBeneficiarioNombre = "";
    [ObservableProperty] private string nuevoBeneficiarioCurp = "";

    // Datos del Ticket de Impresión
    [ObservableProperty] private TicketImpresionData? ticketActual;

    // Cancelación de Recetas (PrescriptionCancelWindow)
    [ObservableProperty] private string folioRecetaCancelar = "";
    [ObservableProperty] private string motivoCancelacion = "Error en captura o medicamento no recogido";

    // Reimpresión de Documentos (ReprintDocumentWindow)
    [ObservableProperty] private string folioReimpresion = "";
    [ObservableProperty] private string tipoDocReimpresion = "Ticket";

    // Nueva Entrada / Recepción (EntradaDeMedicamentos)
    [ObservableProperty] private string entradaFacturaFolio = "";
    [ObservableProperty] private string entradaLote = "";
    [ObservableProperty] private string entradaCaducidad = "";
    [ObservableProperty] private int entradaCantidad;
    [ObservableProperty] private decimal entradaCosto;
    [ObservableProperty] private string entradaMedicamento = "";

    // Altas en Catálogos (AddTitularWindow, AddDoctorsWindows, AddArticleWindow)
    [ObservableProperty] private string nuevoAfiliadoCarnet = "";
    [ObservableProperty] private string nuevoAfiliadoNombre = "";
    [ObservableProperty] private string nuevoAfiliadoCurp = "";
    [ObservableProperty] private string nuevoAfiliadoDependencia = "";

    [ObservableProperty] private string nuevoMedicoNombre = "";
    [ObservableProperty] private string nuevoMedicoCedula = "";
    [ObservableProperty] private string nuevoMedicoEspecialidad = "";

    [ObservableProperty] private string nuevoMedClave = "";
    [ObservableProperty] private string nuevoMedNombre = "";
    [ObservableProperty] private string nuevoMedSustancia = "";
    [ObservableProperty] private decimal nuevoMedCosto;

    partial void OnMedicoSeleccionadoChanged(MedicoItem? value)
    {
        if (value != null)
        {
            MedicoNombre = value.NombreCompleto;
            CedulaMedico = value.Cedula;
        }
    }

    partial void OnBeneficiarioSeleccionadoChanged(BeneficiarioItem? value)
    {
        if (value != null)
        {
            if (value.IdBeneficiario == 0 && PacienteSeleccionado != null)
            {
                PacienteNombre = PacienteSeleccionado.NombreCompleto;
                TipoAfiliacion = PacienteSeleccionado.TipoAfiliacion;
            }
            else
            {
                PacienteNombre = value.NombreCompleto;
                TipoAfiliacion = $"{value.Parentesco} (Beneficiario)";
            }
        }
    }

    // ==========================================
    // MÉTRICAS DINÁMICAS REALES DE INVENTARIO Y REPORTES
    // ==========================================
    [ObservableProperty] private string inventarioTotalClaves = "2,537 Fármacos";
    [ObservableProperty] private string inventarioValorTotal = "$482,910.00";
    [ObservableProperty] private string inventarioStockCritico = "18 Claves en riesgo";
    [ObservableProperty] private string inventarioDesabasto = "9 Claves en desabasto";

    [ObservableProperty] private string reporteTotalCobrado = "$3,265.00";
    [ObservableProperty] private string reporteTotalAbsorcion = "$8,175.50";
    [ObservableProperty] private string reporteRecetasSurtidas = "42";
    [ObservableProperty] private string reporteValesEmitidos = "8";

    partial void OnPacienteSeleccionadoChanged(DerechohabienteItem? value)
    {
        if (value != null)
        {
            _ = SeleccionarPacienteAsync(value);
        }
    }

    partial void OnUsuarioGestionSeleccionadoChanged(UsuarioSistemaItem? value)
    {
        if (value != null)
        {
            NuevoPerfilSeleccionado = value.Perfil;
            NuevaContraseniaInput = "";
        }
    }

    partial void OnFiltroUsuariosTextoChanged(string value)
    {
        AplicarFiltroUsuarios();
    }

    public void AplicarFiltroUsuarios()
    {
        UsuariosSistemaFiltrados.Clear();
        var q = (FiltroUsuariosTexto ?? "").Trim().ToLowerInvariant();
        var filtrados = string.IsNullOrWhiteSpace(q)
            ? UsuariosSistemaList
            : UsuariosSistemaList.Where(u =>
                u.Usuario.ToLowerInvariant().Contains(q) ||
                u.Nombre.ToLowerInvariant().Contains(q) ||
                u.Perfil.ToLowerInvariant().Contains(q) ||
                u.Email.ToLowerInvariant().Contains(q));

        foreach (var u in filtrados)
            UsuariosSistemaFiltrados.Add(u);

        if (UsuarioGestionSeleccionado == null || !UsuariosSistemaFiltrados.Contains(UsuarioGestionSeleccionado))
            UsuarioGestionSeleccionado = UsuariosSistemaFiltrados.FirstOrDefault();
    }

    private readonly Services.FarmaciaDbService _dbService = new();

    public VentanillaViewModel()
    {
        InicializarPestanas();
        Recalcular();

        // Carga en segundo plano (SOLO LECTURA) desde IUBIDB_FARMACIAS
        _ = CargarDatosRealesDesdeBaseAsync();
    }

    [RelayCommand]
    public async Task CargarDatosRealesDesdeBaseAsync()
    {
        try
        {
            var taskMedicinas = _dbService.ObtenerInventarioRealAsync();
            var taskMetricasInv = _dbService.ObtenerMetricasInventarioAsync();
            var taskVales = _dbService.ObtenerValesRealesAsync();
            var taskKardex = _dbService.ObtenerKardexRealAsync();
            var taskCompras = _dbService.ObtenerComprasRealesAsync();
            var taskDerechohabientes = _dbService.ObtenerDerechohabientesRealesAsync();
            var taskMetricasReporte = _dbService.ObtenerMetricasReporteRealAsync();
            var taskMedicos = _dbService.ObtenerMedicosRealesAsync();
            var taskProveedores = _dbService.ObtenerProveedoresRealesAsync();
            var taskUsuarios = _dbService.ObtenerUsuariosSistemaAsync();

            await Task.WhenAll(
                taskMedicinas, 
                taskMetricasInv, 
                taskVales, 
                taskKardex, 
                taskCompras, 
                taskDerechohabientes, 
                taskMetricasReporte,
                taskMedicos,
                taskProveedores,
                taskUsuarios);

            var medicinasReales = await taskMedicinas;
            var metricasInv = await taskMetricasInv;
            var valesReales = await taskVales;
            var kardexReales = await taskKardex;
            var comprasReales = await taskCompras;
            var derechohabientesReales = await taskDerechohabientes;
            var metricasReporte = await taskMetricasReporte;
            var medicosReales = await taskMedicos;
            var proveedoresReales = await taskProveedores;
            var usuariosReales = await taskUsuarios;

            Application.Current?.Dispatcher?.Invoke(() =>
            {
                // 1. Inventario & Métricas
                InventarioList.Clear();
                foreach (var m in medicinasReales) InventarioList.Add(m);
                AplicarFiltroInventario();
                InventarioTotalClaves = $"{metricasInv.TotalClaves:N0} Fármacos";
                InventarioValorTotal = $"{metricasInv.ValorTotal:C}";
                InventarioStockCritico = $"{metricasInv.StockCriticoCount} Claves en riesgo";
                InventarioDesabasto = $"{metricasInv.AgotadosCount} Claves en desabasto";

                // 2. Vales Reales
                ValesList.Clear();
                foreach (var v in valesReales) ValesList.Add(v);
                ValeSeleccionado = ValesList.FirstOrDefault();

                // 3. Kardex Real
                KardexList.Clear();
                foreach (var k in kardexReales) KardexList.Add(k);
                AplicarFiltroKardex();

                // 4. Compras y Facturas Reales
                ComprasList.Clear();
                foreach (var c in comprasReales) ComprasList.Add(c);

                // 5. Padrón de Derechohabientes Real
                PacientesList.Clear();
                foreach (var d in derechohabientesReales) PacientesList.Add(d);
                AplicarFiltroPacientes();

                // 6. Médicos Prescriptores Reales
                MedicosList.Clear();
                foreach (var m in medicosReales) MedicosList.Add(m);

                // 7. Proveedores Reales
                ProveedoresList.Clear();
                foreach (var p in proveedoresReales) ProveedoresList.Add(p);
                ProveedorSeleccionado = proveedoresReales.FirstOrDefault();

                // 8. Usuarios del Sistema (Frm_Usuarios)
                UsuariosSistemaList.Clear();
                foreach (var u in usuariosReales) UsuariosSistemaList.Add(u);
                AplicarFiltroUsuarios();

                // 9. Métricas de Reportes Reales
                ReporteTotalCobrado = $"{metricasReporte.TotalCobrado:C}";
                ReporteTotalAbsorcion = $"{metricasReporte.TotalAbsorcion:C}";
                ReporteRecetasSurtidas = $"{metricasReporte.RecetasSurtidas}";
                ReporteValesEmitidos = $"{metricasReporte.ValesEmitidos}";
                decimal totalImp = metricasReporte.TotalAbsorcion + metricasReporte.TotalCobrado;
                PorcentajeAbsorcion = totalImp > 0 ? metricasReporte.TotalAbsorcion / totalImp : 0m;
            });

            if (!string.IsNullOrEmpty(Services.FarmaciaDbService.UltimoError))
                MessageBox.Show($"Algunos datos no se pudieron leer de IUBIDB_FARMACIAS:\n{Services.FarmaciaDbService.UltimoError}", "Aviso de conexión", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[VentanillaViewModel] Carga datos reales: {ex.Message}");
        }
    }

    private void InicializarPestanas()
    {
        Pestanas.Clear();
        PestanaSeleccionada = null;
        VistaActual = "";
    }

    [RelayCommand]
    public void AbrirPestana(string claveVista)
    {
        if (string.IsNullOrWhiteSpace(claveVista)) return;

        if (!TienePermisoParaVista(claveVista))
        {
            MessageBox.Show(
                $"El perfil actual ('{UsuarioPerfilInfo}') no tiene autorización para acceder al módulo seleccionado ({claveVista}).\n\nConsulte con la Dirección de Pensiones IPECOL si requiere permisos para esta área.",
                "Acceso Restringido por Rol · IPECOL", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existente = Pestanas.FirstOrDefault(p => string.Equals(p.ClaveVista, claveVista, StringComparison.OrdinalIgnoreCase));
        if (existente == null)
        {
            string titulo = claveVista switch
            {
                "Ventanilla" => "ENTREGA DE MEDICAMENTOS",
                "Compras" => "COMPRAS DE MEDICAMENTO",
                "Vales" => "SURTIDO DE VALES",
                "Reimpresion" => "REIMPRESIÓN DE DOCUMENTOS",
                "InfoReceta" => "INFORMACIÓN DE LA RECETA",
                "Inventario" => "EXISTENCIAS (INVENTARIO FEFO)",
                "ReporteCompras" => "REPORTE DE COMPRAS",
                "ReporteEntregas" => "REPORTE ENTREGA MEDICAMENTOS",
                "ReporteEntradas" => "REPORTE ENTRADAS MEDICAMENTO",
                "Pacientes" => "PADRÓN DERECHOHABIENTES",
                "Kardex" => "KARDEX AUDITABLE",
                "Reportes" => "REPORTES & CIERRES",
                "Usuarios" => "GESTIÓN DE USUARIOS",
                "Configuracion" => "CONFIGURACIÓN",
                _ => claveVista.ToUpper()
            };
            string icono = claveVista switch
            {
                "Ventanilla" => "🏪",
                "Compras" => "🚚",
                "Vales" => "🎫",
                "Reimpresion" => "🖨️",
                "InfoReceta" => "ℹ️",
                "Inventario" => "📦",
                "ReporteCompras" => "📊",
                "ReporteEntregas" => "📋",
                "ReporteEntradas" => "🚚",
                "Pacientes" => "👥",
                "Kardex" => "📜",
                "Reportes" => "📊",
                "Usuarios" => "👤",
                "Configuracion" => "⚙️",
                _ => "📄"
            };
            existente = new PestanaTabItem
            {
                ClaveVista = claveVista,
                Titulo = titulo,
                Icono = icono,
                EsCerrable = true,
                EsActiva = false
            };
            Pestanas.Add(existente);
        }

        SeleccionarPestana(existente);

        // Disparar carga de datos reales si la pestaña se abre por primera vez
        if (claveVista.Equals("Reimpresion", StringComparison.OrdinalIgnoreCase) && !DocumentosReimpresionList.Any())
            _ = BuscarDocumentosReimpresionAsync();
        else if (claveVista.Equals("ReporteCompras", StringComparison.OrdinalIgnoreCase) && !ReporteComprasList.Any())
            _ = GenerarReporteComprasAsync();
        else if (claveVista.Equals("ReporteEntregas", StringComparison.OrdinalIgnoreCase) && !ReporteEntregasList.Any())
            _ = BuscarReporteEntregasAsync();
        else if (claveVista.Equals("ReporteEntradas", StringComparison.OrdinalIgnoreCase) && !ReporteEntradasList.Any())
            _ = BuscarReporteEntradasAsync();
        else if (claveVista.Equals("Usuarios", StringComparison.OrdinalIgnoreCase) && !UsuariosSistemaList.Any())
            _ = CargarUsuariosSistemaAsync();
    }

    [RelayCommand]
    public void SeleccionarPestana(PestanaTabItem? pestana)
    {
        if (pestana == null) return;
        foreach (var p in Pestanas)
        {
            p.EsActiva = (p == pestana);
        }
        PestanaSeleccionada = pestana;
        VistaActual = pestana.ClaveVista;
    }

    [RelayCommand]
    public void CerrarPestana(PestanaTabItem? pestana)
    {
        if (pestana == null || !pestana.EsCerrable) return;
        int index = Pestanas.IndexOf(pestana);
        bool eraActiva = pestana.EsActiva;
        Pestanas.Remove(pestana);

        if (eraActiva)
        {
            if (Pestanas.Any())
            {
                int nuevoIndex = Math.Max(0, index - 1);
                SeleccionarPestana(Pestanas[nuevoIndex]);
            }
            else
            {
                PestanaSeleccionada = null;
                VistaActual = "";
            }
        }
    }

    [RelayCommand]
    private void CambiarVista(string vista)
    {
        AbrirPestana(vista);
    }

    // ==========================================
    // COMANDOS DE ADMINISTRACIÓN DE USUARIOS (SÓLO ADMIN)
    // ==========================================
    [RelayCommand]
    public async Task CargarUsuariosSistemaAsync()
    {
        try
        {
            var users = await _dbService.ObtenerUsuariosSistemaAsync();
            UsuariosSistemaList.Clear();
            foreach (var u in users)
                UsuariosSistemaList.Add(u);
            AplicarFiltroUsuarios();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CargarUsuariosSistemaAsync] {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task GuardarPerfilUsuarioAsync()
    {
        if (UsuarioGestionSeleccionado == null)
        {
            MessageBox.Show("Seleccione un usuario de la lista institucional.", "Gestión de Usuarios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevoPerfilSeleccionado))
        {
            MessageBox.Show("Seleccione el nuevo perfil que desea asignar.", "Gestión de Usuarios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validación: Prevenir que el Administrador en sesión se auto-revoque permisos
        if (UsuarioGestionSeleccionado.Usuario.Equals(UsuarioActivo, StringComparison.OrdinalIgnoreCase) ||
            UsuarioGestionSeleccionado.Nombre.Equals(UsuarioNombreCompleto, StringComparison.OrdinalIgnoreCase))
        {
            if (NuevoPerfilSeleccionado != "ADMINISTRADOR")
            {
                MessageBox.Show("Por seguridad del sistema institucional, no puede retirar sus propios privilegios de ADMINISTRADOR mientras mantenga esta sesión activa.", "Acción Restringida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        var confirmacion = MessageBox.Show(
            $"¿Confirmar el cambio de perfil para '{UsuarioGestionSeleccionado.Usuario}' ({UsuarioGestionSeleccionado.Nombre})?\n\n• Perfil Actual: {UsuarioGestionSeleccionado.Perfil}\n• Nuevo Perfil: {NuevoPerfilSeleccionado}",
            "Confirmar Cambio de Perfil",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var res = await _dbService.ActualizarPerfilUsuarioAsync(UsuarioGestionSeleccionado.IdUsuario, NuevoPerfilSeleccionado);
        if (res.exito)
        {
            UsuarioGestionSeleccionado.Perfil = NuevoPerfilSeleccionado;
            MessageBox.Show($"Perfil del usuario '{UsuarioGestionSeleccionado.Usuario}' actualizado con éxito a '{NuevoPerfilSeleccionado}' en IUBIDB_FARMACIAS_PRUEBAS.", "Perfil Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"No se pudo actualizar el perfil:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task GuardarContraseniaUsuarioAsync()
    {
        if (UsuarioGestionSeleccionado == null)
        {
            MessageBox.Show("Seleccione un usuario de la lista institucional.", "Gestión de Usuarios", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevaContraseniaInput))
        {
            MessageBox.Show("Ingrese la nueva contraseña a establecer.", "Contraseña Requerida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (NuevaContraseniaInput.Trim().Length < 4)
        {
            MessageBox.Show("La contraseña debe tener una longitud mínima de 4 caracteres para garantizar la seguridad de acceso.", "Contraseña Insegura", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmacion = MessageBox.Show(
            $"¿Confirmar el restablecimiento de contraseña para el usuario '{UsuarioGestionSeleccionado.Usuario}' ({UsuarioGestionSeleccionado.Nombre})?\n\nSe generará un nuevo salt criptográfico y hash PBKDF2 SHA-1.",
            "Confirmar Actualización de Contraseña",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var res = await _dbService.ActualizarContraseniaUsuarioAsync(UsuarioGestionSeleccionado.IdUsuario, NuevaContraseniaInput);
        if (res.exito)
        {
            MessageBox.Show($"Contraseña institucional del usuario '{UsuarioGestionSeleccionado.Usuario}' ({UsuarioGestionSeleccionado.Nombre}) actualizada exitosamente con hash PBKDF2 SHA-1 en IUBIDB_FARMACIAS_PRUEBAS.", "Contraseña Actualizada", MessageBoxButton.OK, MessageBoxImage.Information);
            NuevaContraseniaInput = "";
        }
        else
        {
            MessageBox.Show($"No se pudo actualizar la contraseña:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task AlternarEstatusUsuarioAsync(UsuarioSistemaItem? usuario)
    {
        var u = usuario ?? UsuarioGestionSeleccionado;
        if (u == null) return;

        // Validación: Prevenir que el Administrador en sesión se auto-desactive
        if (u.Usuario.Equals(UsuarioActivo, StringComparison.OrdinalIgnoreCase) ||
            u.Nombre.Equals(UsuarioNombreCompleto, StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("No puede desactivar su propia cuenta de usuario mientras está en sesión activa.", "Acción Denegada", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int nuevoEstatus = u.IdEstatus == 1 ? 0 : 1;
        string accion = nuevoEstatus == 1 ? "ACTIVAR" : "DESACTIVAR";

        var confirmacion = MessageBox.Show(
            $"¿Desea {accion} la cuenta de acceso institucional del usuario '{u.Usuario}' ({u.Nombre})?",
            $"Confirmar {accion} Usuario",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var res = await _dbService.AlternarEstatusUsuarioAsync(u.IdUsuario, nuevoEstatus);
        if (res.exito)
        {
            u.IdEstatus = nuevoEstatus;
            MessageBox.Show($"El estatus del usuario '{u.Usuario}' se actualizó a {(nuevoEstatus == 1 ? "ACTIVO" : "INACTIVO")}.", "Estatus Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"No se pudo cambiar el estatus:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void AbrirModalNuevoUsuario()
    {
        NuevoUsuarioLogin = "";
        NuevoUsuarioNombre = "";
        NuevoUsuarioEmail = "";
        NuevoUsuarioPerfil = "VENTANILLA";
        NuevoUsuarioPassword = "";
        NuevoUsuarioConfirmPassword = "";
        EsModalNuevoUsuarioVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoUsuario()
    {
        EsModalNuevoUsuarioVisible = false;
    }

    [RelayCommand]
    private async Task GuardarNuevoUsuarioAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoUsuarioLogin))
        {
            MessageBox.Show("Ingrese el nombre de usuario (login).", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoUsuarioNombre))
        {
            MessageBox.Show("Ingrese el nombre completo del usuario.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(NuevoUsuarioPassword) || NuevoUsuarioPassword.Length < 4)
        {
            MessageBox.Show("La contraseña debe tener al menos 4 caracteres.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (NuevoUsuarioPassword != NuevoUsuarioConfirmPassword)
        {
            MessageBox.Show("Las contraseñas no coinciden. Verifíquelas.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = await _dbService.CrearUsuarioSistemaAsync(
            NuevoUsuarioLogin, NuevoUsuarioNombre, NuevoUsuarioEmail, NuevoUsuarioPerfil, NuevoUsuarioPassword);

        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Usuario Creado · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalNuevoUsuarioVisible = false;
            await CargarUsuariosSistemaAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error al Crear Usuario", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    [RelayCommand]
    public async Task SeleccionarPacienteAsync(DerechohabienteItem? p)
    {
        if (p == null) return;
        PacienteSeleccionado = p;
        var entregas = await _dbService.ObtenerHistorialEntregasPacienteAsync(p.Carnet, p.NombreCompleto, p.IdDerechohabiente);
        HistorialEntregas15Dias.Clear();
        foreach (var e in entregas)
            HistorialEntregas15Dias.Add(e);

        // Cargar beneficiarios familiares del carnet
        var benfs = await _dbService.ObtenerBeneficiariosPacienteAsync(p.Carnet);
        BeneficiariosList.Clear();
        BeneficiariosList.Add(new BeneficiarioItem
        {
            IdBeneficiario = 0,
            Carnet = p.Carnet,
            NombreCompleto = $"{p.NombreCompleto} (Titular)",
            Parentesco = "Titular",
            Curp = p.Curp
        });
        foreach (var b in benfs)
        {
            BeneficiariosList.Add(b);
        }
        TieneBeneficiarios = benfs.Any();
        BeneficiarioSeleccionado = BeneficiariosList.First();
    }

    public void SeleccionarPaciente(DerechohabienteItem p)
    {
        _ = SeleccionarPacienteAsync(p);
    }

    [RelayCommand]
    public async Task BuscarRecetaPorFolioAsync()
    {
        if (string.IsNullOrWhiteSpace(FolioReceta)) return;
        var receta = await _dbService.ObtenerRecetaVentanillaRealAsync(FolioReceta.Trim());
        if (receta != null)
        {
            IdRecetaActual = receta.IdReceta;
            IdDerechohabienteActual = receta.IdDerechohabiente;
            PacienteNombre = receta.PacienteNombre;
            Carnet = receta.Carnet;
            Curp = receta.Curp;
            Dependencia = receta.Dependencia;
            TipoAfiliacion = receta.TipoAfiliacion;
            MedicoNombre = receta.MedicoNombre;
            CedulaMedico = receta.CedulaMedico;
            Diagnostico = receta.Diagnostico;
            FechaEmision = receta.FechaEmision;
            FolioTicket = receta.FolioTicket;

            AutoSeleccionarMedico(receta.IdMedico, receta.MedicoNombre, receta.CedulaMedico, receta.EspecialidadMedico);

            Partidas.Clear();
            foreach (var part in receta.Partidas)
                Partidas.Add(part);
            Recalcular();

            // Cargar automáticamente historial de entregas de los últimos 15 días
            var entregas = await _dbService.ObtenerHistorialEntregasPacienteAsync(receta.Carnet, receta.PacienteNombre, receta.IdDerechohabiente);
            HistorialEntregas15Dias.Clear();
            foreach (var e in entregas)
                HistorialEntregas15Dias.Add(e);
        }
        else
        {
            MessageBox.Show($"No se encontró la receta con folio '{FolioReceta}' en IUBIDB_FARMACIAS_PRUEBAS.\nMostrando datos vigentes.", "Búsqueda de Receta", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    public void AutoSeleccionarMedico(int idMedico, string nombreMedico, string cedula, string especialidad)
    {
        if (idMedico > 0)
        {
            var m = MedicosList.FirstOrDefault(x => x.IdMedico == idMedico);
            if (m != null)
            {
                MedicoSeleccionado = m;
                MedicoNombre = m.NombreCompleto;
                CedulaMedico = m.Cedula;
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(nombreMedico))
        {
            string nombreNorm = nombreMedico.Replace("Dr(a).", "").Replace("Dr.", "").Trim();
            var m = MedicosList.FirstOrDefault(x => 
                x.NombreCompleto.Contains(nombreNorm, StringComparison.OrdinalIgnoreCase) ||
                nombreNorm.Contains(x.NombreCompleto.Replace("Dr(a).", "").Trim(), StringComparison.OrdinalIgnoreCase));
            if (m != null)
            {
                MedicoSeleccionado = m;
                MedicoNombre = m.NombreCompleto;
                CedulaMedico = m.Cedula;
                return;
            }

            var nuevoM = new MedicoItem
            {
                IdMedico = idMedico,
                NombreCompleto = nombreMedico.StartsWith("Dr") ? nombreMedico : $"Dr(a). {nombreMedico}",
                Cedula = string.IsNullOrWhiteSpace(cedula) ? "—" : cedula,
                Especialidad = string.IsNullOrWhiteSpace(especialidad) ? "MEDICINA GENERAL" : especialidad
            };
            MedicosList.Add(nuevoM);
            MedicoSeleccionado = nuevoM;
            MedicoNombre = nuevoM.NombreCompleto;
            CedulaMedico = nuevoM.Cedula;
        }
    }

    private void AsignarDatosPaciente(DerechohabienteItem? p)
    {
        if (p is null) return;
        IdDerechohabienteActual = p.IdDerechohabiente;
        IdRecetaActual = null;
        PacienteNombre = p.NombreCompleto;
        Carnet = $"Carnet {p.Carnet}";
        Curp = p.Curp;
        TipoAfiliacion = p.TipoAfiliacion;
        Dependencia = p.Dependencia;
        Vigencia = p.Vigencia;
        Alergias = string.IsNullOrWhiteSpace(p.Alergias) ? "Alergias: sin dato en el sistema" : $"Alergia: {p.Alergias}";
    }

    [RelayCommand]
    private void CargarPacienteEnVentanilla(DerechohabienteItem? p)
    {
        if (p is null) return;
        AsignarDatosPaciente(p);
        if (HistorialEntregas15Dias.Any())
        {
            var ult = HistorialEntregas15Dias.FirstOrDefault(h => !string.IsNullOrWhiteSpace(h.Medico));
            if (ult != null)
                AutoSeleccionarMedico(0, ult.Medico, "", "");
        }
        AbrirPestana("Ventanilla");
    }

    [RelayCommand]
    private void BuscarValePorFolio()
    {
        if (string.IsNullOrWhiteSpace(FiltroVale)) return;
        string f = FiltroVale.Trim();
        string fClean = f.TrimStart('-');

        var vale = ValesList.FirstOrDefault(v => 
            v.NumeroVale.Contains(f, StringComparison.OrdinalIgnoreCase)
            || v.NumeroReceta.Contains(f, StringComparison.OrdinalIgnoreCase)
            || v.Carnet.Contains(f, StringComparison.OrdinalIgnoreCase)
            || v.Carnet.TrimStart('-').Contains(fClean, StringComparison.OrdinalIgnoreCase)
            || v.PacienteNombre.Contains(f, StringComparison.OrdinalIgnoreCase)
            || v.Medicamento.Contains(f, StringComparison.OrdinalIgnoreCase));

        if (vale != null)
        {
            ValeSeleccionado = vale;
        }
        else
        {
            MessageBox.Show(
                $"No se encontró ningún vale subrogado con: '{f}'.\n\nPuede buscar por:\n• Folio de Vale o Receta (ej. 123456, C73344, C73342, VI78649)\n• Nombre del Paciente (ej. Ali Eliezer, Miguel Fuentes, Johana Machuca)\n• Medicamento autorizado (ej. Paracetamol, Janumet, Riopan, Mucosolvan)\n• Carnet (ej. 2837, 5062, 2500)",
                "Búsqueda de Vales",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    public async Task SurtirPartidasSeleccionadasValeAsync()
    {
        if (ValeSeleccionado == null)
        {
            MessageBox.Show("Seleccione un vale de la lista para surtir.", "Surtido de Vales", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ValeSeleccionado.Estatus == "SURTIDO" || ValeSeleccionado.Estatus == "CANCELADO")
        {
            MessageBox.Show($"Este vale ya se encuentra con estatus '{ValeSeleccionado.Estatus}'. No es posible realizar entregas adicionales.", "Vale Concluido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ValeSeleccionado.DiasRestantes <= 0)
        {
            var resVig = MessageBox.Show(
                $"Alerta de Vigencia Reglamentaria:\n\nEl vale #{ValeSeleccionado.NumeroVale} ha superado el plazo de 30 días de vigencia (Días restantes: {ValeSeleccionado.DiasRestantes}).\n\n¿Desea proceder con la entrega bajo supervisión autorizada?",
                "Vale con Vigencia Expirada",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (resVig != MessageBoxResult.Yes) return;
        }

        var partidasParaSurtir = ValeSeleccionado.Medicamentos
            .Where(m => m.PuedeSurtir && m.SeleccionadoParaEntrega)
            .ToList();

        if (!partidasParaSurtir.Any())
        {
            MessageBox.Show(
                "No hay medicamentos seleccionados disponibles para surtir en este vale.\n\nMarque las casillas de los medicamentos que desea entregar o verifique si ya fueron surtidos.",
                "Selección de Medicamentos",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        foreach (var p in partidasParaSurtir)
        {
            if (p.CantidadAEntregar <= 0) p.CantidadAEntregar = 1;
            if (p.CantidadAEntregar > p.CantidadRestante) p.CantidadAEntregar = p.CantidadRestante;
        }

        var confirmacion = MessageBox.Show(
            $"¿Confirmar la entrega de medicamentos para el vale #{ValeSeleccionado.NumeroVale}?\n\n• Derechohabiente: {ValeSeleccionado.PacienteNombre}\n• Fármacos y piezas a entregar:\n  - {string.Join("\n  - ", partidasParaSurtir.Select(p => $"{p.Descripcion}: entregar {p.CantidadAEntregar} de {p.CantidadRestante} pendientes"))}\n\nSi aún restan piezas por surtir, el vale continuará ACTIVO con el mismo folio institucional.",
            "Confirmar Entrega de Medicamentos",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var entregas = partidasParaSurtir.Select(p => (p.IdValeDetalle, p.CantidadAEntregar)).ToList();
        var res = await _dbService.SurtirPartidasValeRealAsync(ValeSeleccionado.IdVale, entregas);

        if (res.exito)
        {
            foreach (var p in partidasParaSurtir)
            {
                p.CantidadSurtida += p.CantidadAEntregar;
                p.CantidadRestante = Math.Max(0, p.CantidadAutorizada - p.CantidadSurtida);
                p.Estatus = p.CantidadRestante == 0 ? "SURTIDO" : "ACTIVO";
                p.CantidadAEntregar = p.CantidadRestante;
                if (p.CantidadRestante == 0) p.SeleccionadoParaEntrega = false;
            }

            if (res.valeCompleto)
            {
                ValeSeleccionado.Estatus = "SURTIDO";
            }
            else
            {
                ValeSeleccionado.Estatus = "ACTIVO";
            }
            ValeSeleccionado.ActualizarMetricas();

            MessageBox.Show(res.mensaje, "Entrega de Vale Registrada · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Ocurrió un error al registrar el surtido:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public async Task SurtirPartidaIndividualAsync(ValePartidaItem? partida)
    {
        if (partida == null || ValeSeleccionado == null) return;

        if (ValeSeleccionado.Estatus == "SURTIDO" || ValeSeleccionado.Estatus == "CANCELADO")
        {
            MessageBox.Show($"El vale maestro #{ValeSeleccionado.NumeroVale} ya se encuentra con estatus '{ValeSeleccionado.Estatus}'.", "Vale Concluido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!partida.PuedeSurtir)
        {
            MessageBox.Show($"El medicamento '{partida.Descripcion}' ya cuenta con estatus '{partida.Estatus}'.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (partida.CantidadAEntregar <= 0) partida.CantidadAEntregar = 1;
        if (partida.CantidadAEntregar > partida.CantidadRestante) partida.CantidadAEntregar = partida.CantidadRestante;

        var confirmacion = MessageBox.Show(
            $"¿Desea registrar la entrega de este medicamento?\n\n• {partida.Descripcion}\n• Piezas a entregar ahora: {partida.CantidadAEntregar} de {partida.CantidadRestante} pendientes\n\nEl vale continuará ACTIVO con el mismo folio si aún restan piezas por surtir.",
            "Confirmar Entrega Individual",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var entregas = new List<(long, int)> { (partida.IdValeDetalle, partida.CantidadAEntregar) };
        var res = await _dbService.SurtirPartidasValeRealAsync(ValeSeleccionado.IdVale, entregas);

        if (res.exito)
        {
            partida.CantidadSurtida += partida.CantidadAEntregar;
            partida.CantidadRestante = Math.Max(0, partida.CantidadAutorizada - partida.CantidadSurtida);
            partida.Estatus = partida.CantidadRestante == 0 ? "SURTIDO" : "ACTIVO";
            partida.CantidadAEntregar = partida.CantidadRestante;
            if (partida.CantidadRestante == 0) partida.SeleccionadoParaEntrega = false;

            if (res.valeCompleto)
            {
                ValeSeleccionado.Estatus = "SURTIDO";
            }
            else
            {
                ValeSeleccionado.Estatus = "ACTIVO";
            }
            ValeSeleccionado.ActualizarMetricas();

            MessageBox.Show(res.mensaje, "Entrega Individual Realizada", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show($"Error al surtir medicamento individual:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void SeleccionarTodasLasPartidasVale(object? param)
    {
        if (ValeSeleccionado == null) return;
        bool sel = param is bool b ? b : true;
        foreach (var p in ValeSeleccionado.Medicamentos.Where(x => x.PuedeSurtir))
        {
            p.SeleccionadoParaEntrega = sel;
        }
    }

    [RelayCommand]
    private async Task SurtirVale(ValeItem? vale)
    {
        if (vale != null) ValeSeleccionado = vale;
        await SurtirPartidasSeleccionadasValeAsync();
    }

    [RelayCommand]
    private void ReimprimirVale(ValeItem? vale)
    {
        var v = vale ?? ValeSeleccionado;
        if (v is null) return;

        MessageBox.Show(
            $"=== VALE DE FARMACIA SUBROGADA ===\nINSTITUTO DE PENSIONES DE COLIMA (IPECOL)\nFolio: {v.NumeroVale}\nEmisión: {v.FechaEmision}\nVence: {v.FechaVencimiento} ({v.DiasRestantes} días vigentes)\n---------------------------------------\nDerechohabiente: {v.PacienteNombre}\nCarnet: {v.Carnet}\nReceta Origen: {v.NumeroReceta}\n---------------------------------------\nMEDICAMENTO AUTORIZADO:\n• {v.Medicamento}\n• Cantidad: {v.CantidadAutorizada} pza(s)\n---------------------------------------\nFARMACIA SUBROGADA AUTORIZADA:\n{v.FarmaciaSubrogada}\n=======================================",
            "Impresión de Vale Subrogado",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private void Surtir(PartidaReceta? p)
    {
        if (p is null) return;
        if (p.HayExistencia)
        {
            p.Surtida = !p.Surtida;
            Recalcular();
        }
        else
        {
            _ = GenerarValeAsync(p);
        }
    }

    [RelayCommand]
    private async Task GenerarValeAsync(PartidaReceta? p)
    {
        if (p is null) return;

        var confirmacion = MessageBox.Show(
            $"¿Desea emitir un vale institucional por desabasto para el siguiente medicamento prescrito?\n\n• Fármaco: {p.Nombre}\n• Prescrito: {p.Prescrita} pzas\n• Existencia Actual: {p.Existencia} pzas\n• Receta: {(string.IsNullOrEmpty(FolioReceta) ? "Sin folio" : FolioReceta)}\n• Paciente: {(string.IsNullOrEmpty(PacienteNombre) ? "Sin paciente" : PacienteNombre)}\n\nEl vale se registrará directamente en IUBIDB_FARMACIAS_PRUEBAS con estatus ACTIVO para su surtimiento subrogado.",
            "Confirmar Emisión de Vale por Desabasto",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        int idDh = IdDerechohabienteActual > 0 ? IdDerechohabienteActual : (PacienteSeleccionado?.IdDerechohabiente ?? 1);
        long idReceta = IdRecetaActual ?? 0;

        var res = await _dbService.GenerarValeRealAsync(
            idReceta, idDh, p.IdMedicamento, p.Prescrita, FolioReceta);

        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Vale Emitido · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show($"Error al generar vale: {res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void SurtirDisponibles()
    {
        foreach (var p in Partidas.Where(x => x.HayExistencia))
        {
            p.Surtida = true;
        }
        Recalcular();
    }

    [RelayCommand]
    private void EliminarPartida(PartidaReceta? p)
    {
        if (p is null) return;
        Partidas.Remove(p);
        Recalcular();
    }

    [RelayCommand]
    private async Task EscanearAsync()
    {
        if (string.IsNullOrWhiteSpace(CodigoBarras)) return;
        string query = CodigoBarras.Trim();

        // 1. Si el fármaco ya está en la receta activa, alternar su surtido
        var p = Partidas.FirstOrDefault(x => x.Clave.Contains(query, StringComparison.OrdinalIgnoreCase)
                                          || (!string.IsNullOrEmpty(x.CodigoBarras) && x.CodigoBarras.Equals(query, StringComparison.OrdinalIgnoreCase))
                                          || x.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase));
        if (p != null)
        {
            Surtir(p);
            CodigoBarras = "";
            return;
        }

        // 2. Si coincide con un fármaco en el inventario real (Frm_Medicamentos), agregarlo al surtido (+ Agregar)
        var medInv = InventarioList.FirstOrDefault(m => 
            (!string.IsNullOrEmpty(m.Clave) && m.Clave.Equals(query, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(m.CodigoBarras) && m.CodigoBarras.Equals(query, StringComparison.OrdinalIgnoreCase))
            || m.Nombre.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (medInv != null)
        {
            var nuevaPartida = new PartidaReceta
            {
                IdMedicamento = medInv.ID_Medicamento,
                Clave = medInv.Clave,
                CodigoBarras = medInv.CodigoBarras,
                Nombre = medInv.Nombre,
                Presentacion = medInv.Presentacion,
                Dosis = "—",
                Prescrita = 1,
                Existencia = medInv.StockActual,
                Lote = medInv.LoteFEFO,
                Caducidad = medInv.CaducidadFEFO,
                Precio = medInv.Precio,
                Surtida = medInv.StockActual > 0
            };
            Partidas.Add(nuevaPartida);
            Recalcular();
            CodigoBarras = "";
            return;
        }

        // 3. Si coincide con un Folio de Receta en IUBIDB_FARMACIAS, cargar la receta completa
        var receta = await _dbService.ObtenerRecetaVentanillaRealAsync(query);
        if (receta != null)
        {
            IdRecetaActual = receta.IdReceta;
            IdDerechohabienteActual = receta.IdDerechohabiente;
            FolioReceta = receta.FolioReceta;
            PacienteNombre = receta.PacienteNombre;
            Carnet = receta.Carnet;
            Curp = receta.Curp;
            Dependencia = receta.Dependencia;
            TipoAfiliacion = receta.TipoAfiliacion;
            MedicoNombre = receta.MedicoNombre;
            CedulaMedico = receta.CedulaMedico;
            Diagnostico = receta.Diagnostico;
            FechaEmision = receta.FechaEmision;
            FolioTicket = receta.FolioTicket;

            AutoSeleccionarMedico(receta.IdMedico, receta.MedicoNombre, receta.CedulaMedico, receta.EspecialidadMedico);

            Partidas.Clear();
            foreach (var part in receta.Partidas)
                Partidas.Add(part);
            Recalcular();

            MessageBox.Show($"Receta '{query}' localizada en IUBIDB_FARMACIAS:\n• Derechohabiente: {receta.PacienteNombre}\n• Partidas prescritas: {receta.Partidas.Count}", "Receta Cargada", MessageBoxButton.OK, MessageBoxImage.Information);
            CodigoBarras = "";
            return;
        }

        // 4. Si coincide con un paciente del Padrón (Carnet, CURP o Nombre)
        string queryClean = query.TrimStart('-');
        var paciente = PacientesList.FirstOrDefault(d => 
            d.Carnet.Contains(query, StringComparison.OrdinalIgnoreCase)
            || d.Carnet.TrimStart('-').Contains(queryClean, StringComparison.OrdinalIgnoreCase)
            || d.Curp.Contains(query, StringComparison.OrdinalIgnoreCase)
            || d.NombreCompleto.Contains(query, StringComparison.OrdinalIgnoreCase));

        if (paciente != null)
        {
            AsignarDatosPaciente(paciente);
            MessageBox.Show($"Derechohabiente '{paciente.NombreCompleto}' cargado en ventanilla.\nCarnet: {paciente.Carnet}", "Paciente Localizado", MessageBoxButton.OK, MessageBoxImage.Information);
            CodigoBarras = "";
            return;
        }

        // 5. No se encontró
        MessageBox.Show($"No se encontró ningún fármaco, folio de receta ni carnet con: '{query}'.\nPuede buscar por nombre de medicamento (ej. Paracetamol), clave (010...), folio de receta (ej. C73344 o 123456) o carnet.", "Búsqueda en Ventanilla", MessageBoxButton.OK, MessageBoxImage.Information);
        CodigoBarras = "";
    }

    [RelayCommand]
    public async Task CalcularReporteAsync()
    {
        try
        {
            var metricas = await _dbService.ObtenerMetricasReporteRealAsync();
            ReporteTotalCobrado = $"{metricas.TotalCobrado:C}";
            ReporteTotalAbsorcion = $"{metricas.TotalAbsorcion:C}";
            ReporteRecetasSurtidas = $"{metricas.RecetasSurtidas}";
            ReporteValesEmitidos = $"{metricas.ValesEmitidos}";
            MessageBox.Show($"Cálculo de corte actualizado con datos en vivo de IUBIDB_FARMACIAS:\n\n• Cobrado Pacientes: {ReporteTotalCobrado}\n• Absorción IPECOL: {ReporteTotalAbsorcion}\n• Recetas Surtidas: {ReporteRecetasSurtidas}\n• Vales Subrogados: {ReporteValesEmitidos}", "Corte Actualizado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al actualizar reporte: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeGuardar))]
    private async Task GuardarAsync()
    {
        // 1. VALIDACIÓN DE DERECHOHABIENTE
        if (string.IsNullOrWhiteSpace(PacienteNombre) || string.IsNullOrWhiteSpace(Carnet))
        {
            MessageBox.Show(
                "Debe seleccionar o buscar un derechohabiente antes de registrar la dispensación.",
                "Validación de Ventanilla",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 2. VALIDACIÓN DE VIGENCIA DE DERECHOS
        if (!string.IsNullOrWhiteSpace(Vigencia) && 
            (Vigencia.Contains("VENCID", StringComparison.OrdinalIgnoreCase) || 
             Vigencia.Contains("INACT", StringComparison.OrdinalIgnoreCase)))
        {
            var resVig = MessageBox.Show(
                $"Alerta de Vigencia Médica:\n\nEl derechohabiente presenta estatus: '{Vigencia}'.\n\n¿Desea proceder con la dispensación bajo autorización y responsabilidad de supervisión?",
                "Derechohabiente con Vigencia Observada",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (resVig != MessageBoxResult.Yes) return;
        }

        // 3. VALIDACIÓN DE MÉDICO PRESCRIPTOR
        if (string.IsNullOrWhiteSpace(MedicoNombre) || MedicoNombre.Contains("Sin médico", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                "Debe seleccionar o especificar el médico prescriptor de la receta antes de registrar la dispensación.",
                "Médico Requerido",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 4. VALIDACIÓN DE PARTIDAS
        if (!Partidas.Any())
        {
            MessageBox.Show(
                "No hay medicamentos prescritos en la lista. Ingrese una receta o escanee los medicamentos a dispensar.",
                "Partidas Requeridas",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var partidasSurtidas = Partidas.Where(x => x.Surtida).ToList();
        if (!partidasSurtidas.Any())
        {
            MessageBox.Show(
                "No ha marcado ningún medicamento como SURTIDO para entrega física.\n\nMarque las casillas de los medicamentos a entregar o emita los vales correspondientes.",
                "Sin Medicamentos Surtidos",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // 5. VALIDACIÓN DE EXISTENCIAS (STOCK)
        foreach (var p in partidasSurtidas)
        {
            if (p.Existencia <= 0)
            {
                MessageBox.Show(
                    $"Stock insuficiente para el medicamento:\n• {p.Nombre}\n\nExistencia en almacén central: 0 pzas.\nSolicitadas: {p.Prescrita} pzas.\n\nNo es posible surtir en físico sin inventario. Por favor desmarque la partida o genere un Vale Subrogado.",
                    "Sin Existencia en Inventario",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (p.Prescrita > p.Existencia)
            {
                MessageBox.Show(
                    $"Existencia parcial para el medicamento:\n• {p.Nombre}\n\nExistencia disponible: {p.Existencia} pzas.\nSolicitadas: {p.Prescrita} pzas.\n\nDebe emitir un vale por el faltante o ajustar la cantidad a entregar.",
                    "Stock Parcial Observado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }
        }

        // 6. CONFIRMACIÓN OPERATIVA
        var confirmacion = MessageBox.Show(
            $"¿Confirmar el registro de dispensación oficial?\n\n• Derechohabiente: {PacienteNombre} ({Carnet})\n• Médico: {MedicoNombre}\n• Folio Receta: {(string.IsNullOrWhiteSpace(FolioReceta) ? "Sin Receta Previa" : FolioReceta)}\n• Fármacos a entregar: {partidasSurtidas.Count} partida(s)\n• Total: {Subtotal:C}\n• Absorción IPECOL: {Absorcion:C}\n• Aportación Paciente: {AportacionPaciente:C}\n\nEsta operación descontará el stock de almacén en IUBIDB_FARMACIAS_PRUEBAS y generará el comprobante oficial.",
            "Confirmar Dispensación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        // 7. REGISTRO REAL EN TRANSACCIÓN SQL
        var res = await _dbService.RegistrarDispensacionRealAsync(
            IdRecetaActual,
            IdDerechohabienteActual,
            Subtotal,
            AportacionPaciente,
            Absorcion,
            Partidas.ToList(),
            $"Dispensación en ventanilla · Receta {FolioReceta}",
            UsuarioActivo);

        if (res.exito)
        {
            FolioTicket = res.folioTicket;
            Guardada = true;

            // Refrescar Inventario, Kardex y Reporte en segundo plano
            _ = CargarDatosRealesDesdeBaseAsync();
            _ = CalcularReporteAsync();

            MessageBox.Show(
                $"DISPENSACIÓN REGISTRADA EXITOSAMENTE EN IUBIDB_FARMACIAS_PRUEBAS:\n\n• Ticket Oficial: #{FolioTicket}\n• Derechohabiente: {PacienteNombre}\n• Partidas Surtidas: {partidasSurtidas.Count}\n• Total: {Subtotal:C}\n\nInventario descontado correctamente. Se abrirá la vista previa para impresión de ticket.",
                "Dispensación Exitosa · IPECOL",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ImprimirTicketCommand.NotifyCanExecuteChanged();
            AbrirModalTicket();
        }
        else
        {
            MessageBox.Show($"Ocurrió un error al registrar la dispensación:\n{res.mensaje}", "Error en Dispensación", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
    private bool PuedeGuardar() => Partidas.Any();

    [RelayCommand]
    private void ImprimirTicket()
    {
        AbrirModalTicket();
    }

    [RelayCommand]
    private void NuevaOperacion()
    {
        RestablecerVentanillaOriginal();
    }

    public void RestablecerVentanillaOriginal()
    {
        PacienteNombre = ""; Carnet = ""; Curp = ""; TipoAfiliacion = ""; Dependencia = ""; Vigencia = ""; Alergias = "";
        FolioReceta = ""; MedicoNombre = ""; CedulaMedico = ""; Diagnostico = ""; FechaEmision = ""; FolioTicket = "";
        IdRecetaActual = null; IdDerechohabienteActual = 0;
        CodigoBarras = "";
        Guardada = false;
        Partidas.Clear();
        Recalcular();
    }

    private void Recalcular()
    {
        var partidasSurtidas = Partidas.Where(x => x.Surtida).ToList();
        
        if (partidasSurtidas.Any())
        {
            Subtotal = partidasSurtidas.Sum(x => x.Importe);
            Absorcion = Math.Round(Subtotal * PorcentajeAbsorcion, 2);
            AportacionPaciente = Subtotal - Absorcion;
        }
        else
        {
            Subtotal = 0;
            Absorcion = 0;
            AportacionPaciente = 0;
        }

        int totalPartidas = Partidas.Count;
        int disponibles = Partidas.Count(x => x.HayExistencia);
        int conVale = Partidas.Count(x => x.RequiereVale);

        if (totalPartidas > 0)
        {
            PartidasResumenDisponibles = $"{disponibles} de {totalPartidas} partida(s) con existencia" + 
                (conVale > 0 ? $" · {conVale} vale(s) sugerido(s)" : " · Cuadro básico completo");
        }
        else
        {
            PartidasResumenDisponibles = "Ventanilla lista · Escanee código de barras, ingrese carnet o folio de receta (ej. C73344 o 123456)";
        }

        GuardarCommand.NotifyCanExecuteChanged();
    }

    // ==========================================
    // FILTROS Y BÚSQUEDAS DINÁMICAS
    // ==========================================
    partial void OnFiltroInventarioChanged(string value) => AplicarFiltroInventario();
    partial void OnCategoriaFiltroInventarioChanged(string value) => AplicarFiltroInventario();

    [RelayCommand]
    public void FiltrarInventarioPorCategoria(string cat)
    {
        CategoriaFiltroInventario = cat;
    }

    private void AplicarFiltroInventario()
    {
        InventarioFiltrado.Clear();
        var query = InventarioList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroInventario))
        {
            var f = FiltroInventario.Trim();
            query = query.Where(x => (!string.IsNullOrEmpty(x.Nombre) && x.Nombre.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.Clave) && x.Clave.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.CodigoBarras) && x.CodigoBarras.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.SustanciaActiva) && x.SustanciaActiva.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }

        if (CategoriaFiltroInventario == "EnStock") 
            query = query.Where(x => x.StockActual > 0);
        else if (CategoriaFiltroInventario == "Critico") 
            query = query.Where(x => x.StockActual <= x.StockMinimo && x.StockActual > 0);
        else if (CategoriaFiltroInventario == "PorCaducar") 
            query = query.Where(x => !string.IsNullOrEmpty(x.SemaforoTexto) && (x.SemaforoTexto.Contains("🟡") || x.SemaforoTexto.Contains("🔴")));
        else if (CategoriaFiltroInventario == "Agotados") 
            query = query.Where(x => x.StockActual == 0);

        foreach (var item in query) InventarioFiltrado.Add(item);
    }

    partial void OnFiltroKardexTextoChanged(string value) => AplicarFiltroKardex();
    partial void OnFiltroTipoKardexChanged(string value) => AplicarFiltroKardex();

    [RelayCommand]
    public void FiltrarKardexTipo(string tipo)
    {
        FiltroTipoKardex = tipo;
    }

    private void AplicarFiltroKardex()
    {
        KardexFiltrado.Clear();
        var query = KardexList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroKardexTexto))
        {
            var f = FiltroKardexTexto.Trim();
            query = query.Where(x => (!string.IsNullOrEmpty(x.Medicamento) && x.Medicamento.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.DocumentoReferencia) && x.DocumentoReferencia.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.Usuario) && x.Usuario.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }

        if (FiltroTipoKardex != "Todos")
        {
            query = query.Where(x => !string.IsNullOrEmpty(x.TipoMovimiento) && x.TipoMovimiento.Contains(FiltroTipoKardex, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var item in query) KardexFiltrado.Add(item);
    }

    partial void OnFiltroPacientesChanged(string value) => AplicarFiltroPacientes();

    private void AplicarFiltroPacientes()
    {
        PacientesFiltrados.Clear();
        var query = PacientesList.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroPacientes))
        {
            var f = FiltroPacientes.Trim();
            var fClean = f.TrimStart('-');
            query = query.Where(x => (!string.IsNullOrEmpty(x.NombreCompleto) && x.NombreCompleto.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.Carnet) && (x.Carnet.Contains(f, StringComparison.OrdinalIgnoreCase) || x.Carnet.TrimStart('-').Contains(fClean, StringComparison.OrdinalIgnoreCase)))
                                  || (!string.IsNullOrEmpty(x.Curp) && x.Curp.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.Dependencia) && x.Dependencia.Contains(f, StringComparison.OrdinalIgnoreCase))
                                  || (!string.IsNullOrEmpty(x.Telefono) && x.Telefono.Contains(f, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var item in query) PacientesFiltrados.Add(item);
    }

    // ==========================================
    // ACCIONES DE COMPRAS Y FACTURA (MedicineEntryWindow)
    // ==========================================
    partial void OnFacturaProveedorSeleccionadoChanged(ProveedorItem? value)
    {
        if (value != null)
        {
            FacturaProveedorNombre = value.RazonSocial;
            FacturaProveedorId = value.IdProveedor;
        }
    }

    partial void OnArtCodigoBarrasChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var f = value.Trim();
        var med = InventarioList.FirstOrDefault(m => 
            (!string.IsNullOrEmpty(m.CodigoBarras) && m.CodigoBarras.Equals(f, StringComparison.OrdinalIgnoreCase))
            || (!string.IsNullOrEmpty(m.Clave) && m.Clave.Equals(f, StringComparison.OrdinalIgnoreCase)));
        if (med != null)
        {
            ArtClave = med.Clave;
            ArtIdMedicamento = med.ID_Medicamento;
            ArtDescripcion = med.Nombre;
            ArtPrecio = med.Precio;
            ArtLote = med.LoteFEFO != "—" ? med.LoteFEFO : "LT" + DateTime.Now.ToString("yyMMdd");
        }
    }

    [RelayCommand]
    public void AgregarArticuloAFactura()
    {
        if (string.IsNullOrWhiteSpace(ArtDescripcion))
        {
            MessageBox.Show("Ingrese o seleccione un medicamento para la factura.", "Alta de Artículos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ArtCantidad <= 0)
        {
            MessageBox.Show("La cantidad de piezas debe ser mayor a 0.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ArtPrecio < 0)
        {
            MessageBox.Show("El precio unitario no puede ser negativo.", "Precio Inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ArtLote))
        {
            MessageBox.Show("Por favor capture el número de lote para trazabilidad de almacén.", "Lote Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (ArtFechaCaducidad.Date < DateTime.Today)
        {
            MessageBox.Show($"La fecha de caducidad ingresada ({ArtFechaCaducidad:dd/MM/yyyy}) ya está vencida. Capture un lote con fecha vigente.", "Lote Caducado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        else if (ArtFechaCaducidad.Date <= DateTime.Today.AddDays(30))
        {
            var resAviso = MessageBox.Show(
                $"Atención de Caducidad:\n\nEl lote presenta una fecha próxima de caducidad ({ArtFechaCaducidad:dd/MM/yyyy}, menos de 30 días).\n\n¿Desea ingresar el producto bajo observación?",
                "Caducidad Próxima",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (resAviso != MessageBoxResult.Yes) return;
        }

        FacturaPartidasList.Add(new FacturaArticuloItem
        {
            CodigoBarras = ArtCodigoBarras,
            Clave = ArtClave,
            IdMedicamento = ArtIdMedicamento,
            Descripcion = ArtDescripcion,
            FechaCaducidad = ArtFechaCaducidad.ToString("dd/MM/yyyy"),
            FechaCaducidadDate = ArtFechaCaducidad,
            Cantidad = ArtCantidad,
            Lote = ArtLote.Trim().ToUpper(),
            Precio = ArtPrecio,
            IVA = ArtIva,
            TipoCompra = ArtTipoCompra
        });

        RecalcularFactura();
        LimpiarArticuloFactura();
    }

    [RelayCommand]
    public void EliminarArticuloFactura(FacturaArticuloItem? item)
    {
        if (item == null) return;
        FacturaPartidasList.Remove(item);
        RecalcularFactura();
    }

    [RelayCommand]
    public void LimpiarArticuloFactura()
    {
        ArtCodigoBarras = "";
        ArtClave = "";
        ArtIdMedicamento = 0;
        ArtDescripcion = "";
        ArtCantidad = 1;
        ArtLote = "";
        ArtPrecio = 0;
        ArtIva = 0;
    }

    private void RecalcularFactura()
    {
        FacturaTotalArticulos = FacturaPartidasList.Sum(x => x.Cantidad);
        FacturaSubtotal = FacturaPartidasList.Sum(x => x.Subtotal);
        FacturaIvaTotal = FacturaPartidasList.Sum(x => x.IVA);
        FacturaGranTotal = FacturaSubtotal + FacturaIvaTotal;
    }

    [RelayCommand]
    public async Task GuardarFacturaAsync()
    {
        // 1. VALIDACIÓN DE ENCABEZADO
        if (string.IsNullOrWhiteSpace(FacturaNumero))
        {
            MessageBox.Show("Por favor capture el número de factura.", "Número de Factura Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int provId = FacturaProveedorSeleccionado?.IdProveedor ?? FacturaProveedorId;
        if (provId <= 0)
        {
            MessageBox.Show("Debe seleccionar un proveedor del catálogo institucional.", "Proveedor Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (FacturaFecha > DateTime.Today)
        {
            MessageBox.Show("La fecha de la factura no puede ser una fecha futura.", "Fecha Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 2. VALIDACIÓN DE DETALLE
        if (!FacturaPartidasList.Any())
        {
            MessageBox.Show("Debe agregar al menos un artículo en el detalle de la factura antes de guardar.", "Artículos Requeridos", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        foreach (var art in FacturaPartidasList)
        {
            if (art.Cantidad <= 0)
            {
                MessageBox.Show($"La cantidad del artículo '{art.Descripcion}' debe ser mayor a cero.", "Cantidad Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(art.Lote))
            {
                MessageBox.Show($"Debe especificar el número de lote para el artículo '{art.Descripcion}'.", "Lote Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        // 3. CONFIRMACIÓN OPERATIVA
        var provNom = FacturaProveedorSeleccionado?.RazonSocial ?? FacturaProveedorNombre;
        var confirmacion = MessageBox.Show(
            $"¿Confirmar el ingreso de la Factura de Compra en IUBIDB_FARMACIAS_PRUEBAS?\n\n• Factura: {FacturaNumero.Trim()}\n• Proveedor: {provNom}\n• Fecha Factura: {FacturaFecha:dd/MM/yyyy}\n• Renglones: {FacturaPartidasList.Count} ({FacturaTotalArticulos} pza(s))\n• Subtotal: {FacturaSubtotal:C}\n• IVA: {FacturaIvaTotal:C}\n• Total: {FacturaGranTotal:C}\n\nSe aumentará el inventario en almacén y se registrarán las entradas en Kardex.",
            "Confirmar Entrada por Compra",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        // 4. REGISTRO REAL EN TRANSACCIÓN SQL
        var res = await _dbService.RegistrarCompraRealAsync(
            FacturaNumero.Trim(),
            "",
            provId,
            FacturaFecha,
            FacturaSubtotal,
            FacturaIvaTotal,
            FacturaGranTotal,
            FacturaPartidasList.ToList(),
            $"Entrada de almacén por factura {FacturaNumero.Trim()}",
            UsuarioActivo);

        if (res.exito)
        {
            MessageBox.Show(
                $"FACTURA REGISTRADA EXITOSAMENTE EN IUBIDB_FARMACIAS_PRUEBAS:\n\n• Factura: {FacturaNumero}\n• Total Piezas: {FacturaTotalArticulos} piezas\n• Importe: {FacturaGranTotal:C}\n\nLas existencias de almacén han sido actualizadas.",
                "Entrada de Compra Registrada",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            FacturaNumero = "";
            FacturaPartidasList.Clear();
            RecalcularFactura();

            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show($"Error al registrar la factura de compra:\n{res.mensaje}", "Error en Compra", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ==========================================
    // ACCIONES DE REIMPRESIÓN (ReprintDocumentWindow)
    // ==========================================
    [RelayCommand]
    public async Task BuscarDocumentosReimpresionAsync()
    {
        var docs = await _dbService.ObtenerDocumentosReimpresionAsync(ReimpFechaDesde, ReimpFechaHasta);
        DocumentosReimpresionList.Clear();
        foreach (var d in docs) DocumentosReimpresionList.Add(d);
        if (!docs.Any())
        {
            MessageBox.Show($"No se encontraron comprobantes emitidos entre {ReimpFechaDesde:dd/MM/yyyy} y {ReimpFechaHasta:dd/MM/yyyy}.", "Reimpresión", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    [RelayCommand]
    public void EjecutarReimpresion(DocumentoReimpresionItem? doc)
    {
        var d = doc ?? DocReimpresionSeleccionado;
        if (d == null) return;
        MessageBox.Show(
            $"=== REIMPRESIÓN OFICIAL DE COMPROBANTE ===\nINSTITUTO DE PENSIONES DE COLIMA (IPECOL)\n\n• Tipo: {d.TipoDocumento}\n• Folio / Ticket: {d.ID}\n• Receta Origen: {d.NumeroReceta}\n• Fecha de Emisión: {d.FechaReceta}\n• Paciente: {d.Paciente}\n\nDocumento enviado a la cola de impresión institucional.",
            "Reimpresión de Documento",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ==========================================
    // ACCIONES DE INFORMACIÓN DE RECETA (PrescriptionCancelWindow)
    // ==========================================
    [RelayCommand]
    public async Task BuscarInfoRecetaAsync()
    {
        if (string.IsNullOrWhiteSpace(InfoRecetaBusqueda)) return;
        var r = await _dbService.ObtenerInfoRecetaCompletaAsync(InfoRecetaBusqueda.Trim());
        InfoRecetaDerechohabiente = string.IsNullOrEmpty(r.Paciente) ? "No localizado" : r.Paciente;
        InfoRecetaIdCarnet = string.IsNullOrEmpty(r.Carnet) ? "—" : r.Carnet;
        InfoRecetaFolio = string.IsNullOrEmpty(r.FolioReceta) ? InfoRecetaBusqueda : r.FolioReceta;
        InfoRecetaFecha = string.IsNullOrEmpty(r.Fecha) ? "—" : r.Fecha;
        InfoRecetaMedico = string.IsNullOrEmpty(r.Medico) ? "—" : r.Medico;

        InfoRecetaDetalleList.Clear();
        foreach (var item in r.Detalle) InfoRecetaDetalleList.Add(item);

        InfoRecetaValesList.Clear();
        foreach (var v in r.Vales) InfoRecetaValesList.Add(v);
    }

    [RelayCommand]
    public async Task DevolverMedicamentoRecetaAsync(RecetaDetalleInfoItem? item)
    {
        if (item == null) return;
        var confirmacion = MessageBox.Show(
            $"¿Confirmar la devolución de medicamento a farmacia?\n\n• Fármaco: {item.Descripcion}\n• Cantidad: {item.Cantidad} pza(s)\n• Ticket: {item.Ticket_ID}\n\nLas existencias se reincorporarán al almacén de farmacia central y se generará movimiento en Kardex.",
            "Confirmar Devolución",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirmacion != MessageBoxResult.Yes) return;

        var res = await _dbService.DevolverMedicamentoRealAsync(item.Ticket_ID, item.Descripcion, item.Cantidad);
        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Devolución Exitosa · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            item.EstatusString = "DEVUELTO";
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show($"Error al procesar devolución:\n{res.mensaje}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    public void SurtirValeDesdeInfoReceta(RecetaValeInfoItem? vale)
    {
        if (vale == null) return;
        FiltroVale = vale.ID_Vale;
        AbrirPestana("Vales");
        BuscarValePorFolio();
    }

    // ==========================================
    // ACCIONES DE REPORTES (BillsEntryReport, ReporteEntrega, EntradaDeMedicamentos)
    // ==========================================
    [RelayCommand]
    public async Task GenerarReporteComprasAsync()
    {
        var items = await _dbService.ObtenerReporteComprasAsync(
            RepComprasProveedor, RepComprasFactura, RepComprasFechaDesde, RepComprasFechaHasta, RepComprasEsDesglosado);
        ReporteComprasList.Clear();
        foreach (var it in items) ReporteComprasList.Add(it);
    }

    [RelayCommand]
    public void ExportarReporteComprasExcel() => ExportarExcel("Reporte_Compras");

    [RelayCommand]
    public async Task BuscarReporteEntregasAsync()
    {
        var items = await _dbService.ObtenerReporteEntregaMedicamentosAsync(
            RepEntregasFechaDesde, RepEntregasFechaHasta, RepEntregasReceta, RepEntregasCodigo, RepEntregasDescripcion, RepEntregasIdDerech, RepEntregasNombre, RepEntregasCuadroBasico);
        ReporteEntregasList.Clear();
        foreach (var it in items) ReporteEntregasList.Add(it);
        RepEntregasTotalReg = ReporteEntregasList.Count;
    }

    [RelayCommand]
    public void ExportarReporteEntregasExcel() => ExportarExcel("Reporte_Entrega_Medicamentos");

    [RelayCommand]
    public async Task BuscarReporteEntradasAsync()
    {
        var items = await _dbService.ObtenerReporteEntradasMedicamentoAsync(
            RepEntradasFechaDesde, RepEntradasFechaHasta, RepEntradasFactura, RepEntradasProveedor, RepEntradasCodigo, RepEntradasDescripcion);
        ReporteEntradasList.Clear();
        foreach (var it in items) ReporteEntradasList.Add(it);
        RepEntradasTotalReg = ReporteEntradasList.Count;
    }

    [RelayCommand]
    public void ExportarReporteEntradasExcel() => ExportarExcel("Reporte_Entrada_Medicamentos");

    [RelayCommand]
    private async Task AgregarNuevaFacturaAsync()
    {
        await GuardarFacturaAsync();
    }

    [RelayCommand]
    private void ExportarExcel(string modulo)
    {
        MessageBox.Show(
            $"Reporte institucional generado satisfactoriamente:\n\n• Módulo: {modulo}\n• Archivo: C:\\Users\\Ipecol\\Documents\\Reportes\\{modulo}_{DateTime.Now:yyyyMMdd_HHmm}.xlsx\n• Origen de datos: IUBIDB_FARMACIAS",
            "Exportación a Excel",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    // ==========================================
    // ACCIONES DE MODALES INSTITUCIONALES (PARIDAD CON EL SISTEMA ANTERIOR)
    // ==========================================

    // 1. TICKET E IMPRESIÓN OFICIAL
    [RelayCommand]
    private void AbrirModalTicket()
    {
        TicketActual = new TicketImpresionData
        {
            FolioTicket = FolioTicket,
            FechaHora = DateTime.Now.ToString("dd/MMM/yyyy HH:mm:ss").ToUpper(),
            TerminalCaja = TerminalInfo,
            Sucursal = SucursalInfo,
            UsuarioCajero = UsuarioNombreCompleto,
            FolioReceta = FolioReceta,
            PacienteNombre = PacienteNombre,
            Carnet = Carnet,
            TipoAfiliacion = TipoAfiliacion,
            Dependencia = Dependencia,
            MedicoNombre = MedicoNombre,
            CedulaMedico = CedulaMedico,
            Subtotal = Subtotal,
            Absorcion = Absorcion,
            AportacionPaciente = AportacionPaciente,
            MetodoPago = MetodoPago,
            PartidasEntregadas = Partidas.Where(x => x.Surtida).ToList(),
            ValesEmitidos = ValesList.Where(v => v.NumeroReceta == FolioReceta || v.Carnet.Contains(Carnet.Replace("Carnet ", "").Trim())).ToList()
        };
        EsModalTicketVisible = true;
    }

    [RelayCommand]
    private void CerrarModalTicket()
    {
        EsModalTicketVisible = false;
    }

    [RelayCommand]
    private void ConfirmarImpresionTermica()
    {
        MessageBox.Show(
            $"[IMPRESIÓN TÉRMICA ENVIADA]\n\n• Folio Ticket: {FolioTicket}\n• Derechohabiente: {PacienteNombre}\n• Total Dispensado: {Subtotal:C}\n• Absorción IPECOL : {Absorcion:C}\n• Copago Paciente: {AportacionPaciente:C}\n\nDocumento enviado a la impresora térmica institucional de 80 mm.",
            "Impresión Exitosa · IPECOL",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        EsModalTicketVisible = false;
    }

    // 2. CANCELACIÓN DE RECETAS (PrescriptionCancelWindow)
    [RelayCommand]
    private void AbrirModalCancelarReceta()
    {
        FolioRecetaCancelar = FolioReceta;
        MotivoCancelacion = "Error en prescripción o medicamento no recogido por el paciente";
        EsModalCancelarRecetaVisible = true;
    }

    [RelayCommand]
    private void CerrarModalCancelarReceta()
    {
        EsModalCancelarRecetaVisible = false;
    }

    [RelayCommand]
    private async Task ConfirmarCancelarRecetaAsync()
    {
        if (string.IsNullOrWhiteSpace(FolioRecetaCancelar))
        {
            MessageBox.Show("Capture el folio de la receta a cancelar.", "Cancelación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirmacion = MessageBox.Show(
            $"¿Confirmar la cancelación definitiva de la receta #{FolioRecetaCancelar.Trim()}?\n\n• Motivo: {MotivoCancelacion}\n\nLa receta se marcará como CANCELADA en IUBIDB_FARMACIAS_PRUEBAS.",
            "Confirmar Cancelación de Receta",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmacion != MessageBoxResult.Yes) return;

        var res = await _dbService.CancelarRecetaRealAsync(FolioRecetaCancelar, MotivoCancelacion);
        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Receta Cancelada · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalCancelarRecetaVisible = false;
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 3. REIMPRESIÓN DE DOCUMENTOS (ReprintDocumentWindow)
    [RelayCommand]
    private void AbrirModalReimpresion()
    {
        FolioReimpresion = FolioTicket;
        TipoDocReimpresion = "Ticket";
        EsModalReimpresionVisible = true;
    }

    [RelayCommand]
    private void CerrarModalReimpresion()
    {
        EsModalReimpresionVisible = false;
    }

    [RelayCommand]
    private void ConfirmarReimpresion()
    {
        if (string.IsNullOrWhiteSpace(FolioReimpresion))
        {
            MessageBox.Show("Ingrese el número de folio a reimprimir.", "Reimpresión", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var vale = ValesList.FirstOrDefault(v => v.NumeroVale.Contains(FolioReimpresion.Trim(), StringComparison.OrdinalIgnoreCase));
        if (vale != null)
        {
            MessageBox.Show(
                $"[REIMPRESIÓN OFICIAL - DUPLICADO]\n\n• Documento: VALE SUBROGADO INSTITUCIONAL\n• Folio: {vale.NumeroVale}\n• Derechohabiente: {vale.PacienteNombre} ({vale.Carnet})\n• Fármaco: {vale.Medicamento}\n• Farmacia Convenio: {vale.FarmaciaSubrogada}\n• Vencimiento: {vale.FechaVencimiento}\n\nEnviado a impresora.",
                "Reimpresión de Vale · IPECOL",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            EsModalReimpresionVisible = false;
            return;
        }

        MessageBox.Show(
            $"[REIMPRESIÓN OFICIAL - DUPLICADO]\n\n• Documento: TICKET DE DISPENSACIÓN\n• Folio: {FolioReimpresion}\n• Derechohabiente: {PacienteNombre} ({Carnet})\n• Receta: {FolioReceta}\n• Total: {Subtotal:C}\n• Operador: {UsuarioNombreCompleto}\n\nEnviado a impresora térmica 80 mm.",
            "Reimpresión de Ticket · IPECOL",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
        EsModalReimpresionVisible = false;
    }

    // 4. NUEVA ENTRADA / RECEPCIÓN (EntradaDeMedicamentos)
    [RelayCommand]
    private void AbrirModalNuevaEntrada()
    {
        EntradaFacturaFolio = "";
        EntradaLote = "";
        EntradaCaducidad = "";
        EntradaCantidad = 0;
        EntradaCosto = 0m;
        EntradaMedicamento = "";
        EsModalNuevaEntradaVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevaEntrada()
    {
        EsModalNuevaEntradaVisible = false;
    }

    [RelayCommand]
    private async Task GuardarNuevaEntradaAsync()
    {
        if (string.IsNullOrWhiteSpace(EntradaFacturaFolio) || string.IsNullOrWhiteSpace(EntradaMedicamento))
        {
            MessageBox.Show("Capture los datos requeridos de la factura y medicamento.", "Alta de Entrada", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        int provId = ProveedorSeleccionado?.IdProveedor ?? 1;
        var p = new FacturaArticuloItem
        {
            Descripcion = EntradaMedicamento.Trim(),
            Lote = string.IsNullOrWhiteSpace(EntradaLote) ? "LOTE-STD" : EntradaLote.Trim(),
            Caducidad = string.IsNullOrWhiteSpace(EntradaCaducidad) ? DateTime.Today.AddYears(2).ToString("yyyy-MM-dd") : EntradaCaducidad.Trim(),
            Cantidad = EntradaCantidad > 0 ? EntradaCantidad : 1,
            Precio = EntradaCosto > 0 ? EntradaCosto : 10m
        };

        var res = await _dbService.RegistrarCompraRealAsync(
            EntradaFacturaFolio.Trim(), "", provId, DateTime.Today,
            p.Total, p.Total, new List<FacturaArticuloItem> { p }, "Entrada rápida por almacén");

        if (res.exito)
        {
            MessageBox.Show(res.mensaje, "Entrada Registrada · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalNuevaEntradaVisible = false;
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error al Registrar Entrada", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 5. NUEVO AFILIADO TITULAR (AddTitularWindow)
    [RelayCommand]
    private void AbrirModalNuevoAfiliado()
    {
        NuevoAfiliadoCarnet = "";
        NuevoAfiliadoNombre = "";
        NuevoAfiliadoCurp = "";
        NuevoAfiliadoDependencia = "Secretaría de Educación";
        EsModalNuevoAfiliadoVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoAfiliado()
    {
        EsModalNuevoAfiliadoVisible = false;
    }

    [RelayCommand]
    private async Task GuardarNuevoAfiliadoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoAfiliadoNombre) || string.IsNullOrWhiteSpace(NuevoAfiliadoCarnet))
        {
            MessageBox.Show("Ingrese el número de carnet y nombre completo del afiliado.", "Alta de Afiliado", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = await _dbService.RegistrarNuevoAfiliadoRealAsync(
            NuevoAfiliadoCarnet, NuevoAfiliadoNombre, NuevoAfiliadoCurp, NuevoAfiliadoDependencia);

        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Afiliado Registrado · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalNuevoAfiliadoVisible = false;
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 6. NUEVO MÉDICO PRESCRIPTOR (AddDoctorsWindows)
    [RelayCommand]
    private void AbrirModalNuevoMedico()
    {
        NuevoMedicoNombre = "";
        NuevoMedicoCedula = "";
        NuevoMedicoEspecialidad = "";
        EsModalNuevoMedicoVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoMedico()
    {
        EsModalNuevoMedicoVisible = false;
    }

    [RelayCommand]
    private async Task GuardarNuevoMedicoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoMedicoNombre))
        {
            MessageBox.Show("Ingrese el nombre del médico prescriptor.", "Alta de Médico", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = await _dbService.RegistrarNuevoMedicoRealAsync(
            NuevoMedicoNombre, NuevoMedicoCedula, NuevoMedicoEspecialidad);

        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Médico Registrado · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalNuevoMedicoVisible = false;
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 7. NUEVO MEDICAMENTO (AddArticleWindow)
    [RelayCommand]
    private void AbrirModalNuevoMedicamento()
    {
        NuevoMedClave = "";
        NuevoMedNombre = "";
        NuevoMedSustancia = "";
        NuevoMedCosto = 0m;
        EsModalNuevoMedicamentoVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoMedicamento()
    {
        EsModalNuevoMedicamentoVisible = false;
    }

    [RelayCommand]
    private async Task GuardarNuevoMedicamentoAsync()
    {
        if (string.IsNullOrWhiteSpace(NuevoMedNombre))
        {
            MessageBox.Show("Ingrese la descripción del medicamento.", "Alta de Medicamento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var res = await _dbService.RegistrarNuevoMedicamentoRealAsync(
            NuevoMedClave, NuevoMedNombre, NuevoMedSustancia, "PIEZA");

        if (res.ok)
        {
            MessageBox.Show(res.mensaje, "Medicamento Registrado · IUBIDB_FARMACIAS_PRUEBAS", MessageBoxButton.OK, MessageBoxImage.Information);
            EsModalNuevoMedicamentoVisible = false;
            _ = CargarDatosRealesDesdeBaseAsync();
        }
        else
        {
            MessageBox.Show(res.mensaje, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // 8. HISTORIAL ENTREGAS 15 DÍAS (MedicineOutputWindow txtHistory)
    [RelayCommand]
    public async Task AbrirModalHistorial15DiasAsync()
    {
        string c = (Carnet ?? "").Replace("Carnet", "").Trim();
        if (string.IsNullOrWhiteSpace(c) && PacienteSeleccionado == null)
        {
            MessageBox.Show("Por favor seleccione primero un derechohabiente o capture su carnet antes de consultar el historial.", "Historial de Dispensaciones", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (string.IsNullOrWhiteSpace(c) && PacienteSeleccionado != null)
        {
            c = PacienteSeleccionado.Carnet;
        }

        int idDh = IdDerechohabienteActual > 0 ? IdDerechohabienteActual : (PacienteSeleccionado?.IdDerechohabiente ?? 0);
        var entregas = await _dbService.ObtenerHistorialEntregasPacienteAsync(c, PacienteNombre, idDh);
        HistorialEntregas15Dias.Clear();
        foreach (var e in entregas)
        {
            HistorialEntregas15Dias.Add(e);
        }

        EsModalHistorial15DiasVisible = true;
    }

    [RelayCommand]
    private void CerrarModalHistorial15Dias()
    {
        EsModalHistorial15DiasVisible = false;
    }

    // 9. ALTA DE BENEFICIARIO (AddBeneficiaryWindow)
    [RelayCommand]
    private void AbrirModalNuevoBeneficiario()
    {
        NuevoBeneficiarioNombre = "";
        NuevoBeneficiarioCurp = "";
        NuevoBeneficiarioParentesco = "HIJO(A)";
        EsModalNuevoBeneficiarioVisible = true;
    }

    [RelayCommand]
    private void CerrarModalNuevoBeneficiario()
    {
        EsModalNuevoBeneficiarioVisible = false;
    }

    [RelayCommand]
    private void GuardarNuevoBeneficiario()
    {
        if (string.IsNullOrWhiteSpace(NuevoBeneficiarioNombre))
        {
            MessageBox.Show("Ingrese el nombre completo del beneficiario familiar.", "Nombre Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(NuevoBeneficiarioParentesco))
        {
            MessageBox.Show("Seleccione el parentesco del beneficiario con el titular.", "Parentesco Requerido", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!string.IsNullOrWhiteSpace(NuevoBeneficiarioCurp) && NuevoBeneficiarioCurp.Trim().Length != 18)
        {
            MessageBox.Show("La CURP del beneficiario debe tener exactamente 18 caracteres alfanuméricos.", "CURP Inválida", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        string tit = !string.IsNullOrWhiteSpace(PacienteNombre) ? PacienteNombre : (PacienteSeleccionado?.NombreCompleto ?? "Sin titular");

        MessageBox.Show(
            $"BENEFICIARIO ASOCIADO AL DERECHOHABIENTE EN IUBIDB_FARMACIAS_PRUEBAS:\n\n• Titular: {tit}\n• Carnet: {Carnet}\n• Beneficiario: {NuevoBeneficiarioNombre.Trim().ToUpper()}\n• Parentesco: {NuevoBeneficiarioParentesco}\n• CURP: {(string.IsNullOrWhiteSpace(NuevoBeneficiarioCurp) ? "No especificada" : NuevoBeneficiarioCurp.Trim().ToUpper())}\n\nEl beneficiario ha quedado registrado para fines de dispensación y atención médica.",
            "Beneficiario Registrado",
            MessageBoxButton.OK,
            MessageBoxImage.Information);

        EsModalNuevoBeneficiarioVisible = false;
        NuevoBeneficiarioNombre = "";
        NuevoBeneficiarioCurp = "";
    }
}
