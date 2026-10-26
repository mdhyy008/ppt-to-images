using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class DropZone : Control
    {
        private const string HintText = ".ppt / .pptx / .pps / .ppsx / .pptm / .pot / .potx";

        private bool dragHover;
        private readonly Font hintFont = new Font("Microsoft YaHei UI", 9F);

        public DropZone()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = new Font("Microsoft YaHei UI", 10F);
            BackColor = Theme.Window;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) hintFont.Dispose();
            base.Dispose(disposing);
        }

        public bool DragHover
        {
            get { return dragHover; }
            set
            {
                if (dragHover == value) return;
                dragHover = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(Theme.Window))
                g.FillRectangle(b, ClientRectangle);

            g.SmoothingMode = SmoothingMode.AntiAlias;

            int inset = Draw.Scale(this, 28);
            int radius = Draw.Scale(this, 10);
            Rectangle box = new Rectangle(inset, inset, Width - inset * 2, Height - inset * 2);
            if (box.Width <= 0 || box.Height <= 0) return;

            using (GraphicsPath path = Draw.Round(new Rectangle(box.X, box.Y, box.Width - 1, box.Height - 1), radius))
            using (Pen pen = new Pen(dragHover ? Theme.Accent : Theme.Border, 1.5f))
            {
                pen.DashStyle = DashStyle.Dash;
                pen.DashPattern = new float[] { 4f, 3f };
                g.DrawPath(pen, path);
            }

            if (dragHover)
            {
                using (SolidBrush b = new SolidBrush(Color.FromArgb(18, Theme.Accent)))
                {
                    using (GraphicsPath path = Draw.Round(box, radius))
                        g.FillPath(b, path);
                }
            }

            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.NoPrefix;
            string main = "拖入文档";
            Size mainSize = TextRenderer.MeasureText(main, Font);
            Size hintSize = TextRenderer.MeasureText(HintText, hintFont);
            int gap = Draw.Scale(this, 8);
            int top = box.Top + (box.Height - (mainSize.Height + gap + hintSize.Height)) / 2;
            Color color = dragHover ? Theme.Accent : Theme.TextMuted;

            TextRenderer.DrawText(g, main, Font,
                new Rectangle(box.Left, top, box.Width, mainSize.Height), color, flags);
            TextRenderer.DrawText(g, HintText, hintFont,
                new Rectangle(box.Left, top + mainSize.Height + gap, box.Width, hintSize.Height), color, flags);
        }
    }
}
