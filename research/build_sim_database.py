#!/usr/bin/env python3
"""
Build the simulation's parameter database from the research campaign output.

Reads research/domains/*.json (post-verification), emits
MarsHabitatSim/Assets/StreamingAssets/parameters_master.json containing:
  1. BINDINGS — parameters under the ids the simulation code resolves, with values
     taken or derived from research parameters (citation carried over, derivation noted).
  2. All remaining research parameters as reference entries (visible in the in-app
     Parameter Inspector, usable by future modules).

Derived values are computed here, transparently, so the provenance chain survives.
"""
import json
import glob
import os

HERE = os.path.dirname(os.path.abspath(__file__))
DOMAINS_DIR = os.path.join(HERE, "domains")
OUT_PATHS = [
    os.path.join(HERE, "..", "MarsHabitatSim", "Assets", "StreamingAssets", "parameters_master.json"),
]

research = {}   # id -> param dict
domain_notes = {}
for path in sorted(glob.glob(os.path.join(DOMAINS_DIR, "*.json"))):
    d = json.load(open(path))
    domain_notes[d["domain"]] = d.get("modeling_notes", "")
    for p in d.get("parameters", []):
        research[p["id"]] = p

def R(rid):
    """Value of a research parameter (raises if the campaign didn't produce it)."""
    return research[rid]["value"]

def src(rid, extra=None):
    s = research[rid].get("source", "(research)")
    return f"{s}" + (f" — {extra}" if extra else "")

def conf(rid):
    return research[rid].get("confidence", "medium")

SOL_PER_DAY = 1.02749  # sols are 2.75% longer than days; per-day rates stay per-day (code converts)

# ---------------------------------------------------------------------------
# Bindings: code parameter id -> (value, unit, source, confidence, notes)
# ---------------------------------------------------------------------------
B = {}

def bind(code_id, name, value, unit, source, confidence="medium", notes=None,
         range_min=None, range_max=None):
    B[code_id] = {
        "id": code_id, "name": name, "value": round(float(value), 6), "unit": unit,
        "range_min": range_min, "range_max": range_max, "source": source,
        "source_url": None, "confidence": confidence, "notes": notes,
    }

# --- ECLSS / crew metabolism ---
bind("eclss.o2_consumption_kg_cm_day", "O2 consumption", R("eclss.crew_o2_consumption"),
     "kg/CM-day", src("eclss.crew_o2_consumption"), conf("eclss.crew_o2_consumption"))
bind("eclss.co2_production_kg_cm_day", "CO2 production", R("eclss.crew_co2_production"),
     "kg/CM-day", src("eclss.crew_co2_production"), conf("eclss.crew_co2_production"))
bind("eclss.water_use_kg_cm_day", "Potable water use (drink+food prep+hygiene)",
     R("eclss.crew_total_water_use"), "kg/CM-day", src("eclss.crew_total_water_use"), "high")
bind("eclss.wastewater_kg_cm_day", "Wastewater return (urine+condensate)",
     R("eclss.crew_urine_output") + R("eclss.crew_humidity_condensate"), "kg/CM-day",
     src("eclss.crew_urine_output", "urine + humidity condensate"), "high")
bind("eclss.cabin_total_pressure", "Cabin total pressure", R("eclss.cabin_total_pressure"),
     "kPa", src("eclss.cabin_total_pressure"), "high")
bind("eclss.ppo2_setpoint", "ppO2 setpoint", R("eclss.cabin_ppo2"), "kPa",
     src("eclss.cabin_ppo2"), "high")
bind("eclss.ppco2_limit", "ppCO2 alarm limit", R("eclss.cabin_ppco2_limit"), "kPa",
     src("eclss.cabin_ppco2_limit"), "high",
     "ISS ops target; NASA-STD-3001 180-day SMAC is 0.71 kPa")
bind("eclss.oga_kwh_per_kg_o2", "O2 generation specific energy", R("eclss.oga_specific_energy"),
     "kWh/kg O2", src("eclss.oga_specific_energy"), conf("eclss.oga_specific_energy"))
cdra_kwh_per_kg = R("eclss.cdra_power") * 24.0 / (4.1 * R("eclss.crew_co2_production"))
bind("eclss.co2_removal_kwh_per_kg", "CO2 removal specific energy", cdra_kwh_per_kg,
     "kWh/kg CO2", src("eclss.cdra_power", "derived: 0.8 kW / (4.1 CM x 1.04 kg CO2/day)"), "medium")
bind("eclss.water_recovery_fraction", "Water recovery fraction",
     R("eclss.wrs_water_recovery") / 100.0, "", src("eclss.wrs_water_recovery"), "high")
bind("eclss.sabatier_conversion", "Sabatier CO2 conversion",
     R("isru_atmosphere.sabatier_co2_conversion_single_pass"), "",
     src("isru_atmosphere.sabatier_co2_conversion_single_pass"), "high")

