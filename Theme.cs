using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SlideShot
{
    internal static class Theme
    {
        public static bool IsDark;
        public static Color Window;
        public static Color Surface;
        public static Color SurfaceHover;
        public static Color Text;
        public static Color TextMuted;
        public static Color Border;
        public static Color Divider;
        public static Color Accent;
        public static Color AccentText;
        public static Color AccentHover;
        public static Color Hover;
        public static Color Pressed;
        public static Color ImageBack;
        public static Color Danger;

        static Theme()
        {
            Reload();
        }

        public static void Reload()
        {
            bool dark = false;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object v = key.GetValue("AppsUseLightTheme");
                        if (v is int && (int)v == 0) dark = true;
                    }
                }
            }
            catch
            {
            }

            IsDark = dark;

            if (dark)
            {
                Window = Color.FromArgb(32, 32, 32);
                Surface = Color.FromArgb(44, 44, 44);
                SurfaceHover = Color.FromArgb(54, 54, 54);
                Text = Color.FromArgb(255, 255, 255);
                TextMuted = Color.FromArgb(158, 158, 158);
                Border = Color.FromArgb(62, 62, 62);
                Divider = Color.FromArgb(48, 48, 48);
                Accent = Color.FromArgb(76, 194, 255);
                AccentText = Color.FromArgb(12, 20, 26);
                AccentHover = Color.FromArgb(120, 208, 255);
                Hover = Color.FromArgb(20, 255, 255, 255);
                Pressed = Color.FromArgb(32, 255, 255, 255);
                ImageBack = Color.FromArgb(24, 24, 24);
                Danger = Color.FromArgb(255, 110, 110);
            }
            else
            {
                Window = Color.FromArgb(243, 243, 243);
                Surface = Color.FromArgb(255, 255, 255);
                SurfaceHover = Color.FromArgb(250, 250, 250);
                Text = Color.FromArgb(26, 26, 26);
                TextMuted = Color.FromArgb(96, 96, 96);
                Border = Color.FromArgb(226, 226, 226);
                Divider = Color.FromArgb(234, 234, 234);
                Accent = Color.FromArgb(0, 103, 192);
                AccentText = Color.White;
                AccentHover = Color.FromArgb(16, 110, 190);
                Hover = Color.FromArgb(10, 0, 0, 0);
                Pressed = Color.FromArgb(22, 0, 0, 0);
                ImageBack = Color.FromArgb(238, 238, 238);
                Danger = Color.FromArgb(196, 43, 28);
            }
        }
    }

    internal static class Draw
    {
        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Max(1, radius * 2);
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Color color, Rectangle r, int radius)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(color))
                g.FillPath(b, p);
        }

        public static void StrokeRound(Graphics g, Color color, Rectangle r, int radius, float width)
        {
            Rectangle rr = new Rectangle(r.X, r.Y, Math.Max(1, r.Width - 1), Math.Max(1, r.Height - 1));
            using (GraphicsPath p = Round(rr, radius))
            using (Pen pen = new Pen(color, width))
                g.DrawPath(pen, p);
        }

        public static void FillRounded(Graphics g, Color color, RectangleF r, float radius)
        {
            Rectangle ir = Rectangle.Round(r);
            FillRound(g, color, ir, (int)radius);
        }

        public static int Scale(Control c, int px)
        {
            int dpi = c == null ? 96 : c.DeviceDpi;
            return (int)Math.Round(px * dpi / 96.0);
        }

        public static void CheckMark(Graphics g, Rectangle box, Color color)
        {
            float w = box.Width;
            using (Pen pen = new Pen(color, Math.Max(1.6f, w * 0.12f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                PointF[] pts = new PointF[]
                {
                    new PointF(box.Left + w * 0.24f, box.Top + w * 0.52f),
                    new PointF(box.Left + w * 0.43f, box.Top + w * 0.71f),
                    new PointF(box.Left + w * 0.77f, box.Top + w * 0.32f)
                };
                g.DrawLines(pen, pts);
            }
        }

        public static void Cross(Graphics g, Rectangle box, Color color, float thickness)
        {
            using (Pen pen = new Pen(color, thickness))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int inset = box.Width / 4;
                Rectangle r = new Rectangle(box.X + inset, box.Y + inset, box.Width - inset * 2, box.Height - inset * 2);
                g.DrawLine(pen, r.Left, r.Top, r.Right, r.Bottom);
                g.DrawLine(pen, r.Right, r.Top, r.Left, r.Bottom);
            }
        }

        public static void Chevron(Graphics g, Rectangle box, Color color, bool right)
        {
            using (Pen pen = new Pen(color, 1.8f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                int w = box.Width / 2;
                int cy = box.Top + box.Height / 2;
                int cx = box.Left + box.Width / 2;
                if (right)
                {
                    g.DrawLine(pen, cx - w / 3, cy - w / 2, cx + w / 4, cy);
                    g.DrawLine(pen, cx + w / 4, cy, cx - w / 3, cy + w / 2);
                }
                else
                {
                    g.DrawLine(pen, cx + w / 3, cy - w / 2, cx - w / 4, cy);
                    g.DrawLine(pen, cx - w / 4, cy, cx + w / 3, cy + w / 2);
                }
            }
        }
    }
}
