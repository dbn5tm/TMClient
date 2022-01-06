using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace N5TMScrapper
{
    
    #region license
    /*
    (c) 2011-2015, Vladimir Agafonkin, (c) 2016 VPKSoft
    Based on a JavaScript library SunCalc for calculating sun/moon position and light phases.
    https://github.com/mourner/suncalc
    Translated to c# by VPKSoft, http://www.vpksoft.net
    */
    #endregion


    public class SunMoonCalcs
    {
        private const double dayMs = 86400000;
        private const double J1970 = 2440588;
        private const double J2000 = 2451545;
        private const double PI = Math.PI;
        private const double rad = Math.PI / 180.0;
        private const double e = rad * 23.4397; // obliquity of the Earth

        public class SunTime
        {
            public double Angle { get; set; }
            public string MorningName { get; set; }
            public string EveningName { get; set; }
        }

        public class SunTimeRiseSet : SunTime
        {
            public DateTime RiseTime { get; set; }
            public DateTime SetTime { get; set; }
        }

        // sun times configuration (angle, morning name, evening name)
        public static List<SunTime> SunTimes = new List<SunTime>(new SunTime[]
        {
        new SunTime {Angle = -0.833,    MorningName = "sunrise",        EveningName = "sunset" },
        new SunTime {Angle = -0.3,      MorningName = "sunriseEnd",     EveningName = "sunsetStart" },
        new SunTime {Angle = -6,        MorningName = "dawn",           EveningName = "dusk" },
        new SunTime {Angle = -12,       MorningName = "nauticalDawn",   EveningName = "nauticalDusk" },
        new SunTime {Angle = -18,       MorningName = "nightEnd",       EveningName = "night" },
        new SunTime {Angle = 6,         MorningName = "goldenHourEnd",  EveningName = "goldenHour" }
        });

        // adds a custom time to the times config
        public static void AddTime(SunTime sunTime)
        {
            SunTimes.Add(sunTime);
        }

        public class RaDec
        {
            public double ra = 0;
            public double dec = 0;
        }

        public static double ToJulianDate(DateTime dt)
        {
            dt = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;
            return (dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds / dayMs - 0.5 + J1970;
        }

        public static DateTime FromJulianDate(double jd)
        {
            return double.IsNaN(jd) ? DateTime.MinValue : new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds((jd + 0.5 - J1970) * dayMs);
        }

        public static double JulianDays(DateTime dt)
        {
            dt = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;
            return ToJulianDate(dt) - J2000;
        }

        public static double RightAscension(double l, double b)
        {
            return Math.Atan2(Math.Sin(l) * Math.Cos(e) - Math.Tan(b) * Math.Sin(e), Math.Cos(l));
        }

        public static double Declination(double l, double b)
        {
            return Math.Asin(Math.Sin(b) * Math.Cos(e) + Math.Cos(b) * Math.Sin(e) * Math.Sin(l));
        }

        public static double Azimuth(double H, double phi, double dec)
        {
            return Math.Atan2(Math.Sin(H), Math.Cos(H) * Math.Sin(phi) - Math.Tan(dec) * Math.Cos(phi));
        }

        public static double Altitude(double H, double phi, double dec)
        {
            return Math.Asin(Math.Sin(phi) * Math.Sin(dec) + Math.Cos(phi) * Math.Cos(dec) * Math.Cos(H));
        }

        public static double SiderealTime(double d, double lw)
        {
            return rad * (280.16 + 360.9856235 * d) - lw;
        }

        public static double AstroRefraction(double h)
        {
            if (h < 0) // the following formula works for positive altitudes only.
            {
                h = 0; // if h = -0.08901179 a div/0 would occur.
            }

            // formula 16.4 of "Astronomical Algorithms" 2nd edition by Jean Meeus (Willmann-Bell, Richmond) 1998.
            // 1.02 / tan(h + 10.26 / (h + 5.10)) h in degrees, result in arc minutes -> converted to rad:
            return 0.0002967 / Math.Tan(h + 0.00312536 / (h + 0.08901179));
        }

        // general sun calculations
        public static double SolarMeanAnomaly(double d)
        {
            return rad * (357.5291 + 0.98560028 * d);
        }

        public static DateTime HoursLater(DateTime dt, double h)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(dt.ValueOf() + h * dayMs / 24);
        }

        public static double EclipticLongitude(double M)
        {
            double C = rad * (1.9148 * Math.Sin(M) + 0.02 * Math.Sin(2 * M) + 0.0003 * Math.Sin(3 * M)); // equation of center
            double P = rad * 102.9372; // perihelion of the Earth
            return M + C + P + PI;
        }

        public static RaDec SunCoords(double d)
        {
            double M = SolarMeanAnomaly(d);
            double L = EclipticLongitude(M);
            return new RaDec { dec = Declination(L, 0), ra = RightAscension(L, 0) };
        }

        public class MoonRaDecDist
        {
            public double ra = 0;
            public double dec = 0;
            public double dist = 0;
        }

        public static MoonRaDecDist MoonCoords(double d) // geocentric ecliptic coordinates of the moon
        {
            double L = rad * (218.316 + 13.176396 * d); // ecliptic longitude
            double M = rad * (134.963 + 13.064993 * d); // mean anomaly
            double F = rad * (93.272 + 13.229350 * d);  // mean distance

            double l = L + rad * 6.289 * Math.Sin(M); // longitude
            double b = rad * 5.128 * Math.Sin(F);     // latitude
            double dt = 385001 - 20905 * Math.Cos(M);  // distance to the moon in km

            return new MoonRaDecDist { ra = RightAscension(l, b), dec = Declination(l, b), dist = dt };
        }

        public class SunCalc
        {
            private const double J0 = 0.0009;

            public class AzAlt
            {
                public double azimuth = 0;
                public double altitude = 0;
            }

            public static AzAlt GetPosition(DateTime dt, double lat, double lng)
            {
                double lw = rad * -lng;
                double phi = rad * lat;
                double d = JulianDays(dt);
                RaDec c = SunCoords(d);
                double H = SiderealTime(d, lw) - c.ra;
                return new AzAlt { azimuth = Azimuth(H, phi, c.dec), altitude = Altitude(H, phi, c.dec) };
            }

            public static double JulianCycle(double d, double lw)
            {
                return Math.Round(d - J0 - lw / (2 * PI));
            }

            public static double ApproxTransit(double Ht, double lw, double n)
            {
                return J0 + (Ht + lw) / (2 * PI) + n;
            }

            public static double SolarTransitJ(double ds, double M, double L)
            {
                return J2000 + ds + 0.0053 * Math.Sin(M) - 0.0069 * Math.Sin(2.0 * L);
            }

            public static double HourAngle(double h, double phi, double d)
            {
                return Math.Acos((Math.Sin(h) - Math.Sin(phi) * Math.Sin(d)) / (Math.Cos(phi) * Math.Cos(d)));
            }

            // returns set time for the given sun altitude
            public static double GetSetJ(double h, double lw, double phi, double dec, double n, double M, double L)
            {
                double w = HourAngle(h, phi, dec);
                double a = ApproxTransit(w, lw, n);
                return SolarTransitJ(a, M, L);
            }
            // solar disc diameter 
            public static void GetTimes(DateTime dt, double lat, double lng, out DateTime rise, out DateTime set, double angle = -0.833)
            {
                double lw = rad * -lng;
                double phi = rad * lat;
                double d = JulianDays(dt);
                double n = JulianCycle(d, lw);
                double ds = ApproxTransit(0, lw, n);

                double M = SolarMeanAnomaly(ds);
                double L = EclipticLongitude(M);
                double dec = Declination(L, 0);

                double Jnoon = SolarTransitJ(ds, M, L);
                double Jset = GetSetJ(angle * rad, lw, phi, dec, n, M, L);
                double Jrise = Jnoon - (Jset - Jnoon);

                rise = double.IsNaN(Jrise) ? DateTime.MinValue : FromJulianDate(Jrise);
                set = double.IsNaN(Jset) ? DateTime.MinValue : FromJulianDate(Jset);
            }

            public static List<SunTimeRiseSet> GetTimes(DateTime dt, double lat, double lng)
            {
                List<SunTimeRiseSet> retval = new List<SunTimeRiseSet>();
                DateTime rise, set;
                foreach (SunTime st in SunTimes)
                {
                    GetTimes(dt, lat, lng, out rise, out set, st.Angle);
                    retval.Add(new SunTimeRiseSet { Angle = st.Angle, MorningName = st.MorningName, EveningName = st.EveningName, RiseTime = rise, SetTime = set });
                }
                return retval;
            }
        }

        public class MoonCalc
        {
            public class MoonAzAltDistPa
            {
                public double azimuth = 0;
                public double altitude = 0;
                public double distance = 0;
                public double parallacticAngle = 0;
            }

            public class MoonFracPhaseAngle
            {
                public double fraction = 0;
                public double phase = 0;
                public double angle = 0;
            }

            public static MoonAzAltDistPa GetMoonPosition(DateTime dt, double lat, double lng)
            {
                double lw = rad * -lng;
                double phi = rad * lat;
                double d = JulianDays(dt);

                MoonRaDecDist c = MoonCoords(d);
                double H = SiderealTime(d, lw) - c.ra;
                double h = Altitude(H, phi, c.dec);
                // formula 14.1 of "Astronomical Algorithms" 2nd edition by Jean Meeus (Willmann-Bell, Richmond) 1998.
                double pa = Math.Atan2(Math.Sin(H), Math.Tan(phi) * Math.Cos(c.dec) - Math.Sin(c.dec) * Math.Cos(H));

                h += AstroRefraction(h); // altitude correction for refraction
                return new MoonAzAltDistPa { azimuth = Azimuth(H, phi, c.dec), altitude = h, distance = c.dist, parallacticAngle = pa };
            }

            public static MoonFracPhaseAngle GetMoonIllumination(DateTime dt)
            {
                double d = JulianDays(dt);
                RaDec s = SunCoords(d);
                MoonRaDecDist m = MoonCoords(d);
                double sdist = 149598000; // distance from Earth to Sun in km
                double phi = Math.Acos(Math.Sin(s.dec) * Math.Sin(m.dec) + Math.Cos(s.dec) * Math.Cos(m.dec) * Math.Cos(s.ra - m.ra));
                double inc = Math.Atan2(sdist * Math.Sin(phi), m.dist - sdist * Math.Cos(phi));
                double angle = Math.Atan2(Math.Cos(s.dec) * Math.Sin(s.ra - m.ra), Math.Sin(s.dec) * Math.Cos(m.dec) -
                                Math.Cos(s.dec) * Math.Sin(m.dec) * Math.Cos(s.ra - m.ra));
                return new MoonFracPhaseAngle { fraction = (1 + Math.Cos(inc)) / 2, phase = 0.5 + 0.5 * inc * (angle < 0 ? -1 : 1) / PI, angle = angle };
            }

            // DateTime.Max = always up, DateTime.Min = always down
            public static void GetMoonTimes(DateTime dt, double lat, double lng, out DateTime risem, out DateTime setm, out bool? alwaysUp, out bool? alwaysDown)
            {
                dt = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);

                DateTime t = dt;

                double hc = 0.133 * rad;
                double h0 = GetMoonPosition(t, lat, lng).altitude - hc;
                double h1, h2, rise = 0, set = 0, a, b, xe, ye = 0, d, x1, x2, dx;
                int roots;

                for (double i = 1.0; i <= 24.0; i += 2.0)
                {
                    h1 = GetMoonPosition(HoursLater(t, i), lat, lng).altitude - hc;
                    h2 = GetMoonPosition(HoursLater(t, i + 1), lat, lng).altitude - hc;

                    a = (h0 + h2) / 2 - h1;
                    b = (h2 - h0) / 2;
                    xe = -b / (2 * a);
                    ye = (a * xe + b) * xe + h1;
                    d = b * b - 4 * a * h1;
                    roots = 0;

                    if (d >= 0)
                    {
                        dx = Math.Sqrt(d) / (Math.Abs(a) * 2);
                        x1 = xe - dx;
                        x2 = xe + dx;
                        if (Math.Abs(x1) <= 1)
                        {
                            roots++;
                        }

                        if (Math.Abs(x2) <= 1)
                        {
                            roots++;
                        }

                        if (x1 < -1)
                        {
                            x1 = x2;
                        }

                        if (roots == 1)
                        {
                            if (h0 < 0)
                            {
                                rise = i + x1;
                            }
                            else
                            {
                                set = i + x1;
                            }
                        }
                        else if (roots == 2)
                        {
                            rise = i + (ye < 0 ? x2 : x1);
                            set = i + (ye < 0 ? x1 : x2);
                        }

                        if (rise > 0 && set > 0)
                        {
                            break;
                        }

                        h0 = h2;
                    }
                }

                risem = DateTime.MinValue;
                setm = DateTime.MinValue;

                if (rise > 0)
                {
                    risem = HoursLater(t, rise);
                }

                if (set > 0)
                {
                    setm = HoursLater(t, set);
                }

                alwaysUp = null;
                alwaysDown = null;

                if (rise < 0 && set < 0)
                {
                    if (ye > 0)
                    {
                        alwaysUp = true;
                        alwaysDown = false;
                        risem = DateTime.MaxValue;
                        setm = DateTime.MaxValue;
                    }
                    else
                    {
                        alwaysDown = true;
                        alwaysUp = false;
                    }
                }
            }
        }
        public static void MoonPos(int iYear, int iMonth, int iDay, double dHour, double dLong, double dLat,
            ref double dAzimuth, ref double dElevation, ref double dDist, ref double dMoonLong,
            ref double dDecl, ref double dRA, ref double LST)
        {
            double dDec, dLonecl, dLatecl;
            double dNN, dI, dW, dA, dE, dMM, dV, dEE, dEcl, dD, dR, dXV, dYV;
            double dXg, dYg, dZg, dMs, dWs, dLs, dLm, dDD, dFF, dXe, dYe, dZe;
            double dMpar, dGclat, dRho, dGMST0, dLST, dHA, dG, dTopRA, dTopdec;
            double dRad = 57.2957795131, dTwoPI = 6.283185307, dPI, dPIO2;
            // Date / Time
            // Not valid after 2100-02  dD = 367 * iYear - 7 * (iYear + (iMonth + 9) / 12) / 4 + 275 * iMonth / 9 + iDay - 730530 + dHour / 24.0;
            dD = 367 * iYear - 7 * (iYear + (iMonth + 9) / 12) / 4 - 3 * ((iYear + (iMonth - 9) / 7) / 100 + 1) / 4 + 275 * iMonth / 9 + iDay - 730515;
            dD = dD + dHour / 24.0;
            //obliquity of the ecliptic = Jordens lutning
            dEcl = 23.4393 - 3.563e-7 * dD;
            // Orbital elements of the Moon:
            dNN = 125.1228 - 0.0529538083 * dD;
            dI = 5.1454;
            dW = (318.0634 + 0.1643573223 * dD + 360000.0) % 360.0;
            dA = 60.2666;
            dE = 0.0549;
            dMM = (115.3654 + 13.0649929509 * dD + 360000.0) % 360.0;
            //First, compute the eccentric anomaly E from the mean anomaly M and from the eccentricity e (E and M in degrees):
            // or (if E and M are expressed in radians):
            dEE = dMM + dE * dRad * Math.Sin(dMM / dRad) * (1.0 + dE * Math.Cos(dMM / dRad));
            dEE = dEE - (dEE - dE * dRad * Math.Sin(dEE / dRad) - dMM) / (1.0 - dE * Math.Cos(dEE / dRad));
            //Distance
            dXV = dA * (Math.Cos(dEE / dRad) - dE);
            dYV = dA * (Math.Sqrt(1.0 - dE * dE) * Math.Sin(dEE / dRad));
            dV = (dRad * Math.Atan2(dYV, dXV) + 720.0) % 360.0;
            dR = Math.Sqrt(dXV * dXV + dYV * dYV);
            //Compute the planet's position in 3-dimensional space:
            dXg = dR * (Math.Cos(dNN / dRad) * Math.Cos((dV + dW) / dRad) - Math.Sin(dNN / dRad) * Math.Sin((dV + dW) / dRad) * Math.Cos(dI / dRad));
            dYg = dR * (Math.Sin(dNN / dRad) * Math.Cos((dV + dW) / dRad) + Math.Cos(dNN / dRad) * Math.Sin((dV + dW) / dRad) * Math.Cos(dI / dRad));
            dZg = dR * (Math.Sin((dV + dW) / dRad) * Math.Sin(dI / dRad));
            // For the Moon, this is the geocentric (Earth-centered) position in the ecliptic coordinate system.
            dLonecl = (dRad * Math.Atan2(dYg / dRad, dXg / dRad) + 720.0) % 360.0;
            dLatecl = dRad * Math.Atan2(dZg / dRad, Math.Sqrt(dXg * dXg + dYg * dYg) / dRad);
            dMs = (356.0470 + 0.9856002585 * dD + 3600000.0) % 360.0;
            dWs = 282.9404 + 4.70935e-5 * dD;
            dLs = (dMs + dWs + 720.0) % 360.0;
            dLm = (dMM + dW + dNN + 720.0) % 360.0;
            dDD = (dLm - dLs + 360.0) % 360.0;
            dFF = (dLm - dNN + 360.0) % 360.0;
            // If the position of the Moon is computed, and one wishes a better accuracy than about 2 degrees
            dLonecl = dLonecl
                    - 1.274 * Math.Sin((dMM - 2.0 * dDD) / dRad)
                    + 0.658 * Math.Sin(2.0 * dDD / dRad)
                    - 0.186 * Math.Sin(dMs / dRad)
                    - 0.059 * Math.Sin((2.0 * dMM - 2.0 * dDD) / dRad)
                    - 0.057 * Math.Sin((dMM - 2.0 * dDD + dMs) / dRad)
                    + 0.053 * Math.Sin((dMM + 2.0 * dDD) / dRad)
                    + 0.046 * Math.Sin((2.0 * dDD - dMs) / dRad)
                    + 0.041 * Math.Sin((dMM - dMs) / dRad)
                    - 0.035 * Math.Sin(dDD / dRad)
                    - 0.031 * Math.Sin((dMM + dMs) / dRad)
                    - 0.015 * Math.Sin((2.0 * dFF - 2.0 * dDD) / dRad)
                    + 0.011 * Math.Sin((dMM - 4.0 * dDD) / dRad);
            dLatecl = dLatecl
                    - 0.173 * Math.Sin((dFF - 2.0 * dDD) / dRad)
                    - 0.055 * Math.Sin((dMM - dFF - 2.0 * dDD) / dRad)
                    - 0.046 * Math.Sin((dMM + dFF - 2.0 * dDD) / dRad)
                    + 0.033 * Math.Sin((dFF + 2.0 * dDD) / dRad)
                    + 0.017 * Math.Sin((2.0 * dMM + dFF) / dRad);
            dR = 60.36298
               - 3.27746 * Math.Cos(dMM / dRad)
               - 0.57994 * Math.Cos((dMM - 2.0 * dDD) / dRad)
               - 0.46357 * Math.Cos(2.0 * dDD / dRad)
               - 0.08904 * Math.Cos(2.0 * dMM / dRad)
               + 0.03865 * Math.Cos((2.0 * dMM - 2.0 * dDD) / dRad)
               - 0.03237 * Math.Cos((2.0 * dDD - dMs) / dRad)
               - 0.02688 * Math.Cos((dMM + 2.0 * dDD) / dRad)
               - 0.02358 * Math.Cos((dMM - 2.0 * dDD + dMs) / dRad)
               - 0.02030 * Math.Cos((dMM - dMs) / dRad)
               + 0.01719 * Math.Cos(dDD / dRad)
               + 0.01671 * Math.Cos((dMM + dMs) / dRad);
            dDist = dR * 6378.14;
            dXg = dR * Math.Cos(dLonecl / dRad) * Math.Cos(dLatecl / dRad);
            dYg = dR * Math.Sin(dLonecl / dRad) * Math.Cos(dLatecl / dRad);
            dZg = dR * Math.Sin(dLatecl / dRad);
            dXe = dXg;
            dYe = dYg * Math.Cos(dEcl / dRad) - dZg * Math.Sin(dEcl / dRad);
            dZe = dYg * Math.Sin(dEcl / dRad) + dZg * Math.Cos(dEcl / dRad);
            // Moon Right Ascension
            dRA = (dRad * Math.Atan2(dYe, dXe) + 360.0) % 360.0;
            // Moon Declination
            dDec = dRad * Math.Atan2(dZe, Math.Sqrt(dXe * dXe + dYe * dYe));
            dDecl = dDec;
            dMpar = dRad * Math.Asin(1.0 / dR);
            dGclat = dLat - 0.1924 * Math.Sin(2.0 * dLat / dRad);
            dRho = 0.99883 + 0.00167 * Math.Cos(2.0 * dLat / dRad);
            dGMST0 = (dLs + 180.0) / 15.0;
            dLST = (dGMST0 + dHour + dLong / 15.0 + 48.0) % 24.0;
            dHA = 15.0 * dLST - dRA;
            dG = dRad * Math.Atan(Math.Tan(dGclat / dRad) / Math.Cos(dHA / dRad));
            dTopRA = dRA - dMpar * dRho * Math.Cos(dGclat / dRad) * Math.Sin(dHA / dRad) / Math.Cos(dDec / dRad);
            dTopdec = dDec - dMpar * dRho * Math.Sin(dGclat / dRad) * Math.Sin((dG - dDec) / dRad) / Math.Sin(dG / dRad);
            dHA = 15.0 * dLST - dTopRA;
            if (dHA > 180.0) dHA = dHA - 360.0;
            if (dHA < -180.0) dHA = dHA + 360.0;
            dPI = 0.5 * dTwoPI;
            dPIO2 = 0.5 * dPI;
            DCoord(dPI, dPIO2 - dLat / dRad, 0.0, dLat / dRad, dHA * dTwoPI / 360.0, dTopdec / dRad, ref dAzimuth, ref dElevation);
            LST = dLST;
            dAzimuth = dAzimuth * dRad;
            dElevation = dElevation * dRad;
            dMoonLong = -dHA;
        }
        // Corrections
        public static void DCoord(double dA0, double dB0, double dAP, double dBP,
            double dA1, double dB1, ref double dA2, ref double dB2)
        {
            double dSB0, dCB0, dSBP, dCBP, dSB1, dCB1, dSB2, dCB2, dSAA, dCAA, dCBB, dSBB, dSA2, dCA2, dTA2O2 = 0;
            dSB0 = Math.Sin(dB0);
            dCB0 = Math.Cos(dB0);
            dSBP = Math.Sin(dBP);
            dCBP = Math.Cos(dBP);
            dSB1 = Math.Sin(dB1);
            dCB1 = Math.Cos(dB1);
            dSB2 = dSBP * dSB1 + dCBP * dCB1 * Math.Cos(dAP - dA1);
            dCB2 = Math.Sqrt(1.0 - dSB2 * dSB2);
            dB2 = Math.Atan(dSB2 / dCB2);
            dSAA = Math.Sin(dAP - dA1) * dCB1 / dCB2;
            dCAA = (dSB1 - dSB2 * dSBP) / (dCB2 * dCBP);
            dCBB = dSB0 / dCBP;
            dSBB = Math.Sin(dAP - dA0) * dCB0;
            dSA2 = dSAA * dCBB - dCAA * dSBB;
            dCA2 = dCAA * dCBB + dSAA * dSBB;
            if (dCA2 <= 0.0) dTA2O2 = (1.0 - dCA2) / dSA2;
            if (dCA2 > 0.0) dTA2O2 = dSA2 / (1.0 + dCA2);
            dA2 = 2.0 * Math.Atan(dTA2O2);
            if (dA2 < 0.0) dA2 = dA2 + 6.2831853071795864;
        }
    }
    
    public static class DateTimeJavaScriptExt
    {
        public static double ValueOf(this DateTime dt) // JavaScript Date.valueOf()
        {
            dt = dt.Kind == DateTimeKind.Local ? dt.ToUniversalTime() : dt;
            return (dt - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        public static DateTime FromJScriptValue(this DateTime dt, double ms)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
        }
    }
    
}
