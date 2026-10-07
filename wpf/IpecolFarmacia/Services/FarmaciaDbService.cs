using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using IpecolFarmacia.Models;

namespace IpecolFarmacia.Services;

/// <summary>
/// Servicio de solo lectura (READ-ONLY) conectado directamente a IUBIDB_FARMACIAS en SQL Server.
/// Todos los datos mostrados provienen de la base; si no hay dato se devuelve vacío o "—", nunca valores inventados.
/// </summary>
public class FarmaciaDbService
{
    private const string ConnectionString = 
        "Server=10.10.24.247;Database=IUBIDB_FARMACIAS_PRUEBAS;User Id=ipecol;Password=Pensiones2020;TrustServerCertificate=True;Timeout=15;";

    /// <summary>Último error de base de datos (para mostrarlo en la interfaz en vez de ocultarlo).</summary>
    public static string? UltimoError { get; private set; }

    private static void RegistrarError(string origen, Exception ex)
    {
        UltimoError = $"{origen}: {ex.Message}";
        System.Diagnostics.Debug.WriteLine($"[FarmaciaDbService] {UltimoError}");
    }

    private static string S(IDataRecord r, int i) => r.IsDBNull(i) ? "" : Convert.ToString(r.GetValue(i))?.Trim() ?? "";
    private static int I(IDataRecord r, int i) => r.IsDBNull(i) ? 0 : Convert.ToInt32(r.GetValue(i));
    private static decimal M(IDataRecord r, int i) => r.IsDBNull(i) ? 0m : Convert.ToDecimal(r.GetValue(i));
    private static DateTime? F(IDataRecord r, int i) => r.IsDBNull(i) ? null : Convert.ToDateTime(r.GetValue(i));

    // Fragmento SQL: nombre completo de una persona
    private const string NombrePersona =
        "LTRIM(RTRIM(ISNULL({0}.nombres,'') + ' ' + ISNULL({0}.primer_apellido,'') + ' ' + ISNULL({0}.segundo_apellido,'')))";