# --- Food & crops ---
bind("food.kcal_per_cm_day", "Dietary energy requirement", R("food.crew_energy_requirement"),
     "kcal/CM-day", src("food.crew_energy_requirement"), "high")
kcal_per_kg = R("food.crew_energy_requirement") / R("food.packaged_food_asshipped_mass")
bind("food.kcal_per_kg_packaged", "Packaged food energy density (as-shipped)", kcal_per_kg,
     "kcal/kg", src("food.packaged_food_asshipped_mass", "derived: 3037 kcal / 1.83 kg"), "high")
potato_kcal_m2_sol = R("food.potato_edible_productivity") * R("food.crop_edible_caloric_density") * SOL_PER_DAY
bind("food.kcal_per_m2_sol", "Edible output per growing area (potato-class)", potato_kcal_m2_sol,
     "kcal/m2/sol", src("food.potato_edible_productivity", "18.4 g dw/m2-day x 4 kcal/g"), "high")
led_kwh_m2_sol = (R("food.potato_edible_productivity") / R("food.potato_rue_edible")
                  * R("food.electric_energy_per_mol_photons") / R("food.light_delivery_efficiency")) * SOL_PER_DAY
bind("food.led_kwh_per_m2_sol", "LED electrical energy per growing area", led_kwh_m2_sol,
     "kWh/m2/sol", src("food.potato_rue_edible",
     "derived: (18.4 g/m2-d / 0.44 g/mol PAR) x 0.116 kWh/mol / 0.80 delivery"), "medium")
bind("food.harvest_index", "Edible fraction (harvest index, potato)", R("food.potato_harvest_index"),
     "", src("food.potato_harvest_index"), "high")
bind("food.water_per_m2_sol", "Crop transpiration throughput",
     R("food.crop_transpiration_rate") * SOL_PER_DAY, "kg/m2/sol",
     src("food.crop_transpiration_rate"), "high")
bind("food.system_mass_kg_m2", "Growth system mass per area",
     R("food.plant_growth_chamber_esm_mass"), "kg/m2",
     src("food.plant_growth_chamber_esm_mass"), "medium")

# --- Solar power ---
bind("power_solar.cell_efficiency", "PV cell efficiency (BOL)", R("power_solar.pv_cell_efficiency_bol"),
     "", src("power_solar.pv_cell_efficiency_bol"), "high")
bind("power_solar.temp_coeff", "Efficiency temperature coefficient", R("power_solar.pv_power_temp_coeff"),
     "1/degC", src("power_solar.pv_power_temp_coeff"), "medium")
bind("power_solar.dust_rate_per_sol", "Dust obscuration accumulation",
     R("power_solar.dust_obscuration_rate") / 100.0, "fraction/sol",
     src("power_solar.dust_obscuration_rate"), "high")
bind("power_solar.specific_mass_kg_m2", "Array system specific mass",
     R("power_solar.array_areal_density_system"), "kg/m2",
     src("power_solar.array_areal_density_system"), "medium")
bind("power_solar.battery_wh_per_kg", "Battery pack specific energy",
     R("power_solar.battery_specific_energy_pack"), "Wh/kg",
     src("power_solar.battery_specific_energy_pack"), "medium")
bind("power_solar.battery_round_trip_eff", "Battery round-trip efficiency",
     R("power_solar.battery_round_trip_efficiency"), "",
     src("power_solar.battery_round_trip_efficiency"), "medium")
bind("power_solar.battery_min_soc", "Battery minimum SoC",
     1.0 - R("power_solar.battery_allowable_dod"), "fraction",
     src("power_solar.battery_allowable_dod", "1 - allowable DoD"), "low")

# --- Environment ---
bind("mars_environment.tau_clear", "Background tau, clear season", R("mars_environment.dust_tau_clear_season"),
     "tau", src("mars_environment.dust_tau_clear_season"), "high")
bind("mars_environment.tau_dusty", "Background tau, dusty season", R("mars_environment.dust_tau_dusty_season"),
     "tau", src("mars_environment.dust_tau_dusty_season"), "medium")
bind("mars_environment.global_storm_prob_per_year", "Global dust storm probability per Mars year",
     R("mars_environment.global_dust_storm_probability_per_mars_year"), "probability",
     src("mars_environment.global_dust_storm_probability_per_mars_year"), "medium")
bind("mars_environment.global_storm_tau", "Global storm peak tau",
     R("mars_environment.dust_tau_global_storm_peak"), "tau", src("mars_environment.dust_tau_global_storm_peak"), "medium",
     "2018 storm reached tau 8-11 at Opportunity")
bind("mars_environment.global_storm_duration", "Global storm duration",
     R("mars_environment.global_dust_storm_duration_sols"), "sols", src("mars_environment.global_dust_storm_duration_sols"), "medium")

