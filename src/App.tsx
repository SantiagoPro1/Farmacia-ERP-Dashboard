import { useState } from "react";
import Logo from "./Logo";
import Login from "./Login";

type Item = { key: string; name: string; sub: string; dose: string; qty: number; stock: number; lot: string; exp: string; price: number; done?: boolean };
const seed: Item[] = [
  { key: "010.000.5185", name: "Metformina", sub: "Tableta 850 mg · Caja c/30", dose: "1 cada 12 h", qty: 2, stock: 48, lot: "MTF26817", exp: "MAR 2027", price: 86 },
  { key: "010.000.1042", name: "Losartán", sub: "Tableta 50 mg · Caja c/30", dose: "1 cada 24 h", qty: 1, stock: 22, lot: "LSR2519", exp: "ENE 2027", price: 124.5 },
  { key: "010.000.2127", name: "Atorvastatina", sub: "Tableta 20 mg · Caja c/20", dose: "1 por la noche", qty: 1, stock: 16, lot: "ATV0616", exp: "JUN 2027", price: 198 },
  { key: "010.000.4158", name: "Insulina glargina", sub: "100 UI/ml · Pluma 3 ml", dose: "18 UI nocturna", qty: 3, stock: 2, lot: "IGL7782", exp: "DIC 2026", price: 275 },
  { key: "010.000.1098", name: "Tiras reactivas", sub: "Glucosa capilar · Frasco c/50", dose: "2 mediciones/día", qty: 2, stock: 0, lot: "—", exp: "", price: 49.75 },
];
const nav = ["Inicio", "Ventanilla", "Pacientes", "Recetas", "Inventario", "Compras", "Vales", "Reportes", "Configuración"];
const money = (n: number) => n.toLocaleString("es-MX", { style: "currency", currency: "MXN" });
const inp = "rounded-lg border border-sand-100 bg-white px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gold-500";

const Card = ({ title, icon, aside, children, className = "" }: { title?: string; icon?: string; aside?: React.ReactNode; children: React.ReactNode; className?: string }) => (
  <section className={`bg-white rounded-2xl border border-sand-100 shadow-sm ${className}`}>
    {title && (
      <header className="flex items-center gap-2.5 px-5 pt-4">
        {icon && <span className="w-7 h-7 rounded-lg bg-rose-100 text-wine-800 grid place-items-center text-xs font-bold">{icon}</span>}
        <h3 className="font-semibold text-sm text-wine-950">{title}</h3>
        <div className="ml-auto text-xs text-ink/60">{aside}</div>
      </header>
    )}
    <div className="p-5">{children}</div>
  </section>
);
const Field = ({ l, v }: { l: string; v: string }) => (
  <div><div className="text-[10px] uppercase tracking-wider text-ink/50">{l}</div><div className="text-sm font-semibold mt-0.5">{v}</div></div>
);

