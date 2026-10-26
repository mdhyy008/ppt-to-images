using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class FluentTabItem
    {
        public string Id;
        public string Title;
        public string Progress;
        public bool Selected;
    }

    internal sealed class FluentTabStrip : Control
    {
        private readonly List<FluentTabItem> items = new List<FluentTabItem>();
        private int hoverIndex = -1;
        private bool hoverClose;
        private int offset;

        public event EventHandler SelectionChanged;
        public event EventHandler CloseRequested;

        public FluentTabStrip()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Font = new Font("Microsoft YaHei UI", 9F);
            Height = Draw.Scale(this, 40);
        }

        public IList<FluentTabItem> Items
        {
            get { return items; }
        }

        public string SelectedId
        {
            get
            {
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Selected) return items[i].Id;
                return null;
            }
        }

        public void Add(string id, string title)
        {
            FluentTabItem item = new FluentTabItem();
            item.Id = id;
            item.Title = title;
            items.Add(item);
            Invalidate();
        }

        public void Remove(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Id == id)
                {
                    bool wasSelected = items[i].Selected;
                    items.RemoveAt(i);
                    if (wasSelected && items.Count > 0)
                        items[Math.Min(i, items.Count - 1)].Selected = true;
                    Invalidate();
                    return;
                }
            }
        }

        public void Select(string id)
        {
            for (int i = 0; i < items.Count; i++)
                items[i].Selected = items[i].Id == id;
            Invalidate();
        }

        public void SetProgress(string id, string progress)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].Id == id) items[i].Progress = progress;
            Invalidate();
        }

        private int TabWidth(FluentTabItem item)
        {
            int maxW = Draw.Scale(this, 230);
            int minW = Draw.Scale(this, 132);
            int chrome = Draw.Scale(this, 52);
            if (!string.IsNullOrEmpty(item.Progress)) chrome += Draw.Scale(this, 46);
            Size t = TextRenderer.MeasureText(item.Title, Font, new Size(1000, Height), TextFormatFlags.NoPrefix);
            int w = t.Width + chrome;
            if (w > maxW) w = maxW;
            if (w < minW) w = minW;
            return w;
        }

        private int TotalWidth()
        {
            int total = 0;
            for (int i = 0; i < items.Count; i++) total += TabWidth(items[i]);
            return total;
        }

        private int ContentWidth
        {
            get { return Width - Draw.Scale(this, 8); }
        }

        private int TabLeft(int index)
        {
            int x = Draw.Scale(this, 4) - offset;
            for (int i = 0; i < index; i++) x += TabWidth(items[i]);
            return x;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(Theme.Window))
                g.FillRectangle(bg, ClientRectangle);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            for (int i = 0; i < items.Count; i++)
            {
                FluentTabItem item = items[i];
                int left = TabLeft(i);
                int w = TabWidth(item);
                Rectangle r = new Rectangle(left, Draw.Scale(this, 4), w, Height - Draw.Scale(this, 6));

                if (r.Right < 0 || r.Left > Width) continue;

                if (i == hoverIndex && !item.Selected)
                    Draw.FillRound(g, Theme.Hover, r, Draw.Scale(this, 6));

                Color textColor = item.Selected ? Theme.Text : Theme.TextMuted;
                int textLeft = r.Left + Draw.Scale(this, 12);
                int textRight = r.Right - Draw.Scale(this, 34);
                if (!string.IsNullOrEmpty(item.Progress))
                {
                    int progressW = TextRenderer.MeasureText(item.Progress, Font).Width;
                    Rectangle pr = new Rectangle(r.Right - Draw.Scale(this, 38) - progressW, r.Top, progressW, r.Height);
                    TextRenderer.DrawText(g, item.Progress, Font, pr, Theme.TextMuted,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.Right | TextFormatFlags.NoPrefix);
                    textRight = pr.Left - Draw.Scale(this, 8);
                }

                Rectangle tr = new Rectangle(textLeft, r.Top, Math.Max(0, textRight - textLeft), r.Height);
                TextRenderer.DrawText(g, item.Title, Font, tr, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left |
                    TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

                Rectangle close = CloseRect(r);
                if (i == hoverIndex && hoverClose)
                {
                    Draw.FillRound(g, Theme.Hover, close, Draw.Scale(this, 4));
                }
                Draw.Cross(g, close, item.Selected ? Theme.Text : Theme.TextMuted, 1.5f);

                if (item.Selected)
                {
                    Rectangle underline = new Rectangle(r.Left + Draw.Scale(this, 10), r.Bottom - Draw.Scale(this, 2),
                        r.Width - Draw.Scale(this, 20), Draw.Scale(this, 3));
                    Draw.FillRound(g, Theme.Accent, underline, 2);
                }
            }

            // 溢出的左右滚动提示
            if (offset > 0)
            {
                Rectangle left = new Rectangle(0, 0, Draw.Scale(this, 18), Height);
                using (SolidBrush b = new SolidBrush(Theme.Window)) g.FillRectangle(b, left);
                Draw.Chevron(g, left, Theme.TextMuted, false);
            }
            if (TotalWidth() - offset > ContentWidth)
            {
                Rectangle right = new Rectangle(Width - Draw.Scale(this, 18), 0, Draw.Scale(this, 18), Height);
                using (SolidBrush b = new SolidBrush(Theme.Window)) g.FillRectangle(b, right);
                Draw.Chevron(g, right, Theme.TextMuted, true);
            }

            using (Pen p = new Pen(Theme.Divider))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
        }

        private Rectangle CloseRect(Rectangle tab)
        {
            int size = Draw.Scale(this, 22);
            return new Rectangle(tab.Right - Draw.Scale(this, 30), tab.Top + (tab.Height - size) / 2, size, size);
        }

        private int HitTest(Point p, out bool close)
        {
            close = false;
            for (int i = 0; i < items.Count; i++)
            {
                int left = TabLeft(i);
                Rectangle r = new Rectangle(left, 0, TabWidth(items[i]), Height);
                if (r.Contains(p))
                {
                    close = CloseRect(r).Contains(p);
                    return i;
                }
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool close;
            int hit = HitTest(e.Location, out close);
            if (hit != hoverIndex || close != hoverClose)
            {
                hoverIndex = hit;
                hoverClose = close;
                Cursor = close ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hoverIndex = -1;
            hoverClose = false;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                bool close;
                int hit = HitTest(e.Location, out close);
                if (hit >= 0)
                {
                    if (close)
                    {
                        if (CloseRequested != null) CloseRequested(items[hit].Id, EventArgs.Empty);
                    }
                    else
                    {
                        Select(items[hit].Id);
                        if (SelectionChanged != null) SelectionChanged(items[hit].Id, EventArgs.Empty);
                    }
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ScrollBy(-e.Delta / 2);
            base.OnMouseWheel(e);
        }

        public void ScrollBy(int dx)
        {
            int max = Math.Max(0, TotalWidth() - ContentWidth);
            offset += dx;
            if (offset < 0) offset = 0;
            if (offset > max) offset = max;
            Invalidate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ScrollBy(0);
        }
    }
}
