using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class FluentButton : Control
    {
        private bool hovered;
        private bool pressed;
        private bool accent;
        private bool compact;

        public bool AccentStyle
        {
            get { return accent; }
            set { accent = value; Invalidate(); }
        }

        public bool Compact
        {
            get { return compact; }
            set { compact = value; Invalidate(); }
        }

        public FluentButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            Font = new Font("Microsoft YaHei UI", 9F);
            TabStop = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(Theme.Window))
                g.FillRectangle(bg, ClientRectangle);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int pad = Draw.Scale(this, compact ? 10 : 14);
            int radius = Draw.Scale(this, 6);
            Rectangle r = new Rectangle(0, 0, Width, Height);

            Color textColor;
            if (accent)
            {
                Color fill = pressed ? Mix(Theme.Accent, Color.Black, 0.12f)
                           : hovered ? Theme.AccentHover : Theme.Accent;
                if (!Enabled) fill = Mix(Theme.Surface, Theme.TextMuted, 0.25f);
                Draw.FillRound(g, fill, r, radius);
                textColor = Enabled ? Theme.AccentText : Theme.TextMuted;
            }
            else
            {
                Color fill = !Enabled ? Theme.Window
                           : pressed ? Mix(Theme.Surface, Theme.Text, 0.08f)
                           : hovered ? Theme.SurfaceHover : Theme.Surface;
                Draw.FillRound(g, fill, r, radius);
                Draw.StrokeRound(g, Theme.Border, r, radius, 1f);
                textColor = Enabled ? Theme.Text : Theme.TextMuted;
            }

            TextRenderer.DrawText(g, Text, Font, r, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPrefix | TextFormatFlags.EndEllipsis | TextFormatFlags.PreserveGraphicsClipping);

            if (Focused)
            {
                Rectangle fr = new Rectangle(pad / 2, pad / 2, Width - pad, Height - pad);
                using (Pen p = new Pen(Color.FromArgb(140, Theme.Accent), 1.2f))
                {
                    using (System.Drawing.Drawing2D.GraphicsPath gp = Draw.Round(fr, radius))
                        g.DrawPath(p, gp);
                }
            }
        }

        private static Color Mix(Color a, Color b, float amount)
        {
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * amount),
                (int)(a.R + (b.R - a.R) * amount),
                (int)(a.G + (b.G - a.G) * amount),
                (int)(a.B + (b.B - a.B) * amount));
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size t = TextRenderer.MeasureText(Text, Font);
            int w = t.Width + Draw.Scale(this, compact ? 22 : 30);
            int h = Math.Max(Draw.Scale(this, 32), t.Height + Draw.Scale(this, 12));
            return new Size(w, h);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                pressed = true;
                Invalidate();
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }
    }
}
