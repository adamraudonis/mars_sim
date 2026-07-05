namespace MarsSim.Core
{
    /// <summary>
    /// Physical constants and unit conversions used across the simulation.
    /// Time on Mars is measured in sols; energy in kWh; mass in kg.
    /// </summary>
    public static class Units
    {
        /// <summary>Mean solar day on Mars, seconds (Allison &amp; McEwen 2000).</summary>
        public const double SolSeconds = 88775.244;

        public const double SolHours = SolSeconds / 3600.0;          // 24.6598 h
        public const double EarthDaySeconds = 86400.0;
        public const double SolsPerEarthDay = EarthDaySeconds / SolSeconds;

        /// <summary>Mars tropical year, sols (668.5991) — governs the Ls cycle.</summary>
        public const double SolsPerMarsYear = 668.5991;

        /// <summary>Mars synodic period w.r.t. Earth, Earth days (~26 months).</summary>
        public const double SynodicPeriodDays = 779.94;

        public const double MarsGravity = 3.71;                      // m/s^2
        public const double Deg2Rad = System.Math.PI / 180.0;
        public const double Rad2Deg = 180.0 / System.Math.PI;

        /// <summary>kcal content of 1 kg of standard packaged crew food (dry-ish mix). Set from params at runtime.</summary>
        public const double KcalPerKgFoodDefault = 4000.0;

        public static double KwToKwhPerStep(double kw, double dtSeconds) => kw * (dtSeconds / 3600.0);
        public static double KwhPerStepToKw(double kwh, double dtSeconds) => kwh / (dtSeconds / 3600.0);
    }
}
