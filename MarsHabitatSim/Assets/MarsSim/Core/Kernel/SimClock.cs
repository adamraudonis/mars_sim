using System;

namespace MarsSim.Core
{
    /// <summary>
    /// Mission clock. Tracks mission-elapsed sols, time within the sol, areocentric solar
    /// longitude Ls (season), and the mapping to Earth UTC dates.
    ///
    /// Ls is computed with the Allison &amp; McEwen (2000) algorithm ("A post-Pathfinder
    /// evaluation of areocentric solar coordinates", Planet. Space Sci. 48, 215-235),
    /// accurate to ~0.01 deg over the mission timescales simulated here.
    /// </summary>
    public sealed class SimClock
    {
        /// <summary>Timestep, seconds. Default 1/24 sol ("Mars hour").</summary>
        public double DtSeconds { get; private set; }
        public double DtHours => DtSeconds / 3600.0;
        public double DtSols => DtSeconds / Units.SolSeconds;

        /// <summary>Mission elapsed time in sols (sol 0 = scenario epoch, e.g. first landing).</summary>
        public double Sol { get; private set; }

        /// <summary>Integer sol number.</summary>
        public int SolNumber => (int)Math.Floor(Sol);

        /// <summary>Fraction of the current sol [0,1): 0 = local midnight at the reference site.</summary>
        public double TimeOfSol => Sol - Math.Floor(Sol);

        /// <summary>Local true solar time in Mars-hours [0, 24.66).</summary>
        public double LocalSolarHours => TimeOfSol * Units.SolHours;

        /// <summary>Areocentric solar longitude, degrees [0,360). 0=N spring equinox, 90=N summer solstice, 251=perihelion.</summary>
        public double Ls { get; private set; }

        /// <summary>Mars year counter relative to mission start (increments when Ls wraps).</summary>
        public int MarsYear { get; private set; }

        /// <summary>Days since J2000 epoch (TT ~ UTC for our purposes) of the *mission epoch* (sol 0, 00:00 local).</summary>
        private readonly double _epochJ2000;

        public DateTime EpochUtc { get; }

        public long StepCount { get; private set; }

        private double _prevLs;

        public SimClock(DateTime epochUtc, double dtSeconds)
        {
            EpochUtc = epochUtc;
            DtSeconds = dtSeconds;
            _epochJ2000 = (epochUtc - new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc)).TotalDays;
            Sol = 0;
            Ls = ComputeLs(_epochJ2000);
            _prevLs = Ls;
        }

        public void SetTimestep(double dtSeconds) => DtSeconds = dtSeconds;

        public void Advance()
        {
            Sol += DtSols;
            StepCount++;
            double j2000 = _epochJ2000 + Sol * (Units.SolSeconds / Units.EarthDaySeconds);
            Ls = ComputeLs(j2000);
            if (Ls < _prevLs - 180.0) MarsYear++;   // wrapped 360 -> 0
            _prevLs = Ls;
        }

        /// <summary>Earth UTC datetime corresponding to the current sim time.</summary>
        public DateTime EarthUtc => EpochUtc.AddSeconds(Sol * Units.SolSeconds);

        /// <summary>
        /// Allison &amp; McEwen (2000): areocentric solar longitude from days since J2000.
        /// </summary>
        public static double ComputeLs(double daysSinceJ2000)
        {
            double m = Wrap360(19.3871 + 0.52402073 * daysSinceJ2000);          // mean anomaly, deg
            double alphaFms = Wrap360(270.3871 + 0.524038496 * daysSinceJ2000); // fictitious mean sun
            double mRad = m * Units.Deg2Rad;
            double eoc =
                (10.691 + 3.0e-7 * daysSinceJ2000) * Math.Sin(mRad)
                + 0.623 * Math.Sin(2 * mRad)
                + 0.050 * Math.Sin(3 * mRad)
                + 0.005 * Math.Sin(4 * mRad)
                + 0.0005 * Math.Sin(5 * mRad);                                   // equation of center, deg
            return Wrap360(alphaFms + eoc);
        }

        /// <summary>
        /// Solar declination (deg) for a given Ls. delta = asin(sin(25.19 deg) * sin(Ls)).
        /// </summary>
        public static double SolarDeclinationDeg(double lsDeg)
            => Math.Asin(Math.Sin(25.19 * Units.Deg2Rad) * Math.Sin(lsDeg * Units.Deg2Rad)) * Units.Rad2Deg;

        /// <summary>
        /// Sun elevation above the horizon (deg) at a site latitude for the current time of sol.
        /// Hour angle h = 15 deg per Mars-hour from local solar noon (scaled to the 24.66 h sol).
        /// </summary>
        public static double SunElevationDeg(double lsDeg, double latitudeDeg, double timeOfSol)
        {
            double delta = SolarDeclinationDeg(lsDeg) * Units.Deg2Rad;
            double lat = latitudeDeg * Units.Deg2Rad;
            double hourAngle = (timeOfSol - 0.5) * 2.0 * Math.PI; // radians from solar noon
            double sinEl = Math.Sin(lat) * Math.Sin(delta) + Math.Cos(lat) * Math.Cos(delta) * Math.Cos(hourAngle);
            return Math.Asin(Math.Clamp(sinEl, -1.0, 1.0)) * Units.Rad2Deg;
        }

        /// <summary>Sun azimuth (deg, 0=N, 90=E) for visualization.</summary>
        public static double SunAzimuthDeg(double lsDeg, double latitudeDeg, double timeOfSol)
        {
            double delta = SolarDeclinationDeg(lsDeg) * Units.Deg2Rad;
            double lat = latitudeDeg * Units.Deg2Rad;
            double h = (timeOfSol - 0.5) * 2.0 * Math.PI;
            double el = Math.Asin(Math.Clamp(Math.Sin(lat) * Math.Sin(delta) + Math.Cos(lat) * Math.Cos(delta) * Math.Cos(h), -1, 1));
            double cosAz = (Math.Sin(delta) - Math.Sin(lat) * Math.Sin(el)) / (Math.Cos(lat) * Math.Cos(el) + 1e-9);
            double az = Math.Acos(Math.Clamp(cosAz, -1.0, 1.0)) * Units.Rad2Deg;
            return h > 0 ? 360.0 - az : az;
        }

        /// <summary>
        /// Mars-Sun distance in AU for a given Ls (r = a(1-e^2)/(1+e cos(theta)), theta = Ls - 251 deg).
        /// Drives the seasonal +/-19% swing in top-of-atmosphere flux.
        /// </summary>
        public static double MarsSunDistanceAu(double lsDeg)
        {
            const double a = 1.523679, e = 0.0934;
            double trueAnomaly = (lsDeg - 251.0) * Units.Deg2Rad; // perihelion at Ls=251
            return a * (1 - e * e) / (1 + e * Math.Cos(trueAnomaly));
        }

        private static double Wrap360(double x)
        {
            x %= 360.0;
            return x < 0 ? x + 360.0 : x;
        }
    }
}
