using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// Cryogenic propellant storage (surface tank farm and/or a parked Starship's tanks).
    /// Zero-boiloff needs continuous cryocooler power; when that power is shed, boiloff
    /// vents mass — a quiet failure mode that couples the power architecture to the
    /// return-flight schedule (exactly the kind of interaction this sim exists to expose).
    /// </summary>
    public sealed class PropellantDepot : SimModule
    {
        public override string DisplayName => "Propellant depot";

        private Param _boiloffPerSol, _zboKwPerTonne;
        private Store _ch4, _lox;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _boiloffPerSol = p.GetOrRegister("starship.boiloff_fraction_per_sol", "Passive cryo boiloff on Mars surface", 0.0025, "fraction/sol",
                "~0.25%/sol estimate for insulated steel tanks on Mars (research campaign; SpaceX has not published)");
            _zboKwPerTonne = p.GetOrRegister("starship.zbo_kw_per_tonne", "Zero-boiloff cryocooler power", 0.026, "kW/t stored",
                "~40 kW cryocooler per full 1550 t ship (research campaign, speculative)");

            _ch4 = ctx.Stores.GetOrCreate("depot_ch4", Resource.CH4, 0);
            _lox = ctx.Stores.GetOrCreate("depot_lox", Resource.LOX, 0);
        }

        public double StoredTonnes => (_ch4.AmountKg + _lox.AmountKg) / 1000.0;

        public override void PreTick(SimContext ctx)
        {
            double kw = StoredTonnes * _zboKwPerTonne.Value;
            if (kw > 0) ctx.Power.Request(this, kw, LoadPriority.High);
        }

        public override void Tick(SimContext ctx)
        {
            double grant = ctx.Power.GrantedFraction(this);
            if (grant < 0.999 && StoredTonnes > 0)
            {
                // Unpowered fraction of the tank farm boils off.
                double frac = _boiloffPerSol.Value * ctx.DtSols * (1 - grant);
                double lostCh4 = _ch4.Withdraw(_ch4.AmountKg * frac);
                double lostLox = _lox.Withdraw(_lox.AmountKg * frac);
                if ((lostCh4 + lostLox) > 50)
                    Log(ctx, EventSeverity.Warning, $"Cryocoolers underpowered — {(lostCh4 + lostLox):F0} kg propellant boiled off");
            }

            Record(ctx, "depot.ch4", "CH₄ stored", "t", _ch4.AmountKg / 1000.0);
            Record(ctx, "depot.lox", "LOX stored", "t", _lox.AmountKg / 1000.0);
            Record(ctx, "depot.total", "Propellant stored", "t", StoredTonnes);
        }

        public override string StatusLine =>
            $"{_ch4.AmountKg / 1000.0:F0} t CH₄ + {_lox.AmountKg / 1000.0:F0} t LOX";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("CH₄", _ch4.AmountKg / 1000.0, "t");
                yield return ("LOX", _lox.AmountKg / 1000.0, "t");
                yield return ("ZBO power", StoredTonnes * _zboKwPerTonne.Value, "kW");
            }
        }
    }
}