    // ==========================================
    // 1. INVENTARIO & LOTES FEFO
    // ==========================================
    public async Task<List<MedicamentoInventarioItem>> ObtenerInventarioRealAsync()
    {
        var lista = new List<MedicamentoInventarioItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                WITH ex AS (
                    SELECT IdMedicamento, SUM(CantidadExistente) AS Stock
                    FROM Frm_InventarioExistencias GROUP BY IdMedicamento),
                hoy AS (
                    SELECT dd.IdMedicamento, SUM(dd.Cantidad) AS Surtidos
                    FROM Frm_DispensacionesDetalle dd
                    JOIN Frm_Dispensaciones d ON d.IdDispensacion = dd.IdDispensacion
                    WHERE d.FechaHora >= CAST(GETDATE() AS date)
                    GROUP BY dd.IdMedicamento)
                SELECT TOP 600
                    m.IdMedicamento,
                    ISNULL(m.Clave,''), ISNULL(m.CodigoBarras,''),
                    LTRIM(RTRIM(m.Descripcion)), ISNULL(m.Presentacion,''),
                    ISNULL(m.ValorInstitucional,0), ISNULL(lab.Nombre,''),
                    ISNULL(m.InventarioMinimo,0), ISNULL(m.InventarioMaximo,0),
                    ISNULL(ex.Stock,0), ISNULL(m.TipoControl,'LIBRE'),
                    (SELECT STRING_AGG(sa.Nombre, ' + ')
                       FROM Frm_Medicamento_Sustancia ms
                       JOIN SisFrm_CatSustanciasActivas sa ON sa.IdSustancia = ms.IdSustancia
                      WHERE ms.IdMedicamento = m.IdMedicamento),
                    CASE WHEN EXISTS (SELECT 1 FROM Frm_Medicamento_Cuadro c
                                      WHERE c.IdMedicamento = m.IdMedicamento AND c.activo = 1) THEN 1 ELSE 0 END,
                    fe.NumeroLote, fe.FechaCaducidad,
                    ISNULL(hoy.Surtidos,0)
                FROM Frm_Medicamentos m
                LEFT JOIN ex ON ex.IdMedicamento = m.IdMedicamento
                LEFT JOIN hoy ON hoy.IdMedicamento = m.IdMedicamento
                LEFT JOIN SisFrm_CatLaboratorios lab ON lab.IdLaboratorio = m.IdLaboratorio
                OUTER APPLY (
                    SELECT TOP 1 l.NumeroLote, l.FechaCaducidad
                    FROM Frm_InventarioExistencias e
                    JOIN Frm_Lotes l ON l.IdLote = e.IdLote
                    WHERE e.IdMedicamento = m.IdMedicamento AND e.CantidadExistente > 0
                    ORDER BY l.FechaCaducidad) fe
                WHERE m.activo = 1 AND LEN(LTRIM(RTRIM(m.Descripcion))) > 2
                ORDER BY ISNULL(ex.Stock,0) DESC, m.Descripcion";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                int id = I(r, 0);
                string clave = S(r, 1);
                int stock = I(r, 9);
                int stockMin = I(r, 7);
                string tipoControl = S(r, 10);
                string lote = S(r, 13);
                DateTime? cad = F(r, 14);

                string semaforoColor = "#38A169";
                string semaforoTexto = "🟢 Vigente";
                string caducidadTexto = cad.HasValue ? cad.Value.ToString("MMM yyyy").ToUpper() : "—";

                if (stock <= 0)
                {
                    semaforoColor = "#E53E3E";
                    semaforoTexto = "🔴 Agotado · Subrogar";
                    lote = "—";
                    caducidadTexto = "—";
                }
                else if (cad.HasValue && cad.Value.Date < DateTime.Today)
                {
                    semaforoColor = "#E53E3E";
                    semaforoTexto = "🔴 Caducado";
                }
                else if (cad.HasValue && cad.Value.Date <= DateTime.Today.AddMonths(2))
                {
                    semaforoColor = "#E53E3E";
                    semaforoTexto = "🔴 Por Caducar (FEFO)";
                }
                else if (cad.HasValue && cad.Value.Date <= DateTime.Today.AddMonths(4))
                {
                    semaforoColor = "#DD6B20";
                    semaforoTexto = "🟡 Caducidad Media (FEFO)";
                }
                else if (stock <= stockMin)
                {
                    semaforoColor = "#DD6B20";
                    semaforoTexto = $"🟡 Stock Crítico ({stock} pzas)";
                }

                lista.Add(new MedicamentoInventarioItem
                {
                    ID_Medicamento = id,
                    Clave = clave,
                    CodigoBarras = S(r, 2),
                    Nombre = S(r, 3),
                    Presentacion = S(r, 4),
                    SustanciaActiva = S(r, 11),
                    Laboratorio = S(r, 6),
                    StockActual = stock,
                    StockMinimo = stockMin,
                    StockMaximo = I(r, 8),
                    Disponibles = stock,
                    Surtidos = I(r, 15),
                    Precio = M(r, 5),
                    LoteFEFO = string.IsNullOrEmpty(lote) ? "—" : lote,
                    CaducidadFEFO = caducidadTexto,
                    EsControlado = !tipoControl.Equals("LIBRE", StringComparison.OrdinalIgnoreCase) && tipoControl != "",
                    EsCuadroBasico = I(r, 12) == 1,
                    SemaforoColor = semaforoColor,
                    SemaforoTexto = semaforoTexto
                });
            }
        }
        catch (Exception ex) { RegistrarError("Inventario", ex); }

        return lista;
    }

    public async Task<MetricasInventarioData> ObtenerMetricasInventarioAsync()
    {
        var data = new MetricasInventarioData { TotalClaves = 0, ValorTotal = 0, StockCriticoCount = 0, AgotadosCount = 0 };
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = @"
                WITH ex AS (SELECT IdMedicamento, SUM(CantidadExistente) AS Stock
                            FROM Frm_InventarioExistencias GROUP BY IdMedicamento)
                SELECT
                    COUNT(*) AS Total,
                    ISNULL(SUM(ISNULL(ex.Stock,0) * ISNULL(m.ValorInstitucional,0)),0) AS Valor,
                    SUM(CASE WHEN ISNULL(ex.Stock,0) > 0 AND ex.Stock <= ISNULL(m.InventarioMinimo,0) THEN 1 ELSE 0 END) AS Critico,
                    SUM(CASE WHEN ISNULL(ex.Stock,0) <= 0 THEN 1 ELSE 0 END) AS Agotados
                FROM Frm_Medicamentos m
                LEFT JOIN ex ON ex.IdMedicamento = m.IdMedicamento
                WHERE m.activo = 1";
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                data.TotalClaves = I(r, 0);
                data.ValorTotal = M(r, 1);
                data.StockCriticoCount = I(r, 2);
                data.AgotadosCount = I(r, 3);
            }
        }
        catch (Exception ex) { RegistrarError("Métricas inventario", ex); }
        return data;
    }

    // ==========================================
    // 2. VALES SUBROGADOS (Frm_Vales + Frm_ValesDetalle)
    // ==========================================
    public async Task<List<ValeItem>> ObtenerValesRealesAsync()
    {
        var lista = new List<ValeItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = $@"
                SELECT TOP 1000
                    v.IdVale,
                    v.NumeroVale,
                    COALESCE(
                        NULLIF(NULLIF(dh_c.carnet,'0'),''),
                        NULLIF(NULLIF(dh.carnet,'0'),''),
                        af_c.no_afiliado,
                        af.no_afiliado,
                        CAST(v.Id_Derechohabiente AS nvarchar(30))
                    ) AS Carnet,
                    COALESCE(
                        NULLIF(r.Diagnostico,''),
                        NULLIF({string.Format(NombrePersona, "p_carnet")},''),
                        NULLIF({string.Format(NombrePersona, "pb")},''),
                        {string.Format(NombrePersona, "pt")}, ''
                    ) AS Paciente,
                    ISNULL(r.NumeroReceta,'') AS Receta,
                    ISNULL({string.Format(NombrePersona, "pm")},'') AS Medico,
                    v.FechaEmision, v.FechaVencimiento, v.Estatus AS ValeEstatus,
                    v.IdFarmaciaExternaConvenio,
                    vd.IdValeDetalle, vd.IdMedicamento,
                    ISNULL(m.CodigoBarras,'') AS CodigoBarras,
                    ISNULL(m.Descripcion,'') AS Medicamento,
                    vd.CantidadAutorizada, vd.CantidadDispensada,
                    ISNULL(vd.Estatus,'POR SURTIR') AS DetalleEstatus
                FROM Frm_Vales v
                JOIN Frm_ValesDetalle vd ON vd.IdVale = v.IdVale
                LEFT JOIN Frm_Medicamentos m ON m.IdMedicamento = vd.IdMedicamento
                LEFT JOIN Frm_Recetas r ON r.IdReceta = v.IdReceta
                LEFT JOIN SeM_Medicos med ON med.Id_Medico = r.Id_Medico
                LEFT JOIN Afi_Personas pm ON pm.Id_Persona = med.Id_persona
                LEFT JOIN SeM_Derechohabientes dh_c ON (
                    dh_c.carnet = CAST(v.Id_Derechohabiente AS nvarchar(30))
                    OR dh_c.carnet = '-' + CAST(v.Id_Derechohabiente AS nvarchar(30))
                    OR dh_c.carnet LIKE CAST(v.Id_Derechohabiente AS nvarchar(30)) + '-%'
                    OR dh_c.carnet LIKE '%-' + CAST(v.Id_Derechohabiente AS nvarchar(30))
                )
                LEFT JOIN Afi_Afiliados af_c ON af_c.Id_Afiliado = dh_c.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p1.* FROM Afi_RelAfiliadosPersonas rel1
                             JOIN Afi_Personas p1 ON p1.Id_Persona = rel1.Id_persona
                             WHERE rel1.Id_afiliado = dh_c.Id_afiliado) p_carnet
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = v.Id_Derechohabiente
                LEFT JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p2.* FROM Afi_RelAfiliadosPersonas rel2
                             JOIN Afi_Personas p2 ON p2.Id_Persona = rel2.Id_persona
                             WHERE rel2.Id_afiliado = dh.Id_afiliado) pt
                LEFT JOIN SeM_Beneficiarios b ON b.Id_Beneficiario = NULLIF(v.Id_Beneficiario,0)
                LEFT JOIN Afi_Personas pb ON pb.Id_Persona = b.Id_persona
                WHERE ISNULL(vd.Estatus,'') <> 'ELIMINADO'
                ORDER BY v.IdVale DESC, vd.IdValeDetalle";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();

            var dic = new Dictionary<long, ValeItem>();
            while (await r.ReadAsync())
            {
                long idVale = Convert.ToInt64(r.GetValue(0));
                string numVale = S(r, 1);
                string carnetVal = S(r, 2);
                if (carnetVal.StartsWith("-") && carnetVal.Length > 1) carnetVal = carnetVal.Substring(1);
                string paciente = S(r, 3);
                string receta = S(r, 4);
                string medico = S(r, 5);
                DateTime? emision = F(r, 6);
                DateTime? venc = F(r, 7);
                string estatusVale = S(r, 8);
                int convenio = I(r, 9);
                long idValeDetalle = Convert.ToInt64(r.GetValue(10));
                int idMed = I(r, 11);
                string codBarras = S(r, 12);
                string medDesc = S(r, 13);
                int autorizada = I(r, 14);
                int dispensada = I(r, 15);
                string detEstatus = S(r, 16);

                if (!dic.TryGetValue(idVale, out var vale))
                {
                    int diasRest = venc.HasValue ? Math.Max(0, (int)(venc.Value.Date - DateTime.Today).TotalDays) : 0;
                    vale = new ValeItem
                    {
                        IdVale = idVale,
                        NumeroVale = numVale,
                        Carnet = carnetVal,
                        PacienteNombre = paciente,
                        NumeroReceta = receta,
                        MedicoNombre = string.IsNullOrEmpty(medico) ? "—" : $"Dr(a). {medico}",
                        FechaEmision = emision?.ToString("dd/MMM/yyyy") ?? "—",
                        FechaVencimiento = venc?.ToString("dd/MMM/yyyy") ?? "—",
                        DiasRestantes = diasRest,
                        FarmaciaSubrogada = convenio > 0 ? $"Farmacia convenio #{convenio}" : "Sin convenio asignado",
                        Estatus = estatusVale.ToUpper()
                    };
                    dic[idVale] = vale;
                    lista.Add(vale);
                }

                var partida = new ValePartidaItem
                {
                    IdValeDetalle = idValeDetalle,
                    IdVale = idVale,
                    IdMedicamento = idMed,
                    CodigoBarras = codBarras,
                    Descripcion = medDesc,
                    CantidadAutorizada = autorizada,
                    CantidadSurtida = dispensada,
                    CantidadRestante = Math.Max(0, autorizada - dispensada),
                    Estatus = detEstatus.ToUpper(),
                    SeleccionadoParaEntrega = autorizada > dispensada
                };
                vale.Medicamentos.Add(partida);

                if (vale.Medicamentos.Count == 1)
                {
                    vale.CodigoBarras = codBarras;
                    vale.Medicamento = medDesc;
                    vale.CantidadAutorizada = autorizada;
                    vale.CantidadSurtida = dispensada;
                    vale.CantidadRestante = Math.Max(0, autorizada - dispensada);
                }
            }
        }
        catch (Exception ex) { RegistrarError("Vales", ex); }

        return lista;
    }

    public async Task<(bool exito, bool valeCompleto, string mensaje)> SurtirPartidasValeRealAsync(
        long idVale, List<(long idDetalle, int cantidadAEntregar)> entregas)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            int totalEntregadas = 0;
            foreach (var (idDet, cant) in entregas)
            {
                if (cant <= 0) continue;
                totalEntregadas += cant;

                const string sqlDet = @"
                    UPDATE Frm_ValesDetalle
                    SET CantidadDispensada = ISNULL(CantidadDispensada, 0) + @cant,
                        Estatus = CASE 
                            WHEN ISNULL(CantidadDispensada, 0) + @cant >= CantidadAutorizada THEN 'SURTIDO' 
                            ELSE 'ACTIVO' 
                        END,
                        fecha_modificacion = GETDATE()
                    WHERE IdValeDetalle = @idDet";
                await using var cmdDet = new SqlCommand(sqlDet, conn);
                cmdDet.Parameters.AddWithValue("@cant", cant);
                cmdDet.Parameters.AddWithValue("@idDet", idDet);
                await cmdDet.ExecuteNonQueryAsync();
            }

            // Verificar si aún quedan partidas por surtir en este vale
            const string sqlCheck = @"
                SELECT COUNT(*), ISNULL(SUM(CantidadAutorizada - CantidadDispensada), 0)
                FROM Frm_ValesDetalle
                WHERE IdVale = @idVale 
                  AND ISNULL(Estatus,'') NOT IN ('SURTIDO','ELIMINADO')
                  AND CantidadAutorizada > CantidadDispensada";
            int pendientes = 0;
            int piezasPendientes = 0;
            await using (var cmdCheck = new SqlCommand(sqlCheck, conn))
            {
                cmdCheck.Parameters.AddWithValue("@idVale", idVale);
                await using var rChk = await cmdCheck.ExecuteReaderAsync();
                if (await rChk.ReadAsync())
                {
                    pendientes = I(rChk, 0);
                    piezasPendientes = I(rChk, 1);
                }
            }

            bool completo = pendientes == 0;
            string nuevoEstatus = completo ? "SURTIDO" : "ACTIVO";

            const string sqlVale = @"
                UPDATE Frm_Vales
                SET Estatus = @est, fecha_modificacion = GETDATE()
                WHERE IdVale = @idVale;
                SELECT ISNULL(NumeroVale, CAST(@idVale AS nvarchar(30))) FROM Frm_Vales WHERE IdVale = @idVale;";
            string numVale = "";
            await using (var cmdVale = new SqlCommand(sqlVale, conn))
            {
                cmdVale.Parameters.AddWithValue("@est", nuevoEstatus);
                cmdVale.Parameters.AddWithValue("@idVale", idVale);
                object? resNum = await cmdVale.ExecuteScalarAsync();
                numVale = resNum?.ToString() ?? idVale.ToString();
            }

            return (true, completo, completo 
                ? $"Vale #{numVale} surtido en su totalidad con éxito. Estatus actualizado a 'SURTIDO'." 
                : $"Se entregaron {totalEntregadas} pieza(s). El vale #{numVale} continúa ACTIVO con {piezasPendientes} pieza(s) pendientes de entrega.");
        }
        catch (Exception ex)
        {
            RegistrarError("Surtir Partidas Vale", ex);
            return (false, false, ex.Message);
        }
    }

    public Task<(bool exito, bool valeCompleto, string mensaje)> SurtirPartidasValeRealAsync(
        long idVale, List<long> idsDetallesASurtir)
    {
        var entregas = idsDetallesASurtir.Select(id => (id, 1)).ToList();
        return SurtirPartidasValeRealAsync(idVale, entregas);
    }

    public async Task<(bool ok, string folioVale, string mensaje)> GenerarValeRealAsync(
        long idReceta, int idDerechohabiente, int idMedicamento, int cantidad, string folioReceta)
    {
        if (cantidad <= 0) cantidad = 1;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            long idVale = 0;
            string numVale = "";

            // 1. Buscar si ya existe un vale activo para esta receta
            if (idReceta > 0)
            {
                const string sqlExiste = "SELECT TOP 1 IdVale, NumeroVale FROM Frm_Vales WHERE IdReceta = @rx AND Estatus = 'ACTIVO' ORDER BY IdVale DESC";
                await using var cmdEx = new SqlCommand(sqlExiste, conn);
                cmdEx.Parameters.AddWithValue("@rx", idReceta);
                await using var rEx = await cmdEx.ExecuteReaderAsync();
                if (await rEx.ReadAsync())
                {
                    idVale = Convert.ToInt64(rEx.GetValue(0));
                    numVale = S(rEx, 1);
                }
            }

            // 2. Si no existe, crear un nuevo vale institucional en Frm_Vales
            if (idVale == 0)
            {
                numVale = string.IsNullOrWhiteSpace(folioReceta)
                    ? $"V-{DateTime.Now:yyMMddHHmmss}"
                    : (folioReceta.StartsWith("V-") ? folioReceta : $"V-{folioReceta}");

                const string sqlInsVale = @"
                    INSERT INTO Frm_Vales (
                        NumeroVale, Id_Sucursal, Id_Derechohabiente, Id_Beneficiario, IdReceta,
                        FechaEmision, FechaVencimiento, TipoVale, IdFarmaciaExternaConvenio,
                        Estatus, Id_estatus, fecha_creacion, Id_usuario_creacion
                    ) VALUES (
                        @numVale, 1, @idDh, NULL, @idReceta,
                        GETDATE(), DATEADD(day, 30, GETDATE()), 'VALE INTERNO', NULL,
                        'ACTIVO', 1, GETDATE(), 1
                    );
                    SELECT CAST(SCOPE_IDENTITY() AS bigint);";

                await using var cmdInsV = new SqlCommand(sqlInsVale, conn);
                cmdInsV.Parameters.AddWithValue("@numVale", numVale);
                cmdInsV.Parameters.AddWithValue("@idDh", idDerechohabiente > 0 ? idDerechohabiente : 1);
                cmdInsV.Parameters.AddWithValue("@idReceta", idReceta > 0 ? (object)idReceta : DBNull.Value);
                object? resId = await cmdInsV.ExecuteScalarAsync();
                idVale = Convert.ToInt64(resId);
            }

            // 3. Insertar la partida en Frm_ValesDetalle
            const string sqlInsDet = @"
                INSERT INTO Frm_ValesDetalle (
                    IdVale, IdRecetaDetalle, IdMedicamento,
                    CantidadAutorizada, CantidadDispensada,
                    Estatus, Id_estatus, fecha_creacion, Id_usuario_creacion
                ) VALUES (
                    @idVale, NULL, @idMed,
                    @cant, 0,
                    'POR SURTIR', 1, GETDATE(), 1
                );";

            await using var cmdDet = new SqlCommand(sqlInsDet, conn);
            cmdDet.Parameters.AddWithValue("@idVale", idVale);
            cmdDet.Parameters.AddWithValue("@idMed", idMedicamento > 0 ? idMedicamento : 1);
            cmdDet.Parameters.AddWithValue("@cant", cantidad);
            await cmdDet.ExecuteNonQueryAsync();

            return (true, numVale, $"Vale #{numVale} generado y registrado con éxito en IUBIDB_FARMACIAS_PRUEBAS ({cantidad} piezas autorizadas).");
        }
        catch (Exception ex)
        {
            RegistrarError("Generar Vale", ex);
            return (false, "", $"Error al generar vale: {ex.Message}");
        }
    }

    // ==========================================
    // 3. KARDEX AUDITABLE (Frm_MovimientosInventario)
    // ==========================================
    public async Task<List<MovimientoKardexItem>> ObtenerKardexRealAsync()
    {
        var lista = new List<MovimientoKardexItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT TOP 300
                    mv.FechaHora, tm.Descripcion, tm.Naturaleza,
                    ISNULL(m.Descripcion,''), ISNULL(l.NumeroLote,''),
                    mv.Cantidad, ISNULL(mv.ReferenciaTipo,''), ISNULL(CAST(mv.ReferenciaId AS nvarchar(30)),''),
                    ISNULL(u.usuario,'')
                FROM Frm_MovimientosInventario mv
                LEFT JOIN SisFrm_CatTiposMovimiento tm ON tm.IdTipoMovimiento = mv.IdTipoMovimiento
                LEFT JOIN Frm_Medicamentos m ON m.IdMedicamento = mv.IdMedicamento
                LEFT JOIN Frm_Lotes l ON l.IdLote = mv.IdLote
                LEFT JOIN Frm_Usuarios u ON u.Id_Usuario = mv.Id_usuario_creacion
                ORDER BY mv.FechaHora DESC, mv.IdMovimiento DESC";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                DateTime? fecha = F(r, 0);
                bool entrada = S(r, 2).Equals("ENTRADA", StringComparison.OrdinalIgnoreCase);
                int cant = Math.Abs(I(r, 5));
                string refTipo = S(r, 6);
                string refId = S(r, 7);

                lista.Add(new MovimientoKardexItem
                {
                    Fecha = fecha?.ToString("dd/MMM/yyyy HH:mm") ?? "—",
                    TipoMovimiento = S(r, 1).ToUpper(),
                    Medicamento = S(r, 3),
                    Lote = S(r, 4),
                    CantidadTexto = $"{(entrada ? "+" : "-")} {cant} pza(s)",
                    EsEntrada = entrada,
                    DocumentoReferencia = string.IsNullOrEmpty(refTipo) ? "—" : $"{refTipo} {refId}".Trim(),
                    Usuario = string.IsNullOrEmpty(S(r, 8)) ? "—" : S(r, 8)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Kardex", ex); }

        return lista;
    }

    // ==========================================
    // 4. COMPRAS Y FACTURAS (Frm_Compras + Frm_ComprasDetalle)
    // ==========================================
    public async Task<List<FacturaCompraItem>> ObtenerComprasRealesAsync()
    {
        var lista = new List<FacturaCompraItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = @"
                SELECT TOP 200
                    ISNULL(c.SerieFactura,'') + ISNULL(c.NumeroFactura,''),
                    ISNULL(p.RazonSocial,''), c.FechaRecepcion,
                    ISNULL((SELECT SUM(d.Cantidad) FROM Frm_ComprasDetalle d WHERE d.IdCompra = c.IdCompra),0),
                    ISNULL(c.Total,0), ISNULL(c.Estatus,''), ISNULL(c.Observaciones,'')
                FROM Frm_Compras c
                LEFT JOIN SisFrm_CatProveedores p ON p.IdProveedor = c.IdProveedor
                ORDER BY c.FechaRecepcion DESC, c.IdCompra DESC";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new FacturaCompraItem
                {
                    NumeroFactura = S(r, 0),
                    Proveedor = S(r, 1),
                    FechaRecepcion = F(r, 2)?.ToString("dd/MMM/yyyy") ?? "—",
                    PiezasTotales = I(r, 3),
                    Total = M(r, 4),
                    Estatus = S(r, 5).ToUpper(),
                    Observaciones = S(r, 6)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Compras", ex); }
        return lista;
    }

    // ==========================================
    // 5. PADRÓN DE DERECHOHABIENTES
    // ==========================================
    public async Task<List<DerechohabienteItem>> ObtenerDerechohabientesRealesAsync()
    {
        var lista = new List<DerechohabienteItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = $@"
                SELECT TOP 6000
                    ISNULL(NULLIF(NULLIF(dh.carnet,'0'),''), af.no_afiliado) AS Carnet,
                    {string.Format(NombrePersona, "p")} AS Nombre,
                    ISNULL(p.curp,''), ISNULL(dh.nombramiento,''),
                    ISNULL(af.Id_dependencia_actual,0),
                    dh.fecha_vencimiento,
                    ISNULL(NULLIF(p.celular,''), ISNULL(p.telefono,'')),
                    ISNULL(dh.Id_estatus,0),
                    dh.Id_Derechohabiente,
                    COALESCE(NULLIF(dep.nombre,''), NULLIF(dep.siglas,''), '') AS DependenciaNombre
                FROM SeM_Derechohabientes dh
                JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                LEFT JOIN Sis_CatDependencias dep ON dep.Id_Dependencia = af.Id_dependencia_actual
                OUTER APPLY (SELECT TOP 1 pp.* FROM Afi_RelAfiliadosPersonas rel
                             JOIN Afi_Personas pp ON pp.Id_Persona = rel.Id_persona
                             WHERE rel.Id_afiliado = dh.Id_afiliado) p
                WHERE p.Id_Persona IS NOT NULL
                ORDER BY Nombre";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();

            while (await r.ReadAsync())
            {
                DateTime? venc = F(r, 5);
                bool vigente = I(r, 7) == 1 && (!venc.HasValue || venc.Value.Date >= DateTime.Today);
                string vigTexto = venc.HasValue
                    ? (venc.Value.Date >= DateTime.Today ? $"Vigente al {venc.Value:dd MMM yyyy}" : $"Vencida el {venc.Value:dd MMM yyyy}")
                    : (I(r, 7) == 1 ? "Activo" : "Inactivo");
                int dep = I(r, 4);
                string depNombre = S(r, 9);
                string nomb = S(r, 3);

                string carnetStr = S(r, 0);
                if (carnetStr.StartsWith("-") && carnetStr.Length > 1) carnetStr = carnetStr.Substring(1);

                lista.Add(new DerechohabienteItem
                {
                    IdDerechohabiente = I(r, 8),
                    Carnet = carnetStr,
                    NombreCompleto = S(r, 1),
                    Curp = S(r, 2),
                    TipoAfiliacion = string.IsNullOrEmpty(nomb) ? "Titular" : nomb,
                    Dependencia = !string.IsNullOrWhiteSpace(depNombre) ? depNombre : (dep > 0 ? $"Dependencia {dep}" : "—"),
                    Vigencia = vigTexto,
                    Alergias = "",
                    DiagnosticoFrecuente = "",
                    Telefono = S(r, 6),
                    EsVigente = vigente
                });
            }
        }
        catch (Exception ex) { RegistrarError("Padrón", ex); }

        return lista;
    }

    public async Task<List<HistorialEntregaItem>> ObtenerHistorialEntregasPacienteAsync(
        string carnet, string nombrePaciente, int idDerechohabiente = 0)
    {
        var lista = new List<HistorialEntregaItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string carnetLimpio = (carnet ?? "").Replace("Carnet", "").Trim();
            if (carnetLimpio.StartsWith("-") && carnetLimpio.Length > 1) carnetLimpio = carnetLimpio.Substring(1);

            string sql = $@"
                SELECT TOP 30
                    d.FechaHora, ISNULL(r.NumeroReceta,''), ISNULL(m.Descripcion,''), dd.Cantidad,
                    ISNULL({string.Format(NombrePersona, "pm")},''),
                    ISNULL(c.NumeroTicket, CAST(d.NumeroDispensacion AS nvarchar(30)))
                FROM Frm_Dispensaciones d
                JOIN Frm_DispensacionesDetalle dd ON dd.IdDispensacion = d.IdDispensacion
                LEFT JOIN Frm_Medicamentos m ON m.IdMedicamento = dd.IdMedicamento
                LEFT JOIN Frm_Recetas r ON r.IdReceta = d.IdReceta
                LEFT JOIN SeM_Medicos med ON med.Id_Medico = r.Id_Medico
                LEFT JOIN Afi_Personas pm ON pm.Id_Persona = med.Id_persona
                LEFT JOIN Frm_ComprobantesDispensacion c ON c.IdDispensacion = d.IdDispensacion
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = d.Id_Derechohabiente
                LEFT JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                WHERE (@idDh > 0 AND d.Id_Derechohabiente = @idDh)
                   OR (@carnet <> '' AND (dh.carnet = @carnet OR af.no_afiliado = @carnet OR dh.carnet LIKE '%' + @carnet + '%' OR af.no_afiliado LIKE '%' + @carnet + '%'))
                ORDER BY d.FechaHora DESC, dd.IdDispensacionDetalle";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            cmd.Parameters.AddWithValue("@idDh", idDerechohabiente);
            cmd.Parameters.AddWithValue("@carnet", carnetLimpio);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                int cant = I(r, 3);
                lista.Add(new HistorialEntregaItem
                {
                    Fecha = F(r, 0)?.ToString("dd/MMM/yyyy") ?? "—",
                    NumeroReceta = S(r, 1),
                    Medicamento = $"{S(r, 2)} ({cant} pza)",
                    Cantidad = cant,
                    Medico = S(r, 4),
                    FolioTicket = S(r, 5)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Historial paciente", ex); }

        return lista;
    }

    // ==========================================
    // 6. VENTANILLA & RECETAS REALES (Frm_Recetas & Frm_RecetasDetalle)
    // ==========================================
    public async Task<RecetaVentanillaData?> ObtenerRecetaVentanillaRealAsync(string? numReceta = null)
    {
        if (string.IsNullOrWhiteSpace(numReceta)) return null;
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sqlHeader = $@"
                SELECT TOP 1
                    r.IdReceta, r.NumeroReceta,
                    COALESCE(
                        NULLIF(r.Diagnostico,''),
                        NULLIF({string.Format(NombrePersona, "p_carnet")},''),
                        NULLIF({string.Format(NombrePersona, "pb")},''),
                        {string.Format(NombrePersona, "pt")}, ''
                    ) AS Paciente,
                    ISNULL({string.Format(NombrePersona, "pm")},'') AS Medico,
                    r.FechaHora,
                    COALESCE(
                        NULLIF(NULLIF(dh_c.carnet,'0'),''),
                        NULLIF(NULLIF(dh.carnet,'0'),''),
                        af_c.no_afiliado,
                        af.no_afiliado,
                        CAST(r.Id_Derechohabiente AS nvarchar(30))
                    ) AS Carnet,
                    COALESCE(NULLIF(p_carnet.curp,''), NULLIF(pb.curp,''), pt.curp, '') AS Curp,
                    ISNULL(r.Diagnostico,''),
                    ISNULL(dh_c.nombramiento, ISNULL(dh.nombramiento,'')),
                    COALESCE(NULLIF(af_c.Id_dependencia_actual,0), af.Id_dependencia_actual, 0),
                    (SELECT TOP 1 c.NumeroTicket FROM Frm_Dispensaciones d
                       JOIN Frm_ComprobantesDispensacion c ON c.IdDispensacion = d.IdDispensacion
                      WHERE d.IdReceta = r.IdReceta ORDER BY d.IdDispensacion DESC),
                    ISNULL(r.Id_Medico, 0) AS IdMedico,
                    ISNULL(esp.Nombre, '') AS EspecialidadMedico,
                    ISNULL(pm.rfc, '') AS CedulaMedico,
                    ISNULL(r.Id_Derechohabiente, 0) AS IdDerechohabiente,
                    COALESCE(NULLIF(dep.nombre,''), NULLIF(dep.siglas,''), '') AS DependenciaNombre
                FROM Frm_Recetas r
                LEFT JOIN SeM_Derechohabientes dh_c ON (
                    dh_c.carnet = CAST(r.Id_Derechohabiente AS nvarchar(30))
                    OR dh_c.carnet = '-' + CAST(r.Id_Derechohabiente AS nvarchar(30))
                    OR dh_c.carnet LIKE CAST(r.Id_Derechohabiente AS nvarchar(30)) + '-%'
                    OR dh_c.carnet LIKE '%-' + CAST(r.Id_Derechohabiente AS nvarchar(30))
                )
                LEFT JOIN Afi_Afiliados af_c ON af_c.Id_Afiliado = dh_c.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p1.* FROM Afi_RelAfiliadosPersonas rel1
                             JOIN Afi_Personas p1 ON p1.Id_Persona = rel1.Id_persona
                             WHERE rel1.Id_afiliado = dh_c.Id_afiliado) p_carnet
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = r.Id_Derechohabiente
                LEFT JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p2.* FROM Afi_RelAfiliadosPersonas rel2
                             JOIN Afi_Personas p2 ON p2.Id_Persona = rel2.Id_persona
                             WHERE rel2.Id_afiliado = dh.Id_afiliado) pt
                LEFT JOIN SeM_Beneficiarios b ON b.Id_Beneficiario = NULLIF(r.Id_Beneficiario,0)
                LEFT JOIN Afi_Personas pb ON pb.Id_Persona = b.Id_persona
                LEFT JOIN SeM_Medicos med ON med.Id_Medico = r.Id_Medico
                LEFT JOIN Afi_Personas pm ON pm.Id_Persona = med.Id_persona
                LEFT JOIN SisFrm_CatEspecialidades esp ON esp.IdEspecialidad = med.Id_especialidad
                LEFT JOIN Sis_CatDependencias dep ON dep.Id_Dependencia = COALESCE(NULLIF(af_c.Id_dependencia_actual,0), af.Id_dependencia_actual, 0)
                WHERE r.NumeroReceta = @rx
                ORDER BY r.IdReceta DESC";

            long idReceta;
            RecetaVentanillaData result;
            await using (var cmd = new SqlCommand(sqlHeader, conn))
            {
                cmd.Parameters.AddWithValue("@rx", numReceta.Trim());
                await using var r = await cmd.ExecuteReaderAsync();
                if (!await r.ReadAsync()) return null;

                idReceta = Convert.ToInt64(r.GetValue(0));
                int dep = I(r, 9);
                string depNombre = S(r, 15);
                string nomb = S(r, 8);
                string medico = S(r, 3);
                string paciente = S(r, 2);
                string diagRaw = S(r, 7);
                string ticket = S(r, 10);
                string carnetRaw = S(r, 5);
                int idMed = I(r, 11);
                string esp = S(r, 12);
                string cedula = S(r, 13);
                int idDh = I(r, 14);
                if (string.IsNullOrWhiteSpace(cedula) || cedula.Length < 4) cedula = "—";

                if (carnetRaw.StartsWith("-") && carnetRaw.Length > 1) 
                    carnetRaw = carnetRaw.Substring(1);

                // Si el diagnóstico contiene el nombre del derechohabiente (debido a la migración histórica de FARMACIADB),
                // mostrar una descripción clínica adecuada en lugar de repetir el nombre de la persona en el diagnóstico
                string diagLimpio = diagRaw;
                if (string.IsNullOrWhiteSpace(diagLimpio) 
                    || diagLimpio.Equals(paciente, StringComparison.OrdinalIgnoreCase)
                    || diagLimpio.Split(' ').Length >= 3)
                {
                    diagLimpio = "Consulta Externa / Prescripción Médica Ambulatoria";
                }

                result = new RecetaVentanillaData
                {
                    IdReceta = idReceta,
                    IdDerechohabiente = idDh,
                    FolioReceta = S(r, 1),
                    PacienteNombre = paciente,
                    Carnet = $"Carnet {carnetRaw}",
                    Curp = S(r, 6),
                    Dependencia = !string.IsNullOrWhiteSpace(depNombre) ? depNombre : (dep > 0 ? $"Dependencia {dep}" : "—"),
                    TipoAfiliacion = string.IsNullOrEmpty(nomb) ? "Titular" : nomb,
                    IdMedico = idMed,
                    MedicoNombre = string.IsNullOrEmpty(medico) ? "Sin médico registrado" : $"Dr(a). {medico}",
                    CedulaMedico = cedula,
                    EspecialidadMedico = esp,
                    Diagnostico = diagLimpio,
                    FechaEmision = F(r, 4)?.ToString("dd MMM yyyy") ?? "—",
                    FolioTicket = string.IsNullOrEmpty(ticket) ? "Sin ticket" : ticket
                };
            }

            const string sqlPartidas = @"
                SELECT
                    ISNULL(m.Clave,''), ISNULL(m.CodigoBarras,''), ISNULL(m.Descripcion,''), ISNULL(m.Presentacion,''),
                    rd.CantidadPrescrita, ISNULL(rd.CantidadDispensada,0), ISNULL(rd.Indicaciones,''), ISNULL(rd.DiasTratamiento,0),
                    ISNULL(m.ValorInstitucional,0), ISNULL(m.TipoControl,'LIBRE'),
                    ISNULL((SELECT SUM(e.CantidadExistente) FROM Frm_InventarioExistencias e WHERE e.IdMedicamento = rd.IdMedicamento),0),
                    fe.NumeroLote, fe.FechaCaducidad,
                    rd.IdMedicamento, fe.IdLote
                FROM Frm_RecetasDetalle rd
                JOIN Frm_Medicamentos m ON m.IdMedicamento = rd.IdMedicamento
                OUTER APPLY (
                    SELECT TOP 1 l.IdLote, l.NumeroLote, l.FechaCaducidad
                    FROM Frm_InventarioExistencias e JOIN Frm_Lotes l ON l.IdLote = e.IdLote
                    WHERE e.IdMedicamento = rd.IdMedicamento AND e.CantidadExistente > 0
                    ORDER BY l.FechaCaducidad) fe
                WHERE rd.IdReceta = @id AND ISNULL(rd.Estatus,'') <> 'ELIMINADO'
                ORDER BY rd.Renglon";

            await using (var cmdP = new SqlCommand(sqlPartidas, conn))
            {
                cmdP.Parameters.AddWithValue("@id", idReceta);
                await using var rp = await cmdP.ExecuteReaderAsync();
                while (await rp.ReadAsync())
                {
                    int prescrita = Math.Max(1, I(rp, 4));
                    int dispensada = I(rp, 5);
                    int existencia = I(rp, 10);
                    string indic = S(rp, 6);
                    int dias = I(rp, 7);
                    DateTime? cad = F(rp, 12);
                    string tipoControl = S(rp, 9);
                    int idMedPartida = I(rp, 13);
                    int? idLotePartida = rp.IsDBNull(14) ? null : rp.GetInt32(14);

                    string claveRaw = S(rp, 0);
                    string codBarras = S(rp, 1);
                    string claveVisible = (!string.IsNullOrWhiteSpace(claveRaw) && claveRaw != "0")
                        ? claveRaw
                        : (!string.IsNullOrWhiteSpace(codBarras) ? codBarras : "—");

                    result.Partidas.Add(new PartidaReceta
                    {
                        IdMedicamento = idMedPartida,
                        IdLote = idLotePartida,
                        Clave = claveVisible,
                        CodigoBarras = codBarras,
                        Nombre = S(rp, 2),
                        Presentacion = S(rp, 3),
                        Dosis = string.IsNullOrEmpty(indic) ? (dias > 0 ? $"{dias} días" : "—") : (dias > 0 ? $"{indic} · {dias} días" : indic),
                        Prescrita = prescrita,
                        Existencia = existencia,
                        Lote = existencia > 0 && !rp.IsDBNull(11) ? S(rp, 11) : "—",
                        Caducidad = existencia > 0 && cad.HasValue ? cad.Value.ToString("MMM yyyy").ToUpper() : "—",
                        Precio = M(rp, 8),
                        EsControlado = !tipoControl.Equals("LIBRE", StringComparison.OrdinalIgnoreCase) && tipoControl != "",
                        Surtida = dispensada >= prescrita && existencia > 0
                    });
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            RegistrarError("Receta", ex);
            return null;
        }
    }

    // ==========================================
    // 7. REPORTES (Frm_Dispensaciones / Frm_Vales)
    // ==========================================
    public async Task<MetricasReporteData> ObtenerMetricasReporteRealAsync()
    {
        var data = new MetricasReporteData { TotalCobrado = 0, TotalAbsorcion = 0, RecetasSurtidas = 0, ValesEmitidos = 0 };
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = @"
                SELECT
                    (SELECT COUNT(DISTINCT IdReceta) FROM Frm_Dispensaciones WHERE ISNULL(Estatus,'') <> 'CANCELADO'),
                    (SELECT COUNT(*) FROM Frm_Vales),
                    (SELECT ISNULL(SUM(ImportePaciente),0) FROM Frm_Dispensaciones WHERE ISNULL(Estatus,'') <> 'CANCELADO'),
                    (SELECT ISNULL(SUM(ImporteInstituto),0) FROM Frm_Dispensaciones WHERE ISNULL(Estatus,'') <> 'CANCELADO')";
            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 60 };
            await using var r = await cmd.ExecuteReaderAsync();
            if (await r.ReadAsync())
            {
                data.RecetasSurtidas = I(r, 0);
                data.ValesEmitidos = I(r, 1);
                data.TotalCobrado = M(r, 2);
                data.TotalAbsorcion = M(r, 3);
            }
        }
        catch (Exception ex) { RegistrarError("Métricas reporte", ex); }
        return data;
    }

    // ==========================================
    // 8. MÉDICOS PRESCRIPTORES (SeM_Medicos + Afi_Personas + SisFrm_CatEspecialidades)
    // ==========================================
    public async Task<List<MedicoItem>> ObtenerMedicosRealesAsync()
    {
        var lista = new List<MedicoItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = $@"
                SELECT m.Id_Medico, {string.Format(NombrePersona, "p")} AS Nombre, ISNULL(e.Nombre,''), ISNULL(p.rfc,'')
                FROM SeM_Medicos m
                JOIN Afi_Personas p ON p.Id_Persona = m.Id_persona
                LEFT JOIN SisFrm_CatEspecialidades e ON e.IdEspecialidad = m.Id_especialidad
                ORDER BY Nombre";

            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                string nombre = S(r, 1);
                if (string.IsNullOrWhiteSpace(nombre)) continue;
                string rfc = S(r, 3);
                lista.Add(new MedicoItem
                {
                    IdMedico = I(r, 0),
                    NombreCompleto = $"Dr(a). {nombre}",
                    Cedula = string.IsNullOrWhiteSpace(rfc) || rfc.Length < 4 ? "—" : rfc,
                    Especialidad = S(r, 2)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Médicos", ex); }

        return lista;
    }

    // ==========================================
    // 9. BENEFICIARIOS DEL TITULAR (SeM_Beneficiarios)
    // ==========================================
    public async Task<List<BeneficiarioItem>> ObtenerBeneficiariosPacienteAsync(string carnet)
    {
        var lista = new List<BeneficiarioItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string carnetLimpio = (carnet ?? "").Replace("Carnet", "").Trim();
            if (carnetLimpio.Length == 0) return lista;

            string sql = $@"
                SELECT b.Id_Beneficiario, ISNULL(b.carnet,''),
                       {string.Format(NombrePersona, "p")}, ISNULL(NULLIF(b.parentesco_descripcion,''),'Familiar'),
                       ISNULL(p.curp,'')
                FROM SeM_Derechohabientes dh
                JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                JOIN SeM_Beneficiarios b ON b.Id_derechohabiente = dh.Id_Derechohabiente
                JOIN Afi_Personas p ON p.Id_Persona = b.Id_persona
                WHERE dh.carnet = @carnet OR af.no_afiliado = @carnet
                ORDER BY b.Id_Beneficiario";

            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@carnet", carnetLimpio);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new BeneficiarioItem
                {
                    IdBeneficiario = I(r, 0),
                    Carnet = S(r, 1),
                    NombreCompleto = S(r, 2),
                    Parentesco = S(r, 3),
                    Curp = S(r, 4)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Beneficiarios", ex); }

        return lista;
    }

    // ==========================================
    // 10. PROVEEDORES (SisFrm_CatProveedores)
    // ==========================================
    public async Task<List<ProveedorItem>> ObtenerProveedoresRealesAsync()
    {
        var lista = new List<ProveedorItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                SELECT IdProveedor, ISNULL(RazonSocial,''), ISNULL(RFC,''), ISNULL(Telefono,'')
                FROM SisFrm_CatProveedores
                WHERE activo = 1 AND LEN(LTRIM(RTRIM(ISNULL(RazonSocial,'')))) > 0
                ORDER BY RazonSocial";

            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ProveedorItem
                {
                    IdProveedor = I(r, 0),
                    RazonSocial = S(r, 1),
                    Rfc = S(r, 2),
                    Telefono = S(r, 3)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Proveedores", ex); }

        return lista;
    }

    // ==========================================
    // 11. AUTENTICACIÓN (Frm_Usuarios en IUBIDB_FARMACIAS)
    // ==========================================
    public async Task<(UsuarioLogueado? usuario, string mensajeError)> AutenticarUsuarioConValidacionAsync(string usuario, string password)
    {
        if (string.IsNullOrWhiteSpace(usuario))
            return (null, "Por favor ingresa tu nombre de usuario institucional.");

        if (string.IsNullOrWhiteSpace(password))
            return (null, "Por favor ingresa tu contraseña de acceso.");

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sqlIubi = @"
                SELECT TOP 1
                    usuario AS ID_Usuario, 
                    ISNULL(clave, '') AS Contraseña, 
                    ISNULL(salt, '') AS Salt, 
                    ISNULL(nombre, '') AS Nombre, 
                    ISNULL(perfil, 'FARMACIA_ENTREGA') AS Perfil_De_Usuario, 
                    1 AS Farmacia_ID,
                    Id_estatus
                FROM Frm_Usuarios
                WHERE usuario = @user";

            string idUsuario = "";
            string dbHash = "";
            string dbSalt = "";
            string nombre = "";
            string perfil = "";
            int? farmaciaId = null;
            int idEstatus = 0;
            bool usuarioEncontrado = false;

            await using (var cmd = new SqlCommand(sqlIubi, conn))
            {
                cmd.Parameters.AddWithValue("@user", usuario.Trim());
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    idUsuario = r.GetString(0);
                    dbHash = r.GetString(1);
                    dbSalt = r.GetString(2);
                    nombre = r.GetString(3);
                    perfil = r.GetString(4);
                    farmaciaId = !r.IsDBNull(5) ? r.GetInt32(5) : null;
                    idEstatus = !r.IsDBNull(6) ? r.GetInt32(6) : 0;
                    usuarioEncontrado = true;
                }
            }

            if (!usuarioEncontrado)
            {
                return (null, $"El usuario '{usuario.Trim()}' no existe en el catálogo institucional (Frm_Usuarios).");
            }

            if (idEstatus != 1)
            {
                return (null, $"La cuenta de usuario '{usuario.Trim()}' se encuentra INACTIVA en el sistema. Contacte al Administrador.");
            }

            if (!ValidarPassword(password, dbHash, dbSalt))
            {
                return (null, "Contraseña incorrecta. Verifique sus credenciales e intente de nuevo.");
            }

            string nombreCompleto = nombre.Trim();
            if (string.IsNullOrWhiteSpace(nombreCompleto))
                nombreCompleto = idUsuario;

            return (new UsuarioLogueado
            {
                ID_Usuario = idUsuario,
                NombreCompleto = nombreCompleto,
                Perfil = perfil,
                FarmaciaID = farmaciaId,
                MenusPermitidos = new List<int>()
            }, "");
        }
        catch (Exception ex)
        {
            RegistrarError("Autenticación", ex);
            return (null, $"Error al comunicar con la base de datos:\n{ex.Message}");
        }
    }

    public async Task<UsuarioLogueado?> AutenticarUsuarioAsync(string usuario, string password)
    {
        var (u, _) = await AutenticarUsuarioConValidacionAsync(usuario, password);
        return u;
    }

    private static bool ValidarPassword(string password, string correctHash, string salt)
    {
        try
        {
            byte[] tmpSalt = Convert.FromBase64String(salt);
            byte[] hash = Convert.FromBase64String(correctHash);

            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(password, tmpSalt, 1000, System.Security.Cryptography.HashAlgorithmName.SHA1);
            byte[] testHash = pbkdf2.GetBytes(24);

            if (hash.Length != testHash.Length) return false;

            int diff = 0;
            for (int i = 0; i < hash.Length; i++)
                diff |= (hash[i] ^ testHash[i]);

            return diff == 0;
        }
        catch
        {
            return false;
        }
    }

    // ==========================================
    // 10. REIMPRESIÓN DE DOCUMENTOS (ReprintDocumentWindow)
    // ==========================================
    public async Task<List<DocumentoReimpresionItem>> ObtenerDocumentosReimpresionAsync(DateTime desde, DateTime hasta)
    {
        var lista = new List<DocumentoReimpresionItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT TOP 200
                    c.IdDispensacion AS Id,
                    ISNULL(r.NumeroReceta, 'SIN FOLIO') AS NumeroReceta,
                    d.FechaHora,
                    'Ticket de Dispensación' AS TipoDocumento,
                    COALESCE(NULLIF(r.Diagnostico,''), NULLIF(p.nombres + ' ' + p.primer_apellido,''), 'Derechohabiente') AS Paciente,
                    c.NumeroTicket
                FROM Frm_ComprobantesDispensacion c
                JOIN Frm_Dispensaciones d ON d.IdDispensacion = c.IdDispensacion
                LEFT JOIN Frm_Recetas r ON r.IdReceta = d.IdReceta
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = d.Id_Derechohabiente
                OUTER APPLY (SELECT TOP 1 pp.* FROM Afi_RelAfiliadosPersonas rel JOIN Afi_Personas pp ON pp.Id_Persona = rel.Id_persona WHERE rel.Id_afiliado = dh.Id_afiliado) p
                WHERE d.FechaHora >= @desde AND d.FechaHora <= @hasta
                UNION ALL
                SELECT TOP 100
                    v.IdVale AS Id,
                    ISNULL(r.NumeroReceta, '—') AS NumeroReceta,
                    v.FechaEmision AS FechaHora,
                    'Vale de Farmacia Subrogada' AS TipoDocumento,
                    COALESCE(NULLIF(r.Diagnostico,''), 'Derechohabiente') AS Paciente,
                    v.NumeroVale AS NumeroTicket
                FROM Frm_Vales v
                LEFT JOIN Frm_Recetas r ON r.IdReceta = v.IdReceta
                WHERE v.FechaEmision >= @desde AND v.FechaEmision <= @hasta
                ORDER BY FechaHora DESC";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 30 };
            cmd.Parameters.AddWithValue("@desde", desde.Date);
            cmd.Parameters.AddWithValue("@hasta", hasta.Date.AddDays(1).AddSeconds(-1));
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new DocumentoReimpresionItem
                {
                    ID = S(r, 0),
                    NumeroReceta = S(r, 1),
                    FechaReceta = F(r, 2)?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    TipoDocumento = S(r, 3),
                    Paciente = S(r, 4),
                    FolioTicket = S(r, 5)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Reimpresión", ex); }
        return lista;
    }

    // ==========================================
    // 11. INFORMACIÓN DE LA RECETA & DEVOLUCIÓN (PrescriptionCancelWindow)
    // ==========================================
    public async Task<(string Paciente, string Carnet, string FolioReceta, string Fecha, string Medico, List<RecetaDetalleInfoItem> Detalle, List<RecetaValeInfoItem> Vales)> ObtenerInfoRecetaCompletaAsync(string folioReceta)
    {
        string pac = "", carnet = "", folio = folioReceta, fecha = "", med = "";
        var detalle = new List<RecetaDetalleInfoItem>();
        var vales = new List<RecetaValeInfoItem>();

        if (string.IsNullOrWhiteSpace(folioReceta)) return (pac, carnet, folio, fecha, med, detalle, vales);

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sqlH = $@"
                SELECT TOP 1
                    r.IdReceta, r.NumeroReceta,
                    COALESCE(NULLIF(r.Diagnostico,''), NULLIF({string.Format(NombrePersona, "p_c")},''), {string.Format(NombrePersona, "pt")}, '') AS Paciente,
                    ISNULL(dh_c.carnet, ISNULL(dh.carnet, CAST(r.Id_Derechohabiente AS nvarchar(30)))) AS Carnet,
                    r.FechaHora,
                    ISNULL({string.Format(NombrePersona, "pm")}, 'Dr(a). Médico Tratante') AS Medico
                FROM Frm_Recetas r
                LEFT JOIN SeM_Derechohabientes dh_c ON (dh_c.carnet = CAST(r.Id_Derechohabiente AS nvarchar(30)) OR dh_c.carnet = '-' + CAST(r.Id_Derechohabiente AS nvarchar(30)))
                LEFT JOIN Afi_Afiliados af_c ON af_c.Id_Afiliado = dh_c.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p1.* FROM Afi_RelAfiliadosPersonas rel1 JOIN Afi_Personas p1 ON p1.Id_Persona = rel1.Id_persona WHERE rel1.Id_afiliado = dh_c.Id_afiliado) p_c
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = r.Id_Derechohabiente
                LEFT JOIN Afi_Afiliados af ON af.Id_Afiliado = dh.Id_afiliado
                OUTER APPLY (SELECT TOP 1 p2.* FROM Afi_RelAfiliadosPersonas rel2 JOIN Afi_Personas p2 ON p2.Id_Persona = rel2.Id_persona WHERE rel2.Id_afiliado = dh.Id_afiliado) pt
                LEFT JOIN SeM_Medicos m ON m.Id_Medico = r.Id_Medico
                LEFT JOIN Afi_Personas pm ON pm.Id_Persona = m.Id_persona
                WHERE r.NumeroReceta = @rx";

            long idReceta = 0;
            await using (var cmd = new SqlCommand(sqlH, conn))
            {
                cmd.Parameters.AddWithValue("@rx", folioReceta.Trim());
                await using var r = await cmd.ExecuteReaderAsync();
                if (await r.ReadAsync())
                {
                    idReceta = Convert.ToInt64(r.GetValue(0));
                    folio = S(r, 1);
                    pac = S(r, 2);
                    carnet = S(r, 3).TrimStart('-');
                    fecha = F(r, 4)?.ToString("dd/MM/yyyy") ?? "—";
                    med = S(r, 5);
                }
            }

            if (idReceta > 0)
            {
                // Detalle de Receta / Dispensación
                string sqlD = @"
                    SELECT
                        ISNULL(c.NumeroTicket, CAST(d.NumeroDispensacion AS nvarchar(30))),
                        ISNULL(dd.Cantidad, rd.CantidadPrescrita),
                        ISNULL(m.CodigoBarras,''),
                        ISNULL(m.Descripcion,''),
                        ISNULL(dd.Id_estatus, rd.Id_estatus)
                    FROM Frm_RecetasDetalle rd
                    JOIN Frm_Medicamentos m ON m.IdMedicamento = rd.IdMedicamento
                    LEFT JOIN Frm_Dispensaciones d ON d.IdReceta = rd.IdReceta
                    LEFT JOIN Frm_DispensacionesDetalle dd ON dd.IdDispensacion = d.IdDispensacion AND dd.IdMedicamento = rd.IdMedicamento
                    LEFT JOIN Frm_ComprobantesDispensacion c ON c.IdDispensacion = d.IdDispensacion
                    WHERE rd.IdReceta = @id
                    ORDER BY rd.Renglon";

                await using (var cmdD = new SqlCommand(sqlD, conn))
                {
                    cmdD.Parameters.AddWithValue("@id", idReceta);
                    await using var rd = await cmdD.ExecuteReaderAsync();
                    while (await rd.ReadAsync())
                    {
                        detalle.Add(new RecetaDetalleInfoItem
                        {
                            Ticket_ID = S(rd, 0),
                            Cantidad = I(rd, 1),
                            CodigoBarras = S(rd, 2),
                            Descripcion = S(rd, 3),
                            EstatusString = I(rd, 4) == 2 ? "CANCELADO" : "SURTIDO"
                        });
                    }
                }

                // Vales vinculados
                string sqlV = @"
                    SELECT
                        v.NumeroVale,
                        vd.CantidadAutorizada,
                        ISNULL(m.CodigoBarras,''),
                        ISNULL(m.Descripcion,''),
                        DATEDIFF(day, GETDATE(), v.FechaVencimiento) AS Dias,
                        ISNULL(vd.CantidadDispensada,0),
                        (vd.CantidadAutorizada - ISNULL(vd.CantidadDispensada,0)) AS Restan
                    FROM Frm_Vales v
                    JOIN Frm_ValesDetalle vd ON vd.IdVale = v.IdVale
                    LEFT JOIN Frm_Medicamentos m ON m.IdMedicamento = vd.IdMedicamento
                    WHERE v.IdReceta = @id";

                await using (var cmdV = new SqlCommand(sqlV, conn))
                {
                    cmdV.Parameters.AddWithValue("@id", idReceta);
                    await using var rv = await cmdV.ExecuteReaderAsync();
                    while (await rv.ReadAsync())
                    {
                        vales.Add(new RecetaValeInfoItem
                        {
                            ID_Vale = S(rv, 0),
                            Cantidad = I(rv, 1),
                            CodigoBarras = S(rv, 2),
                            Descripcion = S(rv, 3),
                            Dias_Surtir = Math.Max(0, I(rv, 4)),
                            Surtido = I(rv, 5),
                            Restan = Math.Max(0, I(rv, 6))
                        });
                    }
                }
            }
        }
        catch (Exception ex) { RegistrarError("Info Receta", ex); }

        return (pac, carnet, folio, fecha, med, detalle, vales);
    }

    // ==========================================
    // 12. REPORTE DE COMPRAS (BillsEntryReportWindow)
    // ==========================================
    public async Task<List<ReporteCompraRowItem>> ObtenerReporteComprasAsync(string? proveedor, string? factura, DateTime? desde, DateTime? hasta, bool desglosado)
    {
        var lista = new List<ReporteCompraRowItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = desglosado ? @"
                SELECT TOP 500
                    c.NumeroFactura,
                    ISNULL(prov.RazonSocial, ISNULL(prov.NombreComercial, c.Observaciones)) AS Proveedor,
                    c.FechaRecepcion,
                    c.IVA,
                    c.Subtotal,
                    c.Total,
                    'DIRECTA' AS TipoCompra,
                    m.Descripcion AS Medicamento,
                    ISNULL(l.NumeroLote,'—') AS Lote,
                    cd.Cantidad,
                    cd.CostoUnitario
                FROM Frm_ComprasDetalle cd
                JOIN Frm_Compras c ON c.IdCompra = cd.IdCompra
                LEFT JOIN SisFrm_CatProveedores prov ON prov.IdProveedor = c.IdProveedor
                JOIN Frm_Medicamentos m ON m.IdMedicamento = cd.IdMedicamento
                LEFT JOIN Frm_Lotes l ON l.IdLote = cd.IdLote
                WHERE (@prov IS NULL OR prov.RazonSocial LIKE '%' + @prov + '%' OR prov.NombreComercial LIKE '%' + @prov + '%')
                  AND (@fac IS NULL OR c.NumeroFactura LIKE '%' + @fac + '%')
                  AND (@desde IS NULL OR c.FechaRecepcion >= @desde)
                  AND (@hasta IS NULL OR c.FechaRecepcion <= @hasta)
                ORDER BY c.FechaRecepcion DESC"
            : @"
                SELECT TOP 500
                    c.NumeroFactura,
                    ISNULL(prov.RazonSocial, ISNULL(prov.NombreComercial, c.Observaciones)) AS Proveedor,
                    c.FechaRecepcion,
                    c.IVA,
                    c.Subtotal,
                    c.Total,
                    'DIRECTA' AS TipoCompra,
                    '' AS Medicamento,
                    '' AS Lote,
                    0 AS Cantidad,
                    0 AS CostoUnitario
                FROM Frm_Compras c
                LEFT JOIN SisFrm_CatProveedores prov ON prov.IdProveedor = c.IdProveedor
                WHERE (@prov IS NULL OR prov.RazonSocial LIKE '%' + @prov + '%' OR prov.NombreComercial LIKE '%' + @prov + '%')
                  AND (@fac IS NULL OR c.NumeroFactura LIKE '%' + @fac + '%')
                  AND (@desde IS NULL OR c.FechaRecepcion >= @desde)
                  AND (@hasta IS NULL OR c.FechaRecepcion <= @hasta)
                ORDER BY c.FechaRecepcion DESC";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 40 };
            cmd.Parameters.AddWithValue("@prov", string.IsNullOrWhiteSpace(proveedor) ? DBNull.Value : proveedor.Trim());
            cmd.Parameters.AddWithValue("@fac", string.IsNullOrWhiteSpace(factura) ? DBNull.Value : factura.Trim());
            cmd.Parameters.AddWithValue("@desde", desde.HasValue ? desde.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", hasta.HasValue ? hasta.Value.Date.AddDays(1).AddSeconds(-1) : DBNull.Value);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ReporteCompraRowItem
                {
                    NumeroFactura = S(r, 0),
                    NombreProveedor = S(r, 1),
                    FechaCompra = F(r, 2)?.ToString("dd/MM/yyyy") ?? "—",
                    IVAFactura = M(r, 3),
                    SubtotalFactura = M(r, 4),
                    TotalFactura = M(r, 5),
                    TipoCompra = S(r, 6),
                    Medicamento = S(r, 7),
                    Lote = S(r, 8),
                    Cantidad = I(r, 9),
                    PrecioUnitario = M(r, 10)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Reporte Compras", ex); }
        return lista;
    }

    // ==========================================
    // 13. REPORTE ENTREGA DE MEDICAMENTOS (ReporteEntregaMedicamentos)
    // ==========================================
    public async Task<List<ReporteEntregaRowItem>> ObtenerReporteEntregaMedicamentosAsync(
        DateTime? desde, DateTime? hasta, string? receta, string? codigo, string? descripcion, string? carnet, string? nombre, bool cuadroBasico)
    {
        var lista = new List<ReporteEntregaRowItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT TOP 600
                    ISNULL(c.NumeroTicket, CAST(d.NumeroDispensacion AS nvarchar(30))) AS Ticket,
                    d.FechaHora,
                    dd.Cantidad,
                    dd.IdMedicamento,
                    ISNULL(m.CodigoBarras,'') AS Codigo,
                    ISNULL(m.Descripcion,'') AS Descripcion,
                    ISNULL(dh.carnet, CAST(d.Id_Derechohabiente AS nvarchar(30))) AS Carnet,
                    COALESCE(NULLIF(r.Diagnostico,''), NULLIF(p.nombres + ' ' + p.primer_apellido,''), 'Derechohabiente') AS Nombre,
                    ISNULL(r.NumeroReceta, '—') AS Receta,
                    CASE WHEN EXISTS (SELECT 1 FROM Frm_Medicamento_Cuadro mc WHERE mc.IdMedicamento = dd.IdMedicamento AND mc.activo = 1) THEN 1 ELSE 0 END AS Cuadro
                FROM Frm_DispensacionesDetalle dd
                JOIN Frm_Dispensaciones d ON d.IdDispensacion = dd.IdDispensacion
                LEFT JOIN Frm_Medicamentos m ON m.IdMedicamento = dd.IdMedicamento
                LEFT JOIN Frm_Recetas r ON r.IdReceta = d.IdReceta
                LEFT JOIN Frm_ComprobantesDispensacion c ON c.IdDispensacion = d.IdDispensacion
                LEFT JOIN SeM_Derechohabientes dh ON dh.Id_Derechohabiente = d.Id_Derechohabiente
                OUTER APPLY (SELECT TOP 1 pp.* FROM Afi_RelAfiliadosPersonas rel JOIN Afi_Personas pp ON pp.Id_Persona = rel.Id_persona WHERE rel.Id_afiliado = dh.Id_afiliado) p
                WHERE (@desde IS NULL OR d.FechaHora >= @desde)
                  AND (@hasta IS NULL OR d.FechaHora <= @hasta)
                  AND (@rx IS NULL OR r.NumeroReceta LIKE '%' + @rx + '%')
                  AND (@cod IS NULL OR m.CodigoBarras LIKE '%' + @cod + '%' OR m.Clave LIKE '%' + @cod + '%')
                  AND (@desc IS NULL OR m.Descripcion LIKE '%' + @desc + '%')
                  AND (@carnet IS NULL OR dh.carnet LIKE '%' + @carnet + '%')
                  AND (@nom IS NULL OR r.Diagnostico LIKE '%' + @nom + '%' OR p.nombres LIKE '%' + @nom + '%')
                  AND (@cuadro = 0 OR EXISTS (SELECT 1 FROM Frm_Medicamento_Cuadro mc WHERE mc.IdMedicamento = dd.IdMedicamento AND mc.activo = 1))
                ORDER BY d.FechaHora DESC";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 50 };
            cmd.Parameters.AddWithValue("@desde", desde.HasValue ? desde.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", hasta.HasValue ? hasta.Value.Date.AddDays(1).AddSeconds(-1) : DBNull.Value);
            cmd.Parameters.AddWithValue("@rx", string.IsNullOrWhiteSpace(receta) ? DBNull.Value : receta.Trim());
            cmd.Parameters.AddWithValue("@cod", string.IsNullOrWhiteSpace(codigo) ? DBNull.Value : codigo.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(descripcion) ? DBNull.Value : descripcion.Trim());
            cmd.Parameters.AddWithValue("@carnet", string.IsNullOrWhiteSpace(carnet) ? DBNull.Value : carnet.Trim().TrimStart('-'));
            cmd.Parameters.AddWithValue("@nom", string.IsNullOrWhiteSpace(nombre) ? DBNull.Value : nombre.Trim());
            cmd.Parameters.AddWithValue("@cuadro", cuadroBasico ? 1 : 0);

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ReporteEntregaRowItem
                {
                    NumeroTicket = S(r, 0),
                    Fecha = F(r, 1)?.ToString("dd/MM/yyyy HH:mm") ?? "—",
                    Cantidad = I(r, 2),
                    ID_Medicamento = I(r, 3),
                    Codigo = S(r, 4),
                    Descripcion = S(r, 5),
                    ID_Derechohabiente = S(r, 6).TrimStart('-'),
                    Nombre = S(r, 7),
                    Receta = S(r, 8),
                    EsCuadroBasico = I(r, 9) == 1
                });
            }
        }
        catch (Exception ex) { RegistrarError("Reporte Entregas", ex); }
        return lista;
    }

    // ==========================================
    // 14. REPORTE ENTRADAS DE MEDICAMENTOS (EntradaDeMedicamentos)
    // ==========================================
    public async Task<List<ReporteEntradaRowItem>> ObtenerReporteEntradasMedicamentoAsync(
        DateTime? desde, DateTime? hasta, string? factura, string? proveedor, string? codigo, string? descripcion)
    {
        var lista = new List<ReporteEntradaRowItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string sql = @"
                SELECT TOP 600
                    c.NumeroFactura,
                    ISNULL(prov.RazonSocial, ISNULL(prov.NombreComercial, c.Observaciones)) AS Proveedor,
                    c.FechaRecepcion,
                    m.IdMedicamento,
                    ISNULL(m.CodigoBarras, ISNULL(m.Clave,'')),
                    m.Descripcion,
                    ROW_NUMBER() OVER (PARTITION BY cd.IdCompra ORDER BY cd.IdCompraDetalle) AS Renglon,
                    cd.Cantidad,
                    cd.CostoUnitario,
                    cd.ImporteIVA,
                    cd.Subtotal,
                    l.FechaCaducidad,
                    ISNULL(l.NumeroLote,'—') AS Lote
                FROM Frm_ComprasDetalle cd
                JOIN Frm_Compras c ON c.IdCompra = cd.IdCompra
                LEFT JOIN SisFrm_CatProveedores prov ON prov.IdProveedor = c.IdProveedor
                JOIN Frm_Medicamentos m ON m.IdMedicamento = cd.IdMedicamento
                LEFT JOIN Frm_Lotes l ON l.IdLote = cd.IdLote
                WHERE (@desde IS NULL OR c.FechaRecepcion >= @desde)
                  AND (@hasta IS NULL OR c.FechaRecepcion <= @hasta)
                  AND (@fac IS NULL OR c.NumeroFactura LIKE '%' + @fac + '%')
                  AND (@prov IS NULL OR prov.RazonSocial LIKE '%' + @prov + '%' OR prov.NombreComercial LIKE '%' + @prov + '%')
                  AND (@cod IS NULL OR m.CodigoBarras LIKE '%' + @cod + '%' OR m.Clave LIKE '%' + @cod + '%')
                  AND (@desc IS NULL OR m.Descripcion LIKE '%' + @desc + '%')
                ORDER BY c.FechaRecepcion DESC, cd.IdCompraDetalle";

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 50 };
            cmd.Parameters.AddWithValue("@desde", desde.HasValue ? desde.Value.Date : DBNull.Value);
            cmd.Parameters.AddWithValue("@hasta", hasta.HasValue ? hasta.Value.Date.AddDays(1).AddSeconds(-1) : DBNull.Value);
            cmd.Parameters.AddWithValue("@fac", string.IsNullOrWhiteSpace(factura) ? DBNull.Value : factura.Trim());
            cmd.Parameters.AddWithValue("@prov", string.IsNullOrWhiteSpace(proveedor) ? DBNull.Value : proveedor.Trim());
            cmd.Parameters.AddWithValue("@cod", string.IsNullOrWhiteSpace(codigo) ? DBNull.Value : codigo.Trim());
            cmd.Parameters.AddWithValue("@desc", string.IsNullOrWhiteSpace(descripcion) ? DBNull.Value : descripcion.Trim());

            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new ReporteEntradaRowItem
                {
                    NumeroFactura = S(r, 0),
                    Proveedor = S(r, 1),
                    FechaCompra = F(r, 2)?.ToString("dd/MM/yyyy") ?? "—",
                    ID_Medicamento = I(r, 3),
                    CodigoBarras = S(r, 4),
                    Descripcion = S(r, 5),
                    Renglon = I(r, 6),
                    Cantidad = I(r, 7),
                    PrecioUnitario = M(r, 8),
                    IVA = M(r, 9),
                    Subtotal = M(r, 10),
                    FechaCaducidad = F(r, 11)?.ToString("dd/MM/yyyy") ?? "—",
                    Lote = S(r, 12)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Reporte Entradas", ex); }
        return lista;
    }

    // ==========================================
    // 15. USUARIOS DEL SISTEMA (Frm_Usuarios)
    // ==========================================
    public async Task<List<UsuarioSistemaItem>> ObtenerUsuariosSistemaAsync()
    {
        var lista = new List<UsuarioSistemaItem>();
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = @"
                SELECT Id_Usuario, usuario, nombre, ISNULL(email,''), perfil, Id_estatus
                FROM Frm_Usuarios
                ORDER BY Id_estatus DESC, perfil, usuario";
            await using var cmd = new SqlCommand(sql, conn);
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                lista.Add(new UsuarioSistemaItem
                {
                    IdUsuario = I(r, 0),
                    Usuario = S(r, 1),
                    Nombre = S(r, 2),
                    Email = S(r, 3),
                    Perfil = S(r, 4),
                    IdEstatus = I(r, 5)
                });
            }
        }
        catch (Exception ex) { RegistrarError("Usuarios Sistema", ex); }
        return lista;
    }

    public async Task<(bool exito, string mensaje)> ActualizarPerfilUsuarioAsync(int idUsuario, string nuevoPerfil)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = "UPDATE Frm_Usuarios SET perfil = @perfil, fecha_modificacion = GETDATE() WHERE Id_Usuario = @id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@perfil", nuevoPerfil.Trim());
            cmd.Parameters.AddWithValue("@id", idUsuario);
            int rows = await cmd.ExecuteNonQueryAsync();
            return (rows > 0, rows > 0 ? "Perfil actualizado correctamente en la base de datos." : "No se encontró el registro del usuario.");
        }
        catch (Exception ex)
        {
            RegistrarError("Actualizar Perfil", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool exito, string mensaje)> ActualizarContraseniaUsuarioAsync(int idUsuario, string nuevaPassword)
    {
        if (string.IsNullOrWhiteSpace(nuevaPassword))
            return (false, "La contraseña no puede estar vacía.");

        try
        {
            byte[] saltBytes = new byte[24];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string saltBase64 = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                nuevaPassword.Trim(), saltBytes, 1000, System.Security.Cryptography.HashAlgorithmName.SHA1);
            byte[] hashBytes = pbkdf2.GetBytes(24);
            string hashBase64 = Convert.ToBase64String(hashBytes);

            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = "UPDATE Frm_Usuarios SET clave = @hash, salt = @salt, fecha_modificacion = GETDATE() WHERE Id_Usuario = @id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@hash", hashBase64);
            cmd.Parameters.AddWithValue("@salt", saltBase64);
            cmd.Parameters.AddWithValue("@id", idUsuario);
            int rows = await cmd.ExecuteNonQueryAsync();
            return (rows > 0, rows > 0 ? "Contraseña institucional actualizada con éxito." : "Usuario no encontrado.");
        }
        catch (Exception ex)
        {
            RegistrarError("Actualizar Contraseña", ex);
            return (false, ex.Message);
        }
    }

    public async Task<(bool exito, string mensaje)> AlternarEstatusUsuarioAsync(int idUsuario, int nuevoEstatus)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string sql = "UPDATE Frm_Usuarios SET Id_estatus = @est, fecha_modificacion = GETDATE() WHERE Id_Usuario = @id";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@est", nuevoEstatus);
            cmd.Parameters.AddWithValue("@id", idUsuario);
            int rows = await cmd.ExecuteNonQueryAsync();
            return (rows > 0, rows > 0 ? $"Estatus actualizado a {(nuevoEstatus == 1 ? "Activo" : "Inactivo")}." : "Usuario no encontrado.");
        }
        catch (Exception ex)
        {
            RegistrarError("Alternar Estatus", ex);
            return (false, ex.Message);
        }
    }

    // ==========================================
    // 12. TRANSACCIONES REALES EN IUBIDB_FARMACIAS_PRUEBAS
    // ==========================================
    public async Task<(bool exito, string folioTicket, long idDispensacion, string mensaje)> RegistrarDispensacionRealAsync(
        long? idReceta,
        int idDerechohabiente,
        decimal total,
        decimal importePaciente,
        decimal importeInstituto,
        List<PartidaReceta> partidas,
        string observaciones,
        string usuario)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();

            try
            {
                // 1. Insertar en Frm_Dispensaciones
                const string sqlDisp = @"
                    INSERT INTO Frm_Dispensaciones (
                        NumeroDispensacion, Id_Sucursal, IdAlmacen, Id_Derechohabiente,
                        Id_Beneficiario, IdReceta, FechaHora, CostoTotalAdquisicion,
                        ValorTotalInstitucional, ImportePaciente, ImporteInstituto,
                        Estatus, Observaciones, Id_estatus, fecha_creacion
                    ) VALUES (
                        'TMP', 1, 1, @idDh,
                        NULL, @idReceta, GETDATE(), @costo,
                        @valor, @impPac, @impInst,
                        'ACTIVO', @obs, 1, GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();";

                long idDisp;
                await using (var cmdDisp = new SqlCommand(sqlDisp, conn, tx))
                {
                    cmdDisp.Parameters.AddWithValue("@idDh", idDerechohabiente > 0 ? idDerechohabiente : 3024);
                    cmdDisp.Parameters.AddWithValue("@idReceta", (object?)idReceta ?? DBNull.Value);
                    cmdDisp.Parameters.AddWithValue("@costo", total);
                    cmdDisp.Parameters.AddWithValue("@valor", total);
                    cmdDisp.Parameters.AddWithValue("@impPac", importePaciente);
                    cmdDisp.Parameters.AddWithValue("@impInst", importeInstituto);
                    cmdDisp.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(observaciones) ? $"Dispensado por {usuario}" : observaciones);

                    object? resId = await cmdDisp.ExecuteScalarAsync();
                    idDisp = Convert.ToInt64(resId);
                }

                string folioTicket = idDisp.ToString();
                const string sqlUpdNum = "UPDATE Frm_Dispensaciones SET NumeroDispensacion = @num WHERE IdDispensacion = @id";
                await using (var cmdNum = new SqlCommand(sqlUpdNum, conn, tx))
                {
                    cmdNum.Parameters.AddWithValue("@num", folioTicket);
                    cmdNum.Parameters.AddWithValue("@id", idDisp);
                    await cmdNum.ExecuteNonQueryAsync();
                }

                // 2. Insertar Frm_DispensacionesDetalle, descontar existencias y registrar Kardex
                foreach (var p in partidas.Where(x => x.Surtida))
                {
                    int cant = p.Prescrita > 0 ? p.Prescrita : 1;
                    decimal unitario = p.Precio;
                    decimal importe = cant * unitario;

                    // Si no tenemos IdMedicamento, buscarlo por clave/descripción
                    int idMed = p.IdMedicamento;
                    if (idMed <= 0)
                    {
                        const string sqlFindMed = "SELECT TOP 1 IdMedicamento FROM Frm_Medicamentos WHERE Clave = @c OR CodigoBarras = @c OR Descripcion = @n";
                        await using (var cmdFm = new SqlCommand(sqlFindMed, conn, tx))
                        {
                            cmdFm.Parameters.AddWithValue("@c", p.Clave);
                            cmdFm.Parameters.AddWithValue("@n", p.Nombre);
                            object? rMed = await cmdFm.ExecuteScalarAsync();
                            if (rMed != null && rMed != DBNull.Value) idMed = Convert.ToInt32(rMed);
                        }
                    }

                    const string sqlDet = @"
                        INSERT INTO Frm_DispensacionesDetalle (
                            IdDispensacion, IdRecetaDetalle, IdMedicamento, IdLote,
                            Cantidad, CostoUnitario, ImporteCosto, ValorInstitucional,
                            Id_estatus, fecha_creacion
                        ) VALUES (
                            @idDisp, NULL, @idMed, @idLote,
                            @cant, @unit, @imp, @imp,
                            1, GETDATE()
                        );";
                    await using (var cmdDet = new SqlCommand(sqlDet, conn, tx))
                    {
                        cmdDet.Parameters.AddWithValue("@idDisp", idDisp);
                        cmdDet.Parameters.AddWithValue("@idMed", idMed > 0 ? idMed : 1);
                        cmdDet.Parameters.AddWithValue("@idLote", (object?)p.IdLote ?? DBNull.Value);
                        cmdDet.Parameters.AddWithValue("@cant", cant);
                        cmdDet.Parameters.AddWithValue("@unit", unitario);
                        cmdDet.Parameters.AddWithValue("@imp", importe);
                        await cmdDet.ExecuteNonQueryAsync();
                    }

                    if (idMed > 0)
                    {
                        // Descontar existencias en almacén central
                        const string sqlDesc = @"
                            UPDATE Frm_InventarioExistencias
                            SET CantidadExistente = CASE WHEN CantidadExistente >= @cant THEN CantidadExistente - @cant ELSE 0 END,
                                FechaUltimoMovimiento = GETDATE()
                            WHERE IdMedicamento = @idMed AND IdAlmacen = 1;";
                        await using (var cmdDesc = new SqlCommand(sqlDesc, conn, tx))
                        {
                            cmdDesc.Parameters.AddWithValue("@cant", cant);
                            cmdDesc.Parameters.AddWithValue("@idMed", idMed);
                            await cmdDesc.ExecuteNonQueryAsync();
                        }

                        // Movimiento en Kardex Frm_MovimientosInventario
                        const string sqlMov = @"
                            INSERT INTO Frm_MovimientosInventario (
                                Id_Sucursal, IdAlmacen, IdMedicamento, IdLote,
                                FechaHora, IdTipoMovimiento, Cantidad, CostoUnitario,
                                ReferenciaTipo, ReferenciaId, Observaciones,
                                Id_estatus, fecha_creacion
                            ) VALUES (
                                1, 1, @idMed, @idLote,
                                GETDATE(), 2, -@cant, @unit,
                                'DISPENSACION', @idDisp, 'Salida por dispensación en ventanilla',
                                1, GETDATE()
                            );";
                        await using (var cmdMov = new SqlCommand(sqlMov, conn, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@idMed", idMed);
                            cmdMov.Parameters.AddWithValue("@idLote", (object?)p.IdLote ?? DBNull.Value);
                            cmdMov.Parameters.AddWithValue("@cant", cant);
                            cmdMov.Parameters.AddWithValue("@unit", unitario);
                            cmdMov.Parameters.AddWithValue("@idDisp", idDisp);
                            await cmdMov.ExecuteNonQueryAsync();
                        }
                    }
                }

                // 3. Insertar Frm_ComprobantesDispensacion (Ticket oficial de ventanilla)
                const string sqlComp = @"
                    INSERT INTO Frm_ComprobantesDispensacion (
                        IdDispensacion, NumeroTicket, FechaEmision, Impreso, Reimpresiones,
                        Id_estatus, fecha_creacion
                    ) VALUES (
                        @idDisp, @numTick, GETDATE(), 1, 0,
                        1, GETDATE()
                    );";
                await using (var cmdComp = new SqlCommand(sqlComp, conn, tx))
                {
                    cmdComp.Parameters.AddWithValue("@idDisp", idDisp);
                    cmdComp.Parameters.AddWithValue("@numTick", folioTicket);
                    await cmdComp.ExecuteNonQueryAsync();
                }

                // 4. Actualizar estatus de Receta médica si aplica
                if (idReceta.HasValue && idReceta.Value > 0)
                {
                    const string sqlRx = @"
                        UPDATE Frm_Recetas
                        SET Estatus = 'SURTIDA',
                            fecha_modificacion = GETDATE()
                        WHERE IdReceta = @idRx;";
                    await using (var cmdRx = new SqlCommand(sqlRx, conn, tx))
                    {
                        cmdRx.Parameters.AddWithValue("@idRx", idReceta.Value);
                        await cmdRx.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                return (true, folioTicket, idDisp, $"Dispensación #{folioTicket} registrada exitosamente en IUBIDB_FARMACIAS_PRUEBAS.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            RegistrarError("Registrar Dispensacion", ex);
            return (false, "", 0, $"Error al registrar dispensación en la base de datos: {ex.Message}");
        }
    }

    public async Task<(bool exito, int idCompra, string mensaje)> RegistrarCompraRealAsync(
        string numeroFactura,
        string serieFactura,
        int idProveedor,
        DateTime fechaFactura,
        decimal subtotal,
        decimal iva,
        decimal total,
        List<FacturaArticuloItem> partidas,
        string observaciones,
        string usuario)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            await using var tx = conn.BeginTransaction();

            try
            {
                const string sqlComp = @"
                    INSERT INTO Frm_Compras (
                        IdProveedor, Id_Sucursal, NumeroFactura, SerieFactura,
                        FechaFactura, FechaRecepcion, Subtotal, IVA, Total,
                        Observaciones, Estatus, Id_estatus, fecha_creacion
                    ) VALUES (
                        @idProv, 1, @numFac, @serie,
                        @fechaFac, GETDATE(), @sub, @iva, @tot,
                        @obs, 'PENDIENTE DE PAGO', 1, GETDATE()
                    );
                    SELECT SCOPE_IDENTITY();";

                int idCompra;
                await using (var cmdC = new SqlCommand(sqlComp, conn, tx))
                {
                    cmdC.Parameters.AddWithValue("@idProv", idProveedor > 0 ? idProveedor : 1);
                    cmdC.Parameters.AddWithValue("@numFac", numeroFactura.Trim());
                    cmdC.Parameters.AddWithValue("@serie", string.IsNullOrWhiteSpace(serieFactura) ? (object)DBNull.Value : serieFactura.Trim());
                    cmdC.Parameters.AddWithValue("@fechaFac", fechaFactura.Date);
                    cmdC.Parameters.AddWithValue("@sub", subtotal);
                    cmdC.Parameters.AddWithValue("@iva", iva);
                    cmdC.Parameters.AddWithValue("@tot", total);
                    cmdC.Parameters.AddWithValue("@obs", string.IsNullOrWhiteSpace(observaciones) ? $"Registrada por {usuario}" : observaciones);
                    object? resId = await cmdC.ExecuteScalarAsync();
                    idCompra = Convert.ToInt32(resId);
                }

                foreach (var p in partidas)
                {
                    int? idLote = null;
                    int idMed = p.IdMedicamento;

                    if (idMed <= 0)
                    {
                        const string sqlFindMed = "SELECT TOP 1 IdMedicamento FROM Frm_Medicamentos WHERE Clave = @c OR CodigoBarras = @c OR Descripcion = @n";
                        await using (var cmdFm = new SqlCommand(sqlFindMed, conn, tx))
                        {
                            cmdFm.Parameters.AddWithValue("@c", p.Clave);
                            cmdFm.Parameters.AddWithValue("@n", p.Descripcion);
                            object? rMed = await cmdFm.ExecuteScalarAsync();
                            if (rMed != null && rMed != DBNull.Value) idMed = Convert.ToInt32(rMed);
                        }
                    }

                    if (idMed > 0 && !string.IsNullOrWhiteSpace(p.Lote))
                    {
                        const string sqlFindLote = "SELECT TOP 1 IdLote FROM Frm_Lotes WHERE IdMedicamento = @idMed AND NumeroLote = @lote";
                        await using (var cmdFl = new SqlCommand(sqlFindLote, conn, tx))
                        {
                            cmdFl.Parameters.AddWithValue("@idMed", idMed);
                            cmdFl.Parameters.AddWithValue("@lote", p.Lote.Trim());
                            object? resL = await cmdFl.ExecuteScalarAsync();
                            if (resL != null && resL != DBNull.Value) idLote = Convert.ToInt32(resL);
                        }

                        if (!idLote.HasValue)
                        {
                            DateTime cad = p.FechaCaducidadDate ?? DateTime.Today.AddYears(1);
                            const string sqlInsLote = @"
                                INSERT INTO Frm_Lotes (
                                    IdMedicamento, NumeroLote, FechaCaducidad,
                                    CostoUnitario, IdProveedor, IdCompra,
                                    Estatus, Id_estatus, fecha_creacion
                                ) VALUES (
                                    @idMed, @lote, @cad,
                                    @costo, @idProv, @idCompra,
                                    'ACTIVO', 1, GETDATE()
                                );
                                SELECT SCOPE_IDENTITY();";
                            await using (var cmdIl = new SqlCommand(sqlInsLote, conn, tx))
                            {
                                cmdIl.Parameters.AddWithValue("@idMed", idMed);
                                cmdIl.Parameters.AddWithValue("@lote", p.Lote.Trim());
                                cmdIl.Parameters.AddWithValue("@cad", cad.Date);
                                cmdIl.Parameters.AddWithValue("@costo", p.Precio);
                                cmdIl.Parameters.AddWithValue("@idProv", idProveedor > 0 ? idProveedor : (object)DBNull.Value);
                                cmdIl.Parameters.AddWithValue("@idCompra", idCompra);
                                object? resIl = await cmdIl.ExecuteScalarAsync();
                                idLote = Convert.ToInt32(resIl);
                            }
                        }
                    }

                    const string sqlDet = @"
                        INSERT INTO Frm_ComprasDetalle (
                            IdCompra, IdMedicamento, IdLote, Cantidad,
                            CostoUnitario, PorcentajeIVA, ImporteIVA, Subtotal, Total,
                            Id_estatus, fecha_creacion
                        ) VALUES (
                            @idCompra, @idMed, @idLote, @cant,
                            @costo, 0, @iva, @sub, @tot,
                            1, GETDATE()
                        );";
                    await using (var cmdDet = new SqlCommand(sqlDet, conn, tx))
                    {
                        cmdDet.Parameters.AddWithValue("@idCompra", idCompra);
                        cmdDet.Parameters.AddWithValue("@idMed", idMed > 0 ? idMed : 1);
                        cmdDet.Parameters.AddWithValue("@idLote", (object?)idLote ?? DBNull.Value);
                        cmdDet.Parameters.AddWithValue("@cant", p.Cantidad);
                        cmdDet.Parameters.AddWithValue("@costo", p.Precio);
                        cmdDet.Parameters.AddWithValue("@iva", p.IVA);
                        cmdDet.Parameters.AddWithValue("@sub", p.Subtotal);
                        cmdDet.Parameters.AddWithValue("@tot", p.Total);
                        await cmdDet.ExecuteNonQueryAsync();
                    }

                    if (idMed > 0)
                    {
                        const string sqlUpdInv = @"
                            IF EXISTS (SELECT 1 FROM Frm_InventarioExistencias WHERE IdMedicamento = @idMed AND IdAlmacen = 1)
                            BEGIN
                                UPDATE Frm_InventarioExistencias
                                SET CantidadExistente = CantidadExistente + @cant,
                                    FechaUltimoMovimiento = GETDATE()
                                WHERE IdMedicamento = @idMed AND IdAlmacen = 1;
                            END
                            ELSE
                            BEGIN
                                INSERT INTO Frm_InventarioExistencias (
                                    Id_Sucursal, IdAlmacen, IdMedicamento, IdLote,
                                    CantidadExistente, CantidadReservada, FechaUltimoMovimiento,
                                    Id_estatus, fecha_creacion
                                ) VALUES (
                                    1, 1, @idMed, @idLote,
                                    @cant, 0, GETDATE(),
                                    1, GETDATE()
                                );
                            END";
                        await using (var cmdInv = new SqlCommand(sqlUpdInv, conn, tx))
                        {
                            cmdInv.Parameters.AddWithValue("@idMed", idMed);
                            cmdInv.Parameters.AddWithValue("@idLote", (object?)idLote ?? DBNull.Value);
                            cmdInv.Parameters.AddWithValue("@cant", p.Cantidad);
                            await cmdInv.ExecuteNonQueryAsync();
                        }

                        const string sqlMov = @"
                            INSERT INTO Frm_MovimientosInventario (
                                Id_Sucursal, IdAlmacen, IdMedicamento, IdLote,
                                FechaHora, IdTipoMovimiento, Cantidad, CostoUnitario,
                                ReferenciaTipo, ReferenciaId, Observaciones,
                                Id_estatus, fecha_creacion
                            ) VALUES (
                                1, 1, @idMed, @idLote,
                                GETDATE(), 1, @cant, @costo,
                                'COMPRA', @idCompra, 'Entrada por factura ' + @numFac,
                                1, GETDATE()
                            );";
                        await using (var cmdMov = new SqlCommand(sqlMov, conn, tx))
                        {
                            cmdMov.Parameters.AddWithValue("@idMed", idMed);
                            cmdMov.Parameters.AddWithValue("@idLote", (object?)idLote ?? DBNull.Value);
                            cmdMov.Parameters.AddWithValue("@cant", p.Cantidad);
                            cmdMov.Parameters.AddWithValue("@costo", p.Precio);
                            cmdMov.Parameters.AddWithValue("@idCompra", idCompra);
                            cmdMov.Parameters.AddWithValue("@numFac", numeroFactura.Trim());
                            await cmdMov.ExecuteNonQueryAsync();
                        }
                    }
                }

                await tx.CommitAsync();
                return (true, idCompra, $"Factura {numeroFactura} registrada exitosamente e inventario incrementado en IUBIDB_FARMACIAS_PRUEBAS.");
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            RegistrarError("Registrar Compra", ex);
            return (false, 0, $"Error al registrar factura de compra: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> CrearUsuarioSistemaAsync(
        string usuario, string nombre, string email, string perfil, string password)
    {
        if (string.IsNullOrWhiteSpace(usuario) || usuario.Trim().Length < 3)
            return (false, "El nombre de usuario debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length < 3)
            return (false, "El nombre completo de la persona debe tener al menos 3 caracteres.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
            return (false, "La contraseña debe tener al menos 4 caracteres.");
        if (string.IsNullOrWhiteSpace(perfil))
            perfil = "VENTANILLA";

        usuario = usuario.Trim().ToLowerInvariant();
        nombre = nombre.Trim();
        email = (email ?? "").Trim();
        perfil = perfil.Trim().ToUpperInvariant();

        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            // 1. Validar que el usuario no exista previamente
            const string sqlCheck = "SELECT COUNT(*) FROM Frm_Usuarios WHERE LOWER(usuario) = @u";
            await using (var cmdCheck = new SqlCommand(sqlCheck, conn))
            {
                cmdCheck.Parameters.AddWithValue("@u", usuario);
                int count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                if (count > 0)
                    return (false, $"El usuario '@{usuario}' ya se encuentra registrado en el sistema. Elija otro nombre de usuario.");
            }

            // 2. Generar salt de 24 bytes y hash PBKDF2 SHA-1 de 1000 iteraciones
            byte[] saltBytes = new byte[24];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            string saltBase64 = Convert.ToBase64String(saltBytes);

            using var pbkdf2 = new System.Security.Cryptography.Rfc2898DeriveBytes(
                password, saltBytes, 1000, System.Security.Cryptography.HashAlgorithmName.SHA1);
            byte[] hashBytes = pbkdf2.GetBytes(24);
            string hashBase64 = Convert.ToBase64String(hashBytes);

            // 3. Insertar en Frm_Usuarios
            const string sqlInsert = @"
                INSERT INTO Frm_Usuarios 
                (usuario, nombre, email, clave, salt, perfil, Id_estatus, fecha_creacion, Id_usuario_creacion)
                VALUES 
                (@u, @nom, @mail, @clave, @salt, @perfil, 1, GETDATE(), 1)";

            await using (var cmdIns = new SqlCommand(sqlInsert, conn))
            {
                cmdIns.Parameters.AddWithValue("@u", usuario);
                cmdIns.Parameters.AddWithValue("@nom", nombre);
                cmdIns.Parameters.AddWithValue("@mail", string.IsNullOrWhiteSpace(email) ? (object)DBNull.Value : email);
                cmdIns.Parameters.AddWithValue("@clave", hashBase64);
                cmdIns.Parameters.AddWithValue("@salt", saltBase64);
                cmdIns.Parameters.AddWithValue("@perfil", perfil);

                await cmdIns.ExecuteNonQueryAsync();
            }

            return (true, $"Usuario '@{usuario}' ({nombre}) creado exitosamente con perfil '{perfil}'. Ya puede iniciar sesión.");
        }
        catch (Exception ex)
        {
            RegistrarError("Crear Usuario", ex);
            return (false, $"Error al registrar usuario: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> CancelarRecetaRealAsync(string folioReceta, string motivo)
    {
        if (string.IsNullOrWhiteSpace(folioReceta))
            return (false, "Capture el folio de la receta.");
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                UPDATE Frm_Recetas
                SET Estatus = 'CANCELADA',
                    Observaciones = ISNULL(Observaciones + ' | Cancelación: ', 'Cancelación: ') + @motivo,
                    fecha_modificacion = GETDATE()
                WHERE NumeroReceta = @rx";
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@rx", folioReceta.Trim());
            cmd.Parameters.AddWithValue("@motivo", string.IsNullOrWhiteSpace(motivo) ? "Cancelada por ventanilla" : motivo.Trim());
            int affected = await cmd.ExecuteNonQueryAsync();
            if (affected > 0)
                return (true, $"Receta #{folioReceta.Trim()} marcada como CANCELADA en IUBIDB_FARMACIAS_PRUEBAS.");
            else
                return (false, $"No se encontró la receta con folio '{folioReceta.Trim()}'.");
        }
        catch (Exception ex)
        {
            RegistrarError("Cancelar Receta", ex);
            return (false, $"Error al cancelar receta: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> DevolverMedicamentoRealAsync(string folioTicket, string medicamento, int cantidad)
    {
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sqlMed = "SELECT TOP 1 IdMedicamento FROM Frm_Medicamentos WHERE Descripcion LIKE '%' + @m + '%'";
            int idMed = 0;
            await using (var cmdM = new SqlCommand(sqlMed, conn))
            {
                cmdM.Parameters.AddWithValue("@m", medicamento.Trim());
                object? resM = await cmdM.ExecuteScalarAsync();
                if (resM != null && resM != DBNull.Value) idMed = Convert.ToInt32(resM);
            }

            if (idMed > 0)
            {
                const string sqlStock = @"
                    UPDATE Frm_InventarioExistencias
                    SET CantidadExistente = CantidadExistente + @cant,
                        fecha_modificacion = GETDATE()
                    WHERE IdMedicamento = @idMed";
                await using (var cmdS = new SqlCommand(sqlStock, conn))
                {
                    cmdS.Parameters.AddWithValue("@cant", cantidad);
                    cmdS.Parameters.AddWithValue("@idMed", idMed);
                    await cmdS.ExecuteNonQueryAsync();
                }

                const string sqlKardex = @"
                    INSERT INTO Frm_MovimientosInventario (
                        IdTipoMovimiento, IdMedicamento, IdLote, FechaHora,
                        Cantidad, CostoUnitario, ReferenciaTipo, ReferenciaId,
                        Observaciones, Id_estatus, fecha_creacion
                    ) VALUES (
                        1, @idMed, NULL, GETDATE(),
                        @cant, 0, 'DEVOLUCION', NULL,
                        @obs, 1, GETDATE()
                    )";
                await using (var cmdK = new SqlCommand(sqlKardex, conn))
                {
                    cmdK.Parameters.AddWithValue("@idMed", idMed);
                    cmdK.Parameters.AddWithValue("@cant", cantidad);
                    cmdK.Parameters.AddWithValue("@obs", $"Devolución de ticket #{folioTicket}");
                    await cmdK.ExecuteNonQueryAsync();
                }
            }

            return (true, $"Devolución procesada correctamente ({cantidad} pza(s) de {medicamento}). Stock reincorporado a farmacia.");
        }
        catch (Exception ex)
        {
            RegistrarError("Devolución", ex);
            return (false, $"Error al procesar devolución: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> RegistrarNuevoAfiliadoRealAsync(
        string carnet, string nombre, string curp, string dependencia)
    {
        if (string.IsNullOrWhiteSpace(carnet) || string.IsNullOrWhiteSpace(nombre))
            return (false, "Carnet y Nombre son obligatorios.");
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            string cLimpio = carnet.Trim().TrimStart('-');

            const string sqlPer = @"
                INSERT INTO Afi_Personas (nombres, primer_apellido, segundo_apellido, curp, fecha_creacion, Id_estatus)
                VALUES (@nom, '', '', @curp, GETDATE(), 1);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idPersona;
            await using (var cmdP = new SqlCommand(sqlPer, conn))
            {
                cmdP.Parameters.AddWithValue("@nom", nombre.Trim().ToUpperInvariant());
                cmdP.Parameters.AddWithValue("@curp", string.IsNullOrWhiteSpace(curp) ? (object)DBNull.Value : curp.Trim().ToUpperInvariant());
                idPersona = Convert.ToInt32(await cmdP.ExecuteScalarAsync());
            }

            const string sqlAfi = @"
                INSERT INTO Afi_Afiliados (no_afiliado, fecha_afiliacion, Id_dependencia_actual, Id_estatus, fecha_creacion)
                VALUES (@noAf, GETDATE(), 1, 1, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idAfiliado;
            await using (var cmdA = new SqlCommand(sqlAfi, conn))
            {
                cmdA.Parameters.AddWithValue("@noAf", cLimpio);
                idAfiliado = Convert.ToInt32(await cmdA.ExecuteScalarAsync());
            }

            const string sqlRel = @"
                INSERT INTO Afi_RelAfiliadosPersonas (Id_afiliado, Id_persona, Id_parentesco, Id_estatus, fecha_creacion)
                VALUES (@idAf, @idP, 1, 1, GETDATE())";
            await using (var cmdR = new SqlCommand(sqlRel, conn))
            {
                cmdR.Parameters.AddWithValue("@idAf", idAfiliado);
                cmdR.Parameters.AddWithValue("@idP", idPersona);
                await cmdR.ExecuteNonQueryAsync();
            }

            const string sqlDh = @"
                INSERT INTO SeM_Derechohabientes (carnet, Id_afiliado, nombramiento, fecha_vencimiento, Id_estatus, fecha_creacion)
                VALUES (@carnet, @idAf, 'TITULAR', DATEADD(year, 1, GETDATE()), 1, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idDh;
            await using (var cmdDh = new SqlCommand(sqlDh, conn))
            {
                cmdDh.Parameters.AddWithValue("@carnet", cLimpio);
                cmdDh.Parameters.AddWithValue("@idAf", idAfiliado);
                idDh = Convert.ToInt32(await cmdDh.ExecuteScalarAsync());
            }

            return (true, $"Afiliado #{cLimpio} ({nombre.ToUpperInvariant()}) registrado exitosamente con ID {idDh} en IUBIDB_FARMACIAS_PRUEBAS.");
        }
        catch (Exception ex)
        {
            RegistrarError("Nuevo Afiliado", ex);
            return (false, $"Error al dar de alta afiliado: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> RegistrarNuevoMedicoRealAsync(
        string nombre, string cedula, string especialidad)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return (false, "El nombre del médico es obligatorio.");
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sqlPer = @"
                INSERT INTO Afi_Personas (nombres, primer_apellido, segundo_apellido, rfc, fecha_creacion, Id_estatus)
                VALUES (@nom, '', '', @ced, GETDATE(), 1);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idP;
            await using (var cmdP = new SqlCommand(sqlPer, conn))
            {
                cmdP.Parameters.AddWithValue("@nom", nombre.Trim().ToUpperInvariant());
                cmdP.Parameters.AddWithValue("@ced", string.IsNullOrWhiteSpace(cedula) ? (object)DBNull.Value : cedula.Trim());
                idP = Convert.ToInt32(await cmdP.ExecuteScalarAsync());
            }

            const string sqlMed = @"
                INSERT INTO SeM_Medicos (Id_persona, Id_especialidad, Id_estatus, fecha_creacion)
                VALUES (@idP, 1, 1, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idMed;
            await using (var cmdM = new SqlCommand(sqlMed, conn))
            {
                cmdM.Parameters.AddWithValue("@idP", idP);
                idMed = Convert.ToInt32(await cmdM.ExecuteScalarAsync());
            }

            return (true, $"Médico Dr(a). {nombre.ToUpperInvariant()} registrado con éxito con ID #{idMed} en IUBIDB_FARMACIAS_PRUEBAS.");
        }
        catch (Exception ex)
        {
            RegistrarError("Nuevo Médico", ex);
            return (false, $"Error al registrar médico: {ex.Message}");
        }
    }

    public async Task<(bool ok, string mensaje)> RegistrarNuevoMedicamentoRealAsync(
        string clave, string nombre, string sustancia, string presentacion)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return (false, "La descripción del fármaco es obligatoria.");
        try
        {
            await using var conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string sql = @"
                INSERT INTO Frm_Medicamentos (
                    Clave, CodigoBarras, Descripcion, Presentacion, SustanciaActiva,
                    TipoControl, ValorInstitucional, Id_estatus, fecha_creacion
                ) VALUES (
                    @c, @c, @d, @p, @s,
                    'LIBRE', 0, 1, GETDATE()
                );
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            int idMed;
            await using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@c", string.IsNullOrWhiteSpace(clave) ? $"MED-{DateTime.Now:yyMMddHHmmss}" : clave.Trim());
                cmd.Parameters.AddWithValue("@d", nombre.Trim().ToUpperInvariant());
                cmd.Parameters.AddWithValue("@p", string.IsNullOrWhiteSpace(presentacion) ? "PIEZA" : presentacion.Trim());
                cmd.Parameters.AddWithValue("@s", string.IsNullOrWhiteSpace(sustancia) ? (object)DBNull.Value : sustancia.Trim());
                idMed = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            }

            const string sqlEx = "INSERT INTO Frm_InventarioExistencias (IdMedicamento, Id_Sucursal, CantidadExistente, StockMinimo, StockMaximo, Id_estatus, fecha_creacion) VALUES (@id, 1, 0, 5, 100, 1, GETDATE())";
            await using (var cmdEx = new SqlCommand(sqlEx, conn))
            {
                cmdEx.Parameters.AddWithValue("@id", idMed);
                await cmdEx.ExecuteNonQueryAsync();
            }

            return (true, $"Medicamento '{nombre.ToUpperInvariant()}' registrado con éxito con ID #{idMed} en IUBIDB_FARMACIAS_PRUEBAS.");
        }
        catch (Exception ex)
        {
            RegistrarError("Nuevo Medicamento", ex);
            return (false, $"Error al registrar medicamento: {ex.Message}");
        }
    }
}

public class UsuarioLogueado
{
    public string ID_Usuario { get; set; } = "";
    public string NombreCompleto { get; set; } = "";
    public string Perfil { get; set; } = "";
    public int? FarmaciaID { get; set; }
    public List<int> MenusPermitidos { get; set; } = new();
}