function Ventanilla() {
  const [items, setItems] = useState(seed);
  const [code, setCode] = useState("");
  const [pay, setPay] = useState("Efectivo");
  const [saved, setSaved] = useState(false);
  const subtotal = items.filter((i) => i.done).reduce((s, i) => s + i.qty * i.price, 0);
  const absorb = subtotal * 0.8;
  const patient = subtotal - absorb;
  const set = (k: string, patch: Partial<Item>) => setItems(items.map((i) => (i.key === k ? { ...i, ...patch } : i)));
  const avail = items.filter((i) => i.stock >= i.qty && !i.done);
  const scan = () => {
    const m = items.find((i) => i.key === code.trim() || i.name.toLowerCase() === code.trim().toLowerCase());
    if (m && m.stock >= m.qty) set(m.key, { done: true });
    setCode("");
  };
  return (
    <div className="grid xl:grid-cols-[1fr_320px] gap-5">
      <div className="space-y-5 min-w-0">
        <div className="grid lg:grid-cols-2 gap-5">
          <Card title="Identificación rápida del derechohabiente" icon="ID" aside="Carnet 04-285119">
            <div className="flex gap-4">
              <div className="w-11 h-11 rounded-full bg-rose-200 text-wine-800 font-bold grid place-items-center shrink-0">MG</div>
              <div className="min-w-0">
                <div className="font-semibold">María Elena González Ruiz</div>
                <span className="inline-block mt-1 text-[11px] bg-gold-100 text-wine-900 font-semibold rounded-full px-2 py-0.5">Vigente al 31 dic 2026</span>
              </div>
            </div>
            <div className="grid grid-cols-3 gap-3 mt-4"><Field l="CURP" v="GORM740219MCMNZR08" /><Field l="Afiliación" v="Pensionada · Titular" /><Field l="Dependencia" v="Gobierno del Estado" /></div>
            <div className="flex gap-2 mt-4 text-[11px]"><span className="border border-rose-300 text-wine-800 rounded px-2 py-0.5">Alergia: penicilina</span><span className="border border-sand-100 rounded px-2 py-0.5 text-ink/60">Sin adeudos</span></div>
          </Card>
          <Card title="Verificación de receta" icon="Rx" aside="RX-2026-0">
            <div className="grid grid-cols-2 gap-3"><Field l="Médico" v="Dra. Adriana Silva Núñez" /><Field l="Cédula" v="6219047" /><Field l="Diagnóstico" v="E11.9 · Diabetes mellitus tipo 2" /><Field l="Fecha de emisión" v="01 oct 2026" /></div>
            <div className="mt-4 text-[11px] bg-gold-100 text-wine-900 rounded-lg px-3 py-1.5 font-semibold">Receta válida · Firma electrónica verificada</div>
          </Card>
        </div>

        <div className="flex flex-wrap items-center gap-3 bg-wine-900 rounded-2xl px-5 py-3 text-white">
          <div className="mr-auto"><div className="text-sm font-semibold">Captura ágil para venta inmediata</div><div className="text-[11px] text-rose-200">Escanea lo que el paciente se lleva ahora</div></div>
          <input value={code} onChange={(e) => setCode(e.target.value)} onKeyDown={(e) => e.key === "Enter" && scan()} placeholder="Escanear o ingresar código de barras" className="flex-1 min-w-48 max-w-md rounded-lg bg-white text-ink px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gold-500" />
          <button onClick={scan} className="bg-gold-500 hover:brightness-110 text-wine-950 font-semibold text-sm rounded-lg px-4 py-2">Agregar</button>
        </div>

        <Card title="Medicamentos prescritos" icon="Rx" aside={<span className="inline-flex gap-3"><span>● Disponible</span><span className="text-gold-500">● Parcial / vale</span></span>} className="overflow-x-auto">
          <table className="w-full min-w-[720px]">
            <thead><tr className="text-[10px] uppercase tracking-wider text-ink/50 text-left">{["Clave", "Medicamento", "Dosis", "Presc.", "Exist.", "Lote / cad.", "Importe", "Acción"].map((h) => <th key={h} className="pb-2 font-semibold">{h}</th>)}</tr></thead>
            <tbody>
              {items.map((i) => {
                const ok = i.stock >= i.qty;
                return (
                  <tr key={i.key} className={`border-t border-sand-100 text-sm ${!ok ? "bg-gold-100/40" : ""}`}>
                    <td className="py-3 text-xs text-ink/60">{i.key}</td>
                    <td><div className="font-semibold">{i.name}</div><div className="text-[11px] text-ink/50">{i.sub}</div></td>
                    <td className="text-xs">{i.dose}</td>
                    <td className="font-semibold">{i.qty}</td>
                    <td><span className={`rounded-full px-2 py-0.5 text-xs font-bold ${i.stock === 0 ? "bg-rose-200 text-wine-900" : ok ? "bg-rose-100 text-wine-800" : "bg-gold-100 text-gold-500"}`}>{i.stock}</span></td>
                    <td className="text-xs"><div className="font-semibold">{i.lot}</div><div className="text-ink/50">{i.exp}</div></td>
                    <td className="font-semibold">{money(i.qty * i.price)}</td>
                    <td>
                      {i.done ? <span className="text-xs font-semibold text-wine-800">Surtido ✓</span> : ok ? (
                        <button onClick={() => set(i.key, { done: true })} className="text-xs font-semibold bg-rose-100 text-wine-800 hover:bg-rose-200 rounded-md px-3 py-1.5">Surtir</button>
                      ) : <button className="text-xs font-semibold bg-gold-100 text-wine-900 hover:brightness-95 rounded-md px-3 py-1.5">Generar vale</button>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <div className="flex justify-between items-center mt-4 text-xs text-ink/60">
            <span>{items.filter((i) => i.stock >= i.qty).length} de {items.length} partidas disponibles · {items.filter((i) => i.stock < i.qty).length} vales sugeridos</span>
            <button onClick={() => setItems(items.map((i) => (i.stock >= i.qty ? { ...i, done: true } : i)))} disabled={!avail.length} className="font-semibold text-wine-800 disabled:opacity-40">Surtir disponibles</button>
          </div>
        </Card>
      </div>

      <Card title="Resumen de cobro" icon="$" aside="FV-2026-001924" className="self-start">
        <dl className="text-sm space-y-2">
          <div className="flex justify-between"><dt>Subtotal</dt><dd>{money(subtotal)}</dd></div>
          <div className="flex justify-between text-wine-800"><dt>Absorción institucional (80%)</dt><dd>− {money(absorb)}</dd></div>
          <div className="flex justify-between"><dt>Aportación del paciente</dt><dd>{money(patient)}</dd></div>
        </dl>
        <div className="mt-4 rounded-xl bg-rose-100 p-4 flex items-end justify-between">
          <div><div className="text-xs font-semibold text-wine-800">Total a cobrar</div><div className="text-[10px] text-ink/50">IVA incluido</div></div>
          <div className="font-display text-3xl font-bold text-wine-900">{money(patient)}</div>
        </div>
        <label className="block mt-4 text-[10px] uppercase tracking-wider text-ink/50">Método de pago
          <select value={pay} onChange={(e) => setPay(e.target.value)} className={`${inp} w-full mt-1 normal-case tracking-normal text-sm text-ink`}><option>Efectivo</option><option>Tarjeta</option><option>Descuento por nómina</option></select>
        </label>
        <div className="mt-4 space-y-2">
          <button onClick={() => setSaved(true)} disabled={!subtotal} className="w-full bg-wine-800 hover:bg-wine-700 disabled:bg-rose-300 text-white font-semibold text-sm rounded-lg py-2.5">Guardar operación</button>
          <button disabled={!saved} className="w-full bg-gold-100 text-wine-900 font-semibold text-sm rounded-lg py-2.5 disabled:opacity-50">Imprimir ticket</button>
          <button onClick={() => { setItems(seed); setSaved(false); }} className="w-full border border-sand-100 text-sm rounded-lg py-2.5">Cancelar</button>
        </div>
        {saved && <p className="mt-3 text-xs text-wine-800 font-semibold">Operación guardada.</p>}
        <p className="mt-3 text-[11px] text-ink/50">Operación auditada y asociada al usuario activo.</p>
      </Card>
    </div>
  );
}

function Placeholder({ name }: { name: string }) {
  return <Card><div className="text-center py-16 text-sm text-ink/50">Módulo «{name}» · base en cero, sin registros.</div></Card>;
}

export default function App() {
  const [auth, setAuth] = useState(false);
  const [mod, setMod] = useState("Ventanilla");
  const [open, setOpen] = useState(true);
  if (!auth) return <Login onEnter={() => setAuth(true)} />;
  return (
    <div className="min-h-screen flex">
      <aside className={`${open ? "w-60" : "w-[72px]"} shrink-0 bg-wine-900 text-white transition-all flex flex-col sticky top-0 h-screen`}>
        <div className="p-4"><Logo dark compact={!open} /></div>
        <div className="px-4 pb-2 text-[10px] tracking-widest text-rose-300">{open && "OPERACIÓN"}</div>
        <nav className="flex-1 px-3 space-y-1 overflow-y-auto">
          {nav.map((m) => (
            <button key={m} onClick={() => setMod(m)} title={m} className={`w-full text-left rounded-lg px-3 py-2.5 text-sm font-medium truncate ${mod === m ? "bg-white text-wine-900" : "text-rose-100 hover:bg-wine-800"}`}>{open ? m : m[0]}</button>
          ))}
        </nav>
        {open && (
          <div className="m-3 rounded-xl bg-wine-800 p-3 text-xs">
            <div className="text-[10px] text-rose-300 tracking-wider">VENTANILLA · <span className="text-gold-500">En línea</span></div>
            <div className="font-semibold mt-1">Farmacia Central · Colima</div>
            <div className="text-rose-200">Terminal 04 · Caja 02</div>
          </div>
        )}
        <button onClick={() => setOpen(!open)} className="mx-3 mb-3 rounded-lg border border-wine-700 text-xs text-rose-200 py-2">{open ? "Contraer menú" : "»"}</button>
      </aside>
      <main className="flex-1 min-w-0">
        <header className="flex items-center gap-4 px-6 py-3 bg-white border-b border-sand-100 sticky top-0 z-10">
          <div className="hidden md:block"><div className="text-[11px] text-ink/50">Farmacia / Operación</div><h1 className="font-semibold text-wine-950">{mod === "Ventanilla" ? "Ventanilla y facturación" : mod}</h1></div>
          <input placeholder="Buscar por CURP, carnet, receta o código de barras…" className="flex-1 max-w-xl mx-auto rounded-full border border-sand-100 bg-sand-50 px-4 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-gold-500" />
          <div className="flex items-center gap-2.5">
            <div className="w-9 h-9 rounded-full bg-rose-200 text-wine-800 text-xs font-bold grid place-items-center">LM</div>
            <div className="hidden lg:block leading-tight text-xs"><div className="font-semibold">Lic. Laura Méndez</div><button onClick={() => setAuth(false)} className="text-wine-700 underline">Cerrar sesión</button></div>
          </div>
        </header>
        <div className="p-6">
          {mod === "Ventanilla" && (
            <>
              <h2 className="font-display text-3xl text-wine-950">Dispensación y facturación inmediata</h2>
              <p className="text-sm text-ink/60 mb-5 mt-1">Valida al derechohabiente, surte la receta y registra el cobro en una sola operación.</p>
              <Ventanilla />
            </>
          )}
          {mod !== "Ventanilla" && <Placeholder name={mod} />}
        </div>
      </main>
    </div>
  );
}
