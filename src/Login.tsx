import { useState } from "react";
import logo from "./assets/ipecol-full.png";

export default function Login({ onEnter }: { onEnter: () => void }) {
  const [u, setU] = useState("");
  const [p, setP] = useState("");
  const inp = "w-full border-0 border-b-2 border-sand-100 bg-transparent px-1 py-2 text-sm focus:outline-none focus:border-wine-800 placeholder:text-ink/40";
  return (
    <div className="min-h-screen grid place-items-center p-6 bg-gradient-to-br from-wine-900 via-wine-800 to-wine-950">
      <form onSubmit={(e) => { e.preventDefault(); onEnter(); }} className="w-full max-w-sm bg-white rounded-xl shadow-2xl shadow-black/30 px-8 py-9 space-y-6">
        <img src={logo} alt="IPECOL - Instituto de Pensiones de los Servidores Públicos del Estado de Colima" className="mx-auto h-40 w-auto" />
        <div className="space-y-4">
          <input className={inp} value={u} onChange={(e) => setU(e.target.value)} placeholder="Usuario" autoComplete="username" />
          <input type="password" className={inp} value={p} onChange={(e) => setP(e.target.value)} placeholder="Contraseña" autoComplete="current-password" />
        </div>
        <button className="w-full bg-wine-800 hover:bg-wine-700 text-white font-semibold tracking-widest text-sm rounded-md py-3 transition-colors">INICIAR SESIÓN</button>
        <p className="text-[11px] text-center text-ink/50">Farmacia institucional · IUBIDB</p>
      </form>
    </div>
  );
}
