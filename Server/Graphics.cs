using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Panopticon
{
    public class Graphics
    {
        public class Bitmap
        {
            internal int stride;
            internal byte[] pixels;
            internal int h;
            internal int w;
            public Bitmap(int HPix, int WPix, byte RbColor, byte GbColor, byte BbColor)
            {
                stride = 3 * WPix + (WPix % 4);
                pixels = new byte[HPix * stride];
                h = HPix;
                w = WPix;
                int x, y = 0;
                for (; y < h; y++)
                {
                    for (x = 0; x < w; x++)
                    {
                        SetPixel(x, y, RbColor, GbColor, BbColor, 1);
                    }
                }
            }
            public void SetPixel(int x, int y, byte R, byte G, byte B, float alpha)
            {

                int id = y * stride + x * 3;
                if (id >= pixels.Length || id < 0 || x < 0)
                    return;
                pixels[id] = (byte)((pixels[id] - R) * (1 - alpha) + R);
                pixels[id + 1] = (byte)((pixels[id + 1] - G) * (1 - alpha) + G);
                pixels[id + 2] = (byte)((pixels[id + 2] - B) * (1 - alpha) + B);

            }
            
            public BitmapSource GetBitmap()
            {
                BitmapSource bs = BitmapSource.Create(w, h, 300, 300, PixelFormats.Rgb24, null, pixels, stride);
                return bs;
            }
        }
        internal static BitmapSource createheatmap(int[,] heatmap)
        {
            int xlen = (int)(20 * (heatmap.GetLength(0)-1));
            int ylen = (int)(20 * (heatmap.GetLength(1)-1));
            Bitmap bitmap = new Bitmap(ylen, xlen, 0, 0, 0);
            double max = 0;
            double min = double.MaxValue;
            for (int x = 0; x < heatmap.GetLength(0); x++)
            {
                for (int y = 0; y < heatmap.GetLength(1); y++)
                {
                    if (heatmap[x, y] > max)
                        max = heatmap[x, y];
                    if (heatmap[x, y] < min)
                        min = heatmap[x, y];
                }
            }
            for (int x = 0; x < xlen; x++)
            {
                for (int y = 0; y < ylen; y++)
                {
                    int ix = (int)(x / 20);
                    int iy = (int)(y / 20);
                    double fx = (double)x / 20.0;
                    double fy = (double)y / 20.0;
                    double val = (1 - (fx - ix) + 1 - (fy - iy)) * heatmap[ix, iy] +
                        ((fx - ix) + 1 - (fy - iy)) * heatmap[ix + 1, iy] +
                        (1 - (fx - ix) + (fy - iy)) * heatmap[ix, iy + 1] +
                        ((fx - ix) + (fy - iy)) * heatmap[ix + 1, iy + 1];
                    val /= 4;
                    val = Math.Pow((val - min) / (max - min), 0.5);
                    byte G = (byte)(val < 0.25 ? 255 : 255 * 4 * (1 - val) / 3);
                    byte B = (byte)(val < 0.25 ? 255 * 4 * (0.25 - val) : 0);
                    bitmap.SetPixel(x, y, (byte)255, G, B, 1);
                }
            }
            return bitmap.GetBitmap();
        }

        internal static void drawHistogram(Canvas cv, IEnumerable<Double> data, Func<double,int,object,String> tooltipper, object tooltipdata, MouseButtonEventHandler onmousedown, Brush[] strokecolors, Brush[] fillcolors, int initialbrush, Func<double,int,object,object> tagger, object tagdata)
        {
            cv.Children.Clear();
            double max = 0.001;
            int count = 0;
            foreach (double d in data)
            {
                count++;
                if (d > max)
                    max = d;
            }
            int i = 0;
            double width = Math.Min(20,cv.Width / count);
            double margin = (cv.Width - count*width)/2;
            foreach (double d in data)
            {
                Rectangle re = new Rectangle();
                re.Height = d / max * cv.Height;
                re.Width = width;
                re.Fill = fillcolors[(initialbrush + i) % fillcolors.Length];
                re.Stroke = strokecolors[(i+ initialbrush) % strokecolors.Length];
                re.VerticalAlignment = VerticalAlignment.Bottom;
                Canvas.SetLeft(re, margin);
                Canvas.SetBottom(re, 0);
                cv.Children.Add(re);
                re.ToolTip = tooltipper(d, i, tooltipdata);
                if (onmousedown != null)
                {
                    re.MouseDown += onmousedown;
                    re.Cursor = Cursors.Hand;
                }
                if (tagger != null)
                    re.Tag = tagger(d, i, tagdata);
                margin += re.Width;
                i++;
            }
        }
    }
}
