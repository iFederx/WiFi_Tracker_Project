﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Panopticon
{
    class Utilities
    {
        public static class FancyColorCreator
        {
            const double goldenratioconiugate = 360 * 0.618033988749895;
            
            private static void HsvToRgb(double h, double S, double V, out int r, out int g, out int b)
            {
                double H = h;
                while (H < 0) { H += 360; };
                while (H >= 360) { H -= 360; };
                double R, G, B;
                if (V <= 0)
                { R = G = B = 0; }
                else if (S <= 0)
                {
                    R = G = B = V;
                }
                else
                {
                    double hf = H / 60.0;
                    int i = (int)Math.Floor(hf);
                    double f = hf - i;
                    double pv = V * (1 - S);
                    double qv = V * (1 - S * f);
                    double tv = V * (1 - S * (1 - f));
                    switch (i)
                    {

                        // Red is the dominant color

                        case 0:
                            R = V;
                            G = tv;
                            B = pv;
                            break;

                        // Green is the dominant color

                        case 1:
                            R = qv;
                            G = V;
                            B = pv;
                            break;
                        case 2:
                            R = pv;
                            G = V;
                            B = tv;
                            break;

                        // Blue is the dominant color

                        case 3:
                            R = pv;
                            G = qv;
                            B = V;
                            break;
                        case 4:
                            R = tv;
                            G = pv;
                            B = V;
                            break;

                        // Red is the dominant color

                        case 5:
                            R = V;
                            G = pv;
                            B = qv;
                            break;

                        // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                        case 6:
                            R = V;
                            G = tv;
                            B = pv;
                            break;
                        case -1:
                            R = V;
                            G = pv;
                            B = qv;
                            break;

                        // The color is not defined, we should throw an error.

                        default:
                            //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                            R = G = B = V; // Just pretend its black/white
                            break;
                    }
                }
                r = clip((int)(R * 255.0));
                g = clip((int)(G * 255.0));
                b = clip((int)(B * 255.0));
            }

            /// <summary>
            /// Clip a value to 0-255
            /// </summary>
            private static int clip(int i)
            {
                if (i < 0) return 0;
                if (i > 255) return 255;
                return i;
            }
            public static Brush randomBrush(int seed)
            {
                int r;
                int g;
                int b;
                double randomhue = (seed % 3600) / 10;
                randomhue += goldenratioconiugate;
                HsvToRgb(randomhue, 0.6, 0.85, out r, out g, out b);
                Color newCol = Color.FromRgb((byte)r, (byte)g, (byte)b);
                System.Diagnostics.Debug.Print(r + " " + g + " " + b + " " + randomhue);
                Brush brush = new SolidColorBrush(newCol);
                return brush;
            }
        }
    }
}
