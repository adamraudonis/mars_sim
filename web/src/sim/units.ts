/** Physical constants and unit conversions. Time on Mars in sols; energy kWh; mass kg. */
export const Units = {
  /** Mean solar day on Mars, seconds (Allison & McEwen 2000). */
  solSeconds: 88775.244,
  get solHours() {
    return this.solSeconds / 3600;
  },
  earthDaySeconds: 86400,
  /** Mars tropical year, sols — governs the Ls cycle. */
  solsPerMarsYear: 668.5991,
  /** Mars synodic period w.r.t. Earth, Earth days (~26 months). */
  synodicPeriodDays: 779.94,
  marsGravity: 3.71,
  deg2rad: Math.PI / 180,
  rad2deg: 180 / Math.PI,
} as const;

export const SOL_HOURS = Units.solSeconds / 3600; // 24.6598
