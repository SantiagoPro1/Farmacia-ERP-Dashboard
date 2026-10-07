import mark from "./assets/ipecol-mark.png";
import emblem from "./assets/ipecol-emblem.png";

export default function Logo({ compact = false }: { dark?: boolean; compact?: boolean }) {
  return (
    <div className="bg-white rounded-xl flex items-center justify-center p-2.5 shadow-sm">
      <img src={compact ? emblem : mark} alt="IPECOL" className={compact ? "h-9 w-auto" : "h-20 w-auto"} />
    </div>
  );
}