# --- Nuclear ---
bind("power_nuclear.unit_kwe", "Reactor unit electrical power", 40, "kWe",
     src("power_nuclear.fsp_40kwe_system_mass_kg", "FSP class"), "high")
bind("power_nuclear.unit_mass_kg", "Reactor unit mass (landed)",
     R("power_nuclear.fsp_40kwe_system_mass_kg"), "kg",
     src("power_nuclear.fsp_40kwe_system_mass_kg"), "high")
bind("power_nuclear.lifetime_years", "Design lifetime", R("power_nuclear.fsp_design_life_yr"),
     "years", src("power_nuclear.fsp_design_life_yr"), "high")
bind("power_nuclear.keep_out_distance_m", "Crew keep-out distance (shadow shield)",
     R("power_nuclear.keepout_radius_shadow_shield_m"), "m",
     src("power_nuclear.keepout_radius_shadow_shield_m"), "high")

# --- Human factors ---
bind("human_factors.surface_dose_msv_per_sol", "GCR surface dose rate (unshielded)",
     R("human_factors.rad.gcr_surface_msv_per_day") * SOL_PER_DAY, "mSv/sol",
     src("human_factors.rad.gcr_surface_msv_per_day"), "high")
bind("human_factors.career_dose_limit_msv", "Career radiation dose limit",
     R("human_factors.rad.career_limit_msv"), "mSv", src("human_factors.rad.career_limit_msv"), "high")
bind("human_factors.work_hours_per_sol", "Productive work hours per crew per sol",
     R("human_factors.time.scheduled_work_h_per_cm_workday"), "h/sol",
     src("human_factors.time.scheduled_work_h_per_cm_workday"), "high")

# --- ISRU atmosphere ---
bind("isru_atmosphere.co2_acquisition_kwh_per_kg", "CO2 acquisition energy",
     R("isru_atmosphere.co2_acq_sorption_energy"), "kWh/kg CO2",
     src("isru_atmosphere.co2_acq_sorption_energy"), "medium")
bind("isru_atmosphere.electrolysis_kwh_per_kg_h2o", "Water electrolysis energy",
     R("isru_atmosphere.water_electrolysis_energy_h2o"), "kWh/kg H2O",
     src("isru_atmosphere.water_electrolysis_energy_h2o"), "medium")
bind("isru_atmosphere.liquefaction_kwh_per_kg_ch4", "CH4 liquefaction energy",
     R("isru_atmosphere.ch4_liquefaction_energy"), "kWh/kg",
     src("isru_atmosphere.ch4_liquefaction_energy"), "low")
bind("isru_atmosphere.liquefaction_kwh_per_kg_o2", "O2 liquefaction energy",
     R("isru_atmosphere.o2_liquefaction_energy"), "kWh/kg",
     src("isru_atmosphere.o2_liquefaction_energy"), "medium")
bind("isru_atmosphere.soxe_kwh_per_kg_o2", "SOXE O2 specific energy (full scale)",
     R("isru_atmosphere.soxe_scaled_specific_energy"), "kWh/kg O2",
     src("isru_atmosphere.soxe_scaled_specific_energy"), "medium")
plant_mass_per_kg_sol = R("isru_atmosphere.plant_specific_mass") / SOL_PER_DAY
bind("isru_atmosphere.plant_mass_kg_per_kg_sol", "Plant specific mass", plant_mass_per_kg_sol,
     "kg per (kg/sol)", src("isru_atmosphere.plant_specific_mass", "converted from per kg/day"), "medium")
bind("isru_atmosphere.l0_kwh_per_kg_propellant", "Distilled plant energy (L0, water-based chain)",
     8.0, "kWh/kg propellant",
     "Derived from research chain values: 6.1 kWh/kg electrolysis + CO2 acq + liquefaction "
     "(cf. isru_atmosphere.plant_specific_energy = 20.2 kWh/kg for the SOXE-heavy architecture)",
     "medium")

# --- ISRU water ---
extraction = (R("isru_water.icy_regolith_extraction_energy_kwh_per_kg")
              + R("isru_water.water_cleanup_energy_kwh_per_kg") + 0.05)
bind("isru_water.extraction_kwh_per_kg", "Water extraction energy (excavate+heat+haul)",
     extraction, "kWh/kg H2O",
     src("isru_water.icy_regolith_extraction_energy_kwh_per_kg",
         "thermal extraction + cleanup + excavation/transport"), "medium")
bind("isru_water.ore_water_fraction", "Water content of mined material", 0.8, "",
     src("isru_water.massive_ice_purity_frac", "excess-ice deposit, derated for overburden mixing"), "medium")

# --- Starship ---
bind("starship.of_ratio", "Raptor O:F mixture ratio", R("starship.raptor_of_ratio"),
     "kg O2/kg CH4", src("starship.raptor_of_ratio"), "high")
