using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class FluentSegmented : Control
    {
        private string[] items;
        private int selectedIndex;
        private int hoverIndex = -1;

        public event EventHandler SelectedIndexChanged;

        public FluentSegmented()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Hand;
            items = new string[0];
            Font = new Font("Microsoft YaHei UI", 9F);
        }

        public void SetItems(params string[] labels)
        {
            items = labels ?? new string[0];
            selectedIndex = 0;
            Width = GetPreferredSize(Size.Empty).Width;
            Invalidate();
        }

        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                if (value < 0 || value >= items.Length || value == selectedIndex) return;
                selectedIndex = value;
                Invalidate();
                if (SelectedIndexChanged != null) SelectedIndexChanged(this, EventArgs.Empty);
            }
        }

        private int Inset
        {
            get { return Draw.Scale(this, 3); }
        }

        private int[] SegmentWidths()
        {
            int[] widths = new int[items.Length];
            for (int i = 0; i < items.Length; i++)
            {
                Size t = TextRenderer.MeasureText(items[i], Font);
                widths[i] = t.Width + Draw.Scale(this, 26);
            }
            return widths;
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            int[] widths = SegmentWidths();
            int total = Inset * 2;
            for (int i = 0; i < widths.Length; i++) total += widths[i];
            int h = Math.Max(Draw.Scale(this, 32), TextRenderer.MeasureText("格式", Font).Height + Draw.Scale(this, 12));
            return new Size(total, h);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(Theme.Window))
                g.FillRectangle(bg, ClientRectangle);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int radius = Draw.Scale(this, 6);
            Rectangle outer = new Rectangle(0, 0, Width, Height);
            Draw.FillRound(g, Theme.Surface, outer, radius);
            Draw.StrokeRound(g, Theme.Border, outer, radius, 1f);

            int[] widths = SegmentWidths();
            int x = Inset;
            for (int i = 0; i < items.Length; i++)
            {
                Rectangle seg = new Rectangle(x, Inset, widths[i], Height - Inset * 2);
                if (i == selectedIndex)
                {
                    Draw.FillRound(g, Theme.Accent, seg, Math.Max(2, radius - 1));
                }
                else if (i == hoverIndex)
                {
                    Draw.FillRound(g, Theme.Hover, seg, Math.Max(2, radius - 1));
                }

                Color color = i == selectedIndex ? Theme.AccentText : Theme.TextMuted;
                TextRenderer.DrawText(g, items[i], Font, seg, color,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                    TextFormatFlags.NoPrefix | TextFormatFlags.PreserveGraphicsClipping);

                x += widths[i];
            }
        }

        private int HitTest(int px)
        {
            int[] widths = SegmentWidths();
            int x = Inset;
            for (int i = 0; i < widths.Length; i++)
            {
                if (px >= x && px < x + widths[i]) return i;
                x += widths[i];
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = HitTest(e.X);
            if (hit != hoverIndex)
            {
                hoverIndex = hit;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            int hit = HitTest(e.X);
            if (hit >= 0) SelectedIndex = hit;
            base.OnMouseUp(e);
        }

        protected override void OnFontChanged(EventArgs e)
        {
            Width = GetPreferredSize(Size.Empty).Width;
            base.OnFontChanged(e);
        }
    }
}
