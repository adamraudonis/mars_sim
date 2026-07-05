using System;
using System.Collections.Generic;
using MarsSim.Core.Params;

namespace MarsSim.Core
{
    /// <summary>
    /// Fidelity ladder. Every module supports a subset; the engine falls back to the nearest
    /// implemented level below the request.
    /// L0 = distilled averages (fast, campaign-scale studies)
    /// L1 = analytic daily/seasonal models (default)
    /// L2 = physics-based sub-sol models (expensive, for exploration + distillation)
    /// </summary>
    public enum FidelityLevel { L0_Distilled = 0, L1_Analytic = 1, L2_Physics = 2 }

    public enum ModuleHealth { Nominal, Degraded, Failed, Offline }

    /// <summary>
    /// Base class for every simulated subsystem. Lifecycle per step:
    ///   PreTick  - declare power offers/demands and labor demands
    ///   Tick     - exchange mass with stores / habitat atmosphere at the granted fractions
    /// Modules self-describe (description, health, key figures) so UI and trade runner are
    /// fully generic: adding a module requires no UI changes.
    /// </summary>
    public abstract class SimModule
    {
        public string Id { get; internal set; }
        public virtual string DisplayName => Id;
        public SimulationEngine Engine { get; internal set; }
        protected ParameterRegistry P => Engine.Params;
        protected SimRandom Rng { get; private set; }

        public FidelityLevel Fidelity { get; set; } = FidelityLevel.L1_Analytic;
        public virtual FidelityLevel MaxFidelity => FidelityLevel.L1_Analytic;
        public FidelityLevel EffectiveFidelity => (FidelityLevel)Math.Min((int)Fidelity, (int)MaxFidelity);

        public ModuleHealth Health { get; protected internal set; } = ModuleHealth.Nominal;

        /// <summary>Overall capacity multiplier from failures/degradation, applied by subclasses.</summary>
        public double CapacityFactor { get; protected internal set; } = 1.0;

        /// <summary>Whether the module has been assembled/commissioned (LaunchCampaign flips this).</summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Fraction of time this module's hardware is actually operating (failures accrue per
        /// operating hour — dormant equipment does not consume its MTBF). Override per module.
        /// </summary>
        public virtual double FailureDutyCycle => Active ? 1.0 : 0.0;

        internal void Bind(SimulationEngine engine, ulong masterSeed)
        {
            Engine = engine;
            Rng = new SimRandom(masterSeed, Id);
        }

        public virtual void Init(SimContext ctx) { }
        public virtual void PreTick(SimContext ctx) { }
        public abstract void Tick(SimContext ctx);

        /// <summary>Short human status line for the systems panel.</summary>
        public virtual string StatusLine => Health.ToString();

        /// <summary>Key numbers for the systems panel: (label, value, unit).</summary>
        public virtual IEnumerable<(string label, double value, string unit)> KeyFigures
            => Array.Empty<(string, double, string)>();

        protected void Record(SimContext ctx, string id, string name, string unit, double v)
            => ctx.History.Record(id, name, unit, v);

        protected void Log(SimContext ctx, EventSeverity sev, string message)
            => ctx.Events.Log(ctx.Clock.Sol, sev, DisplayName, message);
    }

    /// <summary>Everything a module needs during a step (no service locators, easy to test).</summary>
    public sealed class SimContext
    {
        public SimClock Clock { get; internal set; }
        public double DtSeconds => Clock.DtSeconds;
        public double DtHours => Clock.DtHours;
        public double DtSols => Clock.DtSols;

        public StoreSet Stores { get; internal set; }
        public PowerBus Power { get; internal set; }
        public LaborPool Labor { get; internal set; }
        public History History { get; internal set; }
        public EventLog Events { get; internal set; }
        public ParameterRegistry Params { get; internal set; }

        /// <summary>Snapshot of the environment for this step (set by MarsEnvironment before others tick).</summary>
        public EnvironmentState Env { get; internal set; } = new EnvironmentState();
    }

    /// <summary>Per-step environmental conditions every module can read.</summary>
    public sealed class EnvironmentState
    {
        public double SunElevationDeg;
        public double SunAzimuthDeg;
        /// <summary>Direct-beam irradiance on a normal surface at the ground, W/m2.</summary>
        public double DirectNormalWm2;
        /// <summary>Global horizontal irradiance at the ground, W/m2.</summary>
        public double GlobalHorizontalWm2;
        /// <summary>Top-of-atmosphere flux, W/m2.</summary>
        public double TopOfAtmosphereWm2;
        public double OpticalDepthTau;
        public bool GlobalDustStorm;
        public double AirTemperatureC;
        public double GroundTemperatureC;
        public double PressurePa;
        public double WindSpeedMs;
        /// <summary>GCR surface dose rate, mSv/sol (before habitat shielding).</summary>
        public double SurfaceDoseMsvPerSol;
    }
}
