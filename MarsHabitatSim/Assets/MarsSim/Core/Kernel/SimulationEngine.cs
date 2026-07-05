using System;
using System.Collections.Generic;
using System.Linq;
using MarsSim.Core.Params;

namespace MarsSim.Core
{
    /// <summary>
    /// Owns the clock, stores, buses, modules and history; advances the world one fixed step
    /// at a time. Deterministic for a given (scenario, master seed).
    ///
    /// Step pipeline:
    ///   1. environment module ticks first (writes ctx.Env)
    ///   2. PreTick: modules declare power offers/demands + labor demands
    ///   3. PowerBus + LaborPool resolve (priority allocation)
    ///   4. Tick: modules move mass/energy at granted fractions
    ///   5. History.CommitStep + built-in bookkeeping series
    /// </summary>
    public sealed class SimulationEngine
    {
        public SimClock Clock { get; }
        public StoreSet Stores { get; } = new StoreSet();
        public PowerBus Power { get; } = new PowerBus();
        public LaborPool Labor { get; } = new LaborPool();
        public History History { get; } = new History();
        public EventLog Events { get; } = new EventLog();
        public ParameterRegistry Params { get; }
        public ulong MasterSeed { get; }

        private readonly List<SimModule> _modules = new();
        private readonly Dictionary<string, SimModule> _byId = new();
        private readonly SimContext _ctx;
        private bool _initialized;

        /// <summary>Modules in tick order (environment first, then insertion order).</summary>
        public IReadOnlyList<SimModule> Modules => _modules;

        public SimulationEngine(ParameterRegistry parameters, DateTime epochUtc, double dtSeconds, ulong masterSeed)
        {
            Params = parameters;
            MasterSeed = masterSeed;
            Clock = new SimClock(epochUtc, dtSeconds);
            History.Configure(Clock.DtSols);
            _ctx = new SimContext
            {
                Clock = Clock,
                Stores = Stores,
                Power = Power,
                Labor = Labor,
                History = History,
                Events = Events,
                Params = Params,
            };
        }

        public T Add<T>(T module, string id) where T : SimModule
        {
            if (_byId.ContainsKey(id)) throw new InvalidOperationException($"duplicate module id '{id}'");
            module.Id = id;
            module.Bind(this, MasterSeed);
            _modules.Add(module);
            _byId[id] = module;
            if (_initialized) module.Init(_ctx);   // modules can arrive mid-run (new landings)
            return module;
        }

        public SimModule Find(string id) => _byId.TryGetValue(id, out var m) ? m : null;
        public T Find<T>() where T : SimModule => _modules.OfType<T>().FirstOrDefault();
        public IEnumerable<T> FindAll<T>() where T : SimModule => _modules.OfType<T>();

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            foreach (var m in _modules) m.Init(_ctx);
            Events.Log(0, EventSeverity.Milestone, "Engine", "Simulation initialized");
        }

        public void Step()
        {
            if (!_initialized) Initialize();

            Power.BeginStep();
            Labor.BeginStep();

            // Environment first (fills ctx.Env), then everything else.
            foreach (var m in _modules)
                if (m is IEnvironmentProvider && m.Active) m.PreTick(_ctx);
            foreach (var m in _modules)
                if (m is IEnvironmentProvider && m.Active) m.Tick(_ctx);

            foreach (var m in _modules)
                if (!(m is IEnvironmentProvider) && m.Active) m.PreTick(_ctx);

            Power.Resolve(Clock.DtHours);
            Labor.Resolve();

            foreach (var m in _modules)
                if (!(m is IEnvironmentProvider) && m.Active) m.Tick(_ctx);

            RecordBuiltins();
            History.CommitStep();
            Clock.Advance();
        }

        /// <summary>Advance a whole number of steps (timelapse driver / batch runs).</summary>
        public void Step(int n)
        {
            for (int i = 0; i < n; i++) Step();
        }

        /// <summary>Run until the given mission sol (batch trade studies).</summary>
        public void RunToSol(double sol, Action<double> progress = null)
        {
            int guard = 0;
            while (Clock.Sol < sol && guard++ < 50_000_000)
            {
                Step();
                if (progress != null && Clock.StepCount % 1000 == 0) progress(Clock.Sol);
            }
        }

        private void RecordBuiltins()
        {
            var h = History;
            h.Record("power.offered", "Generation available", "kW", Power.GenerationOfferedKw);
            h.Record("power.used", "Generation used", "kW", Power.GenerationUsedKw);
            h.Record("power.demand", "Power demand", "kW", Power.DemandKw);
            h.Record("power.unmet", "Unmet power demand", "kW", Power.UnmetKw);
            h.Record("power.curtailed", "Curtailed generation", "kW", Power.CurtailedKw);
            h.Record("power.battery_soc", "Battery state of charge", "%", Power.Battery.Soc * 100.0);
            h.Record("labor.crew_available", "Crew hours available", "h/step", Labor.CrewHoursAvailable);
            h.Record("labor.robot_available", "Robot hours available", "h/step", Labor.RobotHoursAvailable);
            h.Record("labor.unmet", "Unmet labor", "crew-eq h/step", Labor.UnmetCrewEqHours);

            foreach (var s in Stores.All)
                h.Record($"store.{s.Id}", s.DisplayName, "kg", s.AmountKg);

            var env = _ctx.Env;
            h.Record("env.tau", "Optical depth τ", "", env.OpticalDepthTau);
            h.Record("env.ghi", "Global horizontal irradiance", "W/m²", env.GlobalHorizontalWm2);
            h.Record("env.air_temp", "Air temperature", "°C", env.AirTemperatureC);
            h.Record("env.pressure", "Surface pressure", "Pa", env.PressurePa);
        }

        /// <summary>
        /// Mass conservation audit. Per store the ledger must balance exactly:
        /// deposited - withdrawn == current amount (vented mass never entered the store).
        /// </summary>
        public double ConservationError()
        {
            double err = 0;
            foreach (var s in Stores.All)
                err += Math.Abs(s.TotalDepositedKg - s.TotalWithdrawnKg - s.AmountKg);
            return err;
        }

        public SimContext Context => _ctx;
    }

    /// <summary>Marker: module that fills ctx.Env; ticked before all others.</summary>
    public interface IEnvironmentProvider { }
}
