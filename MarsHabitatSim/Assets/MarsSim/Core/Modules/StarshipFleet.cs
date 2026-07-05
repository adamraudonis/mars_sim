using System;
using System.Collections.Generic;
using System.Linq;
using MarsSim.Core.Params;

namespace MarsSim.Core.Modules
{
    public enum ShipRole { Cargo, CrewTransport, TankerDepot }

    public sealed class LandedShip
    {
        public string Name;
        public ShipRole Role;
        public double LandedSol;
        public bool ContributesHabitatVolume;
        public bool FueledForReturn;
        public bool Departed;
    }

    /// <summary>
    /// Starships on the surface. Ships are pressurized volume (crew ships double as early
    /// habitat), tankage (depot capacity), and — for crew rotation — the return vehicle whose
    /// propellant demand drives the whole ISRU sizing question. Return readiness is evaluated
    /// against the depot; at each Earth-return window, a fueled crew ship can depart.
    /// </summary>
    public sealed class StarshipFleet : SimModule
    {
        public override string DisplayName => "Starship fleet";

        private readonly List<LandedShip> _ships = new();
        public IReadOnlyList<LandedShip> Ships => _ships;

        private Param _pressVolume, _returnPropTonnes, _shipHabVolumeFraction, _tankCapacityTonnes,
                      _shipHotelPowerKw;
        private Store _ch4, _lox;
        private Habitat _hab;

        /// <summary>Sols at which Earth-return windows open (from scenario/campaign).</summary>
        public List<double> ReturnWindowSols { get; } = new();

        public override void Init(SimContext ctx)
        {
            var p = ctx.Params;
            _pressVolume = p.GetOrRegister("starship.pressurized_volume_m3", "Ship pressurized volume", 1000, "m3",
                "SpaceX: ~1000 m3 claimed for crew configuration (company claim)");
            _shipHabVolumeFraction = p.GetOrRegister("starship.habitat_usable_fraction", "Usable fraction of ship volume as habitat", 0.6, "",
                "Outfitting, tankage domes, airlocks reduce net habitable volume");
            _returnPropTonnes = p.GetOrRegister("starship.return_propellant_tonnes", "Propellant to reach Earth-return trajectory", 1200, "t",
                "Mars ascent+TEI ~6.9 km/s; full V3 tanks give ~9.3 km/s at 145 t burnout (research campaign, corrected)");
            _tankCapacityTonnes = p.GetOrRegister("starship.tank_capacity_tonnes", "Ship propellant tank capacity", 1550, "t",
                "Starship V3 as flown: 1550 t (research campaign corrected vs stretch-variant tables)");
            _shipHotelPowerKw = p.GetOrRegister("starship.ship_hotel_power_kw", "Power each landed ship contributes (deployable panels)", 30, "kW",
                "Estimate: ship-mounted arrays/fuel cells for surface ops — breaks the buildup power-labor deadlock");

            _ch4 = ctx.Stores.GetOrCreate("depot_ch4", Resource.CH4, 0);
            _lox = ctx.Stores.GetOrCreate("depot_lox", Resource.LOX, 0);
            _hab = Engine.Find<Habitat>();
        }

        public LandedShip Land(SimContext ctx, string name, ShipRole role, bool contributesVolume)
        {
            var ship = new LandedShip
            {
                Name = name,
                Role = role,
                LandedSol = ctx.Clock.Sol,
                ContributesHabitatVolume = contributesVolume,
            };
            _ships.Add(ship);

            if (contributesVolume && _hab != null)
                _hab.AddVolume(ctx, _pressVolume.Value * _shipHabVolumeFraction.Value, arrivesPressurized: true);

            // Every landed ship's empty tanks add depot capacity.
            double capKg = _tankCapacityTonnes.Value * 1000.0;
            _ch4.AddCapacity(capKg / (1 + Engine.Params.V("starship.of_ratio")));
            _lox.AddCapacity(capKg * Engine.Params.V("starship.of_ratio") / (1 + Engine.Params.V("starship.of_ratio")));

            Log(ctx, EventSeverity.Milestone, $"{name} landed ({role})");
            return ship;
        }

        /// <summary>Fraction of the next return flight's propellant already produced.</summary>
        public double ReturnPropellantFraction
        {
            get
            {
                double of = Engine.Params.V("starship.of_ratio");
                double needKg = _returnPropTonnes.Value * 1000.0;
                double needCh4 = needKg / (1 + of);
                double needLox = needKg * of / (1 + of);
                double frac = Math.Min(_ch4.AmountKg / needCh4, _lox.AmountKg / needLox);
                return Math.Min(1.0, frac);
            }
        }

        public override void PreTick(SimContext ctx)
        {
            // Landed ships supply hotel power (day and night) — the seed power that lets
            // robots build the farm that replaces it.
            int present = _ships.Count(s => !s.Departed);
            if (present > 0)
                ctx.Power.Offer(present * _shipHotelPowerKw.Value);
        }

        public override void Tick(SimContext ctx)
        {
            // Return-readiness bookkeeping + departures at windows.
            var returnShip = _ships.FirstOrDefault(s => s.Role == ShipRole.CrewTransport && !s.Departed);
            if (returnShip != null && !returnShip.FueledForReturn && ReturnPropellantFraction >= 1.0)
            {
                returnShip.FueledForReturn = true;
                Log(ctx, EventSeverity.Milestone, $"{returnShip.Name} fully fueled for Earth return");
            }

            foreach (double window in ReturnWindowSols)
            {
                if (Math.Abs(ctx.Clock.Sol - window) < ctx.DtSols && returnShip is { FueledForReturn: true, Departed: false })
                {
                    double of = Engine.Params.V("starship.of_ratio");
                    double needKg = _returnPropTonnes.Value * 1000.0;
                    _ch4.Withdraw(needKg / (1 + of));
                    _lox.Withdraw(needKg * of / (1 + of));
                    returnShip.Departed = true;
                    Log(ctx, EventSeverity.Milestone, $"{returnShip.Name} departed for Earth at the return window");
                }
            }

            Record(ctx, "fleet.ships", "Ships on surface", "", _ships.Count(s => !s.Departed));
            Record(ctx, "fleet.return_prop", "Return propellant readiness", "%", ReturnPropellantFraction * 100);
        }

        public override string StatusLine =>
            $"{_ships.Count(s => !s.Departed)} ships on surface, return propellant {ReturnPropellantFraction:P0}";

        public override IEnumerable<(string, double, string)> KeyFigures
        {
            get
            {
                yield return ("Ships on surface", _ships.Count(s => !s.Departed), "");
                yield return ("Return propellant", ReturnPropellantFraction * 100, "%");
                yield return ("Required", _returnPropTonnes.Value, "t");
            }
        }
    }
}
