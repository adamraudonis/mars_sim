using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    /// <summary>
    /// The Mars surface environment at the settlement site: sun geometry, insolation vs
    /// optical depth, dust storm process, air/ground temperature, pressure cycle, radiation.
    ///
    /// Insolation model: top-of-atmosphere flux from the Mars-Sun distance (SimClock orbit),
    /// direct beam via Beer's law with airmass, global horizontal via a normalized net-flux
    /// approximation calibrated to Appelbaum &amp; Flood (NASA TM-102299) tables. At L2 a
    /// stochastic regional/global dust storm process modulates tau; at L1 tau follows the
    /// seasonal climatology only; at L0 tau is a single mean value.
    /// </summary>
    public sealed class MarsEnvironment : SimModule, IEnvironmentProvider
    {
        public override string DisplayName => "Mars environment";
        public override FidelityLevel MaxFidelity => FidelityLevel.L2_Physics;

        public double LatitudeDeg { get; set; } = 40.0;   // Arcadia Planitia-class site
        public double ElevationKm { get; set; } = -3.0;

        // Storm process state
        private bool _regionalStorm;
        private bool _globalStorm;
        private double _stormStartSol;
        private double _stormEndSol;
        private double _stormTau;
        private double _lastStormCheckSol = -1;

        private Param _tauClearNorthSpring, _tauDustySeason, _tauMean, _pGlobalStormPerYear,
                      _stormTauGlobal, _stormDurGlobal, _pRegionalPerSol, _stormTauRegional,
                      _stormDurRegional, _netFluxK, _groundAlbedo,
                      _tMeanAnnual, _tSeasonalAmp, _tDiurnalAmp, _pMean, _pSeasonalAmp,
                      _doseSurface, _windMean, _solarConstant;

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _solarConstant = p.GetOrRegister("mars_environment.solar_constant_1au", "Solar constant at 1 AU", 1361, "W/m2",
                "Kopp & Lean 2011, TSI composite");
            _tauClearNorthSpring = p.GetOrRegister("mars_environment.tau_clear", "Background optical depth, clear season (Ls 0-135)", 0.4, "tau",
                "MER/MSL climatology (Lemmon et al. 2015)");
            _tauDustySeason = p.GetOrRegister("mars_environment.tau_dusty", "Background optical depth, dusty season (Ls 180-330)", 0.9, "tau",
                "MER/MSL climatology (Lemmon et al. 2015)");
            _tauMean = p.GetOrRegister("mars_environment.tau_mean", "Mean optical depth (L0)", 0.6, "tau",
                "Derived: annual mean of climatology");
            _pGlobalStormPerYear = p.GetOrRegister("mars_environment.global_storm_prob_per_year", "Global dust storm probability per Mars year", 0.33, "probability",
                "Zurek & Martin 1993 (~1 per 3 Mars years)");
            _stormTauGlobal = p.GetOrRegister("mars_environment.global_storm_tau", "Global storm peak optical depth", 6.0, "tau",
                "2018 storm: tau 8-11 at Opportunity site (Guzewich et al. 2019); 5-6 typical");
            _stormDurGlobal = p.GetOrRegister("mars_environment.global_storm_duration", "Global storm duration", 90, "sols",
                "2018 & 2001 storm observations");
            _pRegionalPerSol = p.GetOrRegister("mars_environment.regional_storm_prob_per_sol", "Regional storm probability per sol (dusty season)", 0.004, "probability",
                "Estimate: several regional storms per dusty season (Kahre et al. 2017)");
            _stormTauRegional = p.GetOrRegister("mars_environment.regional_storm_tau", "Regional storm peak optical depth", 2.5, "tau",
                "MER observations of regional events");
            _stormDurRegional = p.GetOrRegister("mars_environment.regional_storm_duration", "Regional storm duration", 20, "sols",
                "MER observations of regional events");
            _netFluxK = p.GetOrRegister("mars_environment.net_flux_k", "Net-flux extinction coefficient (GHI approximation)", 0.26, "",
                "Calibrated to Appelbaum & Flood 1990 (NASA TM-102299) net flux tables; Mars dust is strongly forward-scattering (Pollack), so tau=5 cuts GHI ~3x, not e^-5");
            _groundAlbedo = p.GetOrRegister("mars_environment.ground_albedo", "Surface albedo", 0.25, "",
                "TES albedo maps, mid-latitude average");
            _tMeanAnnual = p.GetOrRegister("mars_environment.t_mean_annual", "Annual mean air temperature (site)", -55, "degC",
                "REMS/Viking mid-latitude records");
            _tSeasonalAmp = p.GetOrRegister("mars_environment.t_seasonal_amp", "Seasonal temperature amplitude", 20, "degC",
                "Viking lander records vs Ls");
            _tDiurnalAmp = p.GetOrRegister("mars_environment.t_diurnal_amp", "Diurnal temperature half-amplitude", 35, "degC",
                "REMS diurnal range 60-80 degC");
            _pMean = p.GetOrRegister("mars_environment.pressure_mean", "Mean surface pressure (site)", 720, "Pa",
                "Viking/REMS, adjusted for low-elevation site");
            _pSeasonalAmp = p.GetOrRegister("mars_environment.pressure_seasonal_amp", "Seasonal pressure amplitude", 70, "Pa",
                "Viking annual CO2 condensation cycle");
            _doseSurface = p.GetOrRegister("human_factors.surface_dose_msv_per_sol", "GCR surface dose rate (unshielded)", 0.70, "mSv/sol",
                "MSL RAD: 0.67-0.71 mSv/day (Hassler et al. 2014)");
            _windMean = p.GetOrRegister("mars_environment.wind_mean", "Mean near-surface wind speed", 7, "m/s",
                "Viking/InSight anemometry");
        }

        public override void Tick(SimContext ctx)
        {
            var env = ctx.Env;
            double ls = ctx.Clock.Ls;
            double timeOfSol = ctx.Clock.TimeOfSol;

            // --- Optical depth ---
            double tau = EffectiveFidelity switch
            {
                FidelityLevel.L0_Distilled => _tauMean.Value,
                _ => SeasonalTau(ls),
            };
            if (EffectiveFidelity == FidelityLevel.L2_Physics)
            {
                UpdateStormProcess(ctx, ls);
                if (_globalStorm || _regionalStorm)
                    tau = Math.Max(tau, StormTauNow(ctx.Clock.Sol));
            }
            env.OpticalDepthTau = tau;
            env.GlobalDustStorm = _globalStorm;

            // --- Sun geometry & insolation ---
            double rAu = SimClock.MarsSunDistanceAu(ls);
            double toa = _solarConstant.Value / (rAu * rAu);
            env.TopOfAtmosphereWm2 = toa;

            double el = SimClock.SunElevationDeg(ls, LatitudeDeg, timeOfSol);
            env.SunElevationDeg = el;
            env.SunAzimuthDeg = SimClock.SunAzimuthDeg(ls, LatitudeDeg, timeOfSol);

            if (el > 0.1)
            {
                double sinEl = Math.Sin(el * Units.Deg2Rad);
                double airmass = 1.0 / Math.Max(sinEl, 0.05);
                env.DirectNormalWm2 = toa * Math.Exp(-tau * airmass);
                // Normalized net flux approximation (beam + diffuse), calibrated vs the
                // Appelbaum & Flood tables. The airmass in the global term is capped because
                // scattered (diffuse) light dominates at low sun — A&F give f(z=85°, tau=0.5)
                // ≈ 0.15-0.2, which a raw Beer-law exponent would drive to zero.
                double effAirmass = Math.Min(airmass, 3.0);
                // min(1,...) keeps the albedo bounce from exceeding the no-atmosphere limit at low tau.
                env.GlobalHorizontalWm2 = toa * sinEl * Math.Min(1.0,
                    Math.Exp(-_netFluxK.Value * tau * effAirmass) * (1.0 + 0.15 * _groundAlbedo.Value));
            }
            else
            {
                env.DirectNormalWm2 = 0;
                env.GlobalHorizontalWm2 = 0;
            }

            // --- Temperature & pressure ---
            double seasonal = _tSeasonalAmp.Value * Math.Sin((ls - 70) * Units.Deg2Rad) * (LatitudeDeg >= 0 ? 1 : -1);
            double diurnal = _tDiurnalAmp.Value * Math.Cos((timeOfSol - 14.0 / Units.SolHours) * 2 * Math.PI);
            env.AirTemperatureC = _tMeanAnnual.Value + seasonal + diurnal;
            env.GroundTemperatureC = env.AirTemperatureC + (el > 0 ? 10 : -10);

            // Viking-style annual double wave (CO2 caps): minima near Ls 150, secondary near Ls 350.
            env.PressurePa = _pMean.Value
                             + _pSeasonalAmp.Value * (0.7 * Math.Sin((ls - 240) * Units.Deg2Rad)
                                                      + 0.3 * Math.Sin(2 * (ls - 240) * Units.Deg2Rad));
            env.WindSpeedMs = _windMean.Value * (_globalStorm ? 2.0 : 1.0);
            env.SurfaceDoseMsvPerSol = _doseSurface.Value;
        }

        private double SeasonalTau(double ls)
        {
            // Smooth blend between clear (Ls ~0-135) and dusty (Ls ~180-330) seasons.
            double dusty = 0.5 * (1 + Math.Cos((ls - 255) * Units.Deg2Rad)); // peaks at Ls 255
            dusty = Math.Pow(dusty, 1.5);
            return _tauClearNorthSpring.Value + (_tauDustySeason.Value - _tauClearNorthSpring.Value) * dusty;
        }

        private void UpdateStormProcess(SimContext ctx, double ls)
        {
            double sol = ctx.Clock.Sol;
            // Check once per sol for storm onset (deterministic per seed).
            if (Math.Floor(sol) > Math.Floor(_lastStormCheckSol))
            {
                bool stormSeason = ls > 180 && ls < 330;
                if (!_globalStorm && !_regionalStorm && stormSeason)
                {
                    double pGlobalPerSol = _pGlobalStormPerYear.Value / (Units.SolsPerMarsYear * (150.0 / 360.0));
                    if (Rng.Chance(pGlobalPerSol))
                    {
                        _globalStorm = true;
                        _stormTau = _stormTauGlobal.Value * Rng.Range(0.8, 1.3);
                        _stormStartSol = sol;
                        _stormEndSol = sol + _stormDurGlobal.Value * Rng.Range(0.7, 1.4);
                        Log(ctx, EventSeverity.Critical, $"GLOBAL DUST STORM began (tau {_stormTau:F1}, forecast {_stormEndSol - sol:F0} sols)");
                    }
                    else if (Rng.Chance(_pRegionalPerSol.Value))
                    {
                        _regionalStorm = true;
                        _stormTau = _stormTauRegional.Value * Rng.Range(0.7, 1.4);
                        _stormStartSol = sol;
                        _stormEndSol = sol + _stormDurRegional.Value * Rng.Range(0.5, 1.5);
                        Log(ctx, EventSeverity.Warning, $"Regional dust storm began (tau {_stormTau:F1})");
                    }
                }
                else if ((_globalStorm || _regionalStorm) && sol > _stormEndSol)
                {
                    Log(ctx, EventSeverity.Milestone, _globalStorm ? "Global dust storm cleared" : "Regional dust storm cleared");
                    _globalStorm = false;
                    _regionalStorm = false;
                }
            }
            _lastStormCheckSol = sol;
        }

        private double StormTauNow(double sol)
        {
            // Ramp up over ~5 sols, plateau, decay over the last third — using the ACTUAL
            // randomized duration of this storm, not the nominal parameter.
            double elapsed = sol - _stormStartSol;
            double remaining = _stormEndSol - sol;
            double total = Math.Max(1, _stormEndSol - _stormStartSol);
            if (elapsed < 5) return _stormTau * (0.3 + 0.7 * elapsed / 5.0);
            if (remaining < total * 0.33) return _stormTau * Math.Max(0.1, remaining / (total * 0.33));
            return _stormTau;
        }

        public override string StatusLine =>
            _globalStorm ? "GLOBAL DUST STORM" : _regionalStorm ? "Regional dust storm" : "Nominal";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Latitude", LatitudeDeg, "°N");
                yield return ("Optical depth τ", Engine.Context.Env.OpticalDepthTau, "");
                yield return ("Air temp", Engine.Context.Env.AirTemperatureC, "°C");
            }
        }
    }
}