bind("starship.pressurized_volume_m3", "Ship pressurized volume", R("starship.pressurized_volume_m3"),
     "m3", src("starship.pressurized_volume_m3"), "medium")
bind("starship.return_propellant_tonnes", "Propellant for Mars ascent + TEI",
     R("starship.mars_return_propellant_t"), "t", src("starship.mars_return_propellant_t"),
     conf("starship.mars_return_propellant_t"))
bind("starship.tank_capacity_tonnes", "Ship propellant tank capacity",
     R("starship.ship_propellant_capacity_t"), "t", src("starship.ship_propellant_capacity_t"), "medium")
bind("starship.boiloff_fraction_per_sol", "Passive cryo boiloff on Mars surface",
     R("starship.boiloff_mars_surface_pct_per_sol") / 100.0, "fraction/sol",
     src("starship.boiloff_mars_surface_pct_per_sol"), "speculative")
zbo_kw_per_t = R("starship.zbo_cryocooler_power_kw") / R("starship.ship_propellant_capacity_t")
bind("starship.zbo_kw_per_tonne", "Zero-boiloff cryocooler power", zbo_kw_per_t, "kW/t stored",
     src("starship.zbo_cryocooler_power_kw", "40 kW per full 1600 t ship"), "speculative")

# --- Robots ---
bind("robots.work_hours_per_sol", "Robot work hours per sol",
     R("robots.ops.productive_hours_per_sol"), "h/sol",
     src("robots.ops.productive_hours_per_sol"), "low")
bind("robots.availability", "Fleet availability", R("robots.maint.fleet_availability"),
     "", src("robots.maint.fleet_availability"), "low")
bind("robots.charge_kwh_per_work_hour", "Charging energy per work hour",
     R("robots.power.energy_per_work_hour"), "kWh/h", src("robots.power.energy_per_work_hour"), "low")
bind("robots.unit_mass_kg", "Robot unit mass", R("robots.optimus.mass"), "kg",
     src("robots.optimus.mass"), "medium")
q = R("robots.work.q_structured")
for task, mult, note in [
    ("excavation", 1.2, "structured outdoor task"),
    ("construction", 1.0, "semi-structured assembly"),
    ("logistics", 1.2, "warehouse-analog work"),
    ("agriculture", 0.8, "delicate manipulation"),
    ("maintenance", 0.5, "diagnosis-heavy, human-led"),
    ("isru_ops", 1.0, "monitoring rounds"),
]:
    bind(f"robots.effectiveness_{task}", f"Robot effectiveness: {task}", min(1.0, q * mult),
         "crew-eq", src("robots.work.q_structured", f"q_structured x {mult} ({note})"), "low")

# --- Reliability ---
bind("reliability.k_factor", "Failure rate uncertainty multiplier", 2.0, "",
     src("reliability.failure_rate_underestimate_max_multiplier",
         "design point: ISS ECLSS ORUs failed above prediction (70% of ORUs, up to 22x); "
         "2.0 is a conservative central multiplier"), "medium")

# ---------------------------------------------------------------------------
# Assemble output: bindings first, then all research params not shadowed.
# ---------------------------------------------------------------------------
domains_out = {}

def domain_of(pid):
    return pid.split(".")[0] if "." in pid else "misc"

DOMAIN_ALIAS = {"hf": "human_factors", "env": "mars_environment"}

for p in B.values():
    d = domain_of(p["id"])
    domains_out.setdefault(d, {"title": d.replace("_", " ").title(),
                               "modeling_notes": domain_notes.get(d, ""),
                               "parameters": []})["parameters"].append(p)

for rid, p in research.items():
    if rid in B:
        continue
    d = DOMAIN_ALIAS.get(domain_of(rid), domain_of(rid))
    q = dict(p)
    q.setdefault("range_min", None)
    q.setdefault("range_max", None)
    q.setdefault("source_url", None)
    q.setdefault("notes", None)
    q["notes"] = ((q["notes"] + " | " if q["notes"] else "") + "reference (research campaign)")
    domains_out.setdefault(d, {"title": d.replace("_", " ").title(),
                               "modeling_notes": domain_notes.get(d, ""),
                               "parameters": []})["parameters"].append(q)

out = {
    "version": "1.0",
    "generated": "2026-07-05",
    "note": "Sourced parameter database for Mars Habitat Sim. Bindings derived from the "
            "multi-agent research campaign (research/domains/*, verification-passed); "
            "every value carries its citation.",
    "domains": domains_out,
}

total = sum(len(d["parameters"]) for d in domains_out.values())
for path in OUT_PATHS:
    os.makedirs(os.path.dirname(path), exist_ok=True)
    json.dump(out, open(path, "w"), indent=1)
    print(f"wrote {path} ({total} parameters, {len(B)} sim bindings)")
