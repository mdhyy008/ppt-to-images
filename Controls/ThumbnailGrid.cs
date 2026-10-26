using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class ThumbnailCard : Control
    {
        private readonly PageItem item;
        private readonly int imageHeight;
        private bool hovered;
        private bool checkHover;
        private bool isChecked;
        private Bitmap image;

        public event EventHandler CheckedChanged;
        public event EventHandler PreviewRequested;

        public ThumbnailCard(PageItem pageItem, int cardWidth, int imageHeight)
        {
            item = pageItem;
            this.imageHeight = imageHeight;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Cursor = Cursors.Default;
            Width = cardWidth;
            Height = imageHeight + Draw.Scale(this, 36);
        }

        public PageItem Page
        {
            get { return item; }
        }

        public bool Checked
        {
            get { return isChecked; }
            set
            {
                if (isChecked == value) return;
                isChecked = value;
                Invalidate();
                if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
            }
        }

        public void LoadThumbnail()
        {
            if (image != null || item.ThumbPath == null) return;
            try
            {
                using (FileStream fs = new FileStream(item.ThumbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (Image src = Image.FromStream(fs))
                        image = new Bitmap(src);
                }
            }
            catch
            {
            }
            Invalidate();
        }

        public void DisposeImage()
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
        }

        private Rectangle ImageRect
        {
            get
            {
                int pad = Draw.Scale(this, 6);
                return new Rectangle(pad, pad, Width - pad * 2, imageHeight - pad * 2);
            }
        }

        private Rectangle CheckRect
        {
            get
            {
                int size = Draw.Scale(this, 24);
                int pad = Draw.Scale(this, 12);
                return new Rectangle(Width - size - pad, pad + Draw.Scale(this, 4), size, size);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush bg = new SolidBrush(Theme.Window))
                g.FillRectangle(bg, ClientRectangle);

            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            int radius = Draw.Scale(this, 8);
            Rectangle card = new Rectangle(0, 0, Width, Height);
            Draw.FillRound(g, Theme.Surface, card, radius);
            Draw.StrokeRound(g, hovered ? Theme.Accent : Theme.Border, card, radius, hovered ? 1.4f : 1f);

            Rectangle ir = ImageRect;
            using (SolidBrush b = new SolidBrush(Theme.ImageBack))
                g.FillRectangle(b, ir);

            if (image != null)
            {
                float scale = Math.Min((float)ir.Width / image.Width, (float)ir.Height / image.Height);
                int w = (int)(image.Width * scale);
                int h = (int)(image.Height * scale);
                int x = ir.Left + (ir.Width - w) / 2;
                int y = ir.Top + (ir.Height - h) / 2;
                g.DrawImage(image, new Rectangle(x, y, w, h));
            }

            Rectangle footer = new Rectangle(Draw.Scale(this, 12), imageHeight - Draw.Scale(this, 4),
                Width - Draw.Scale(this, 24), Height - imageHeight);
            TextRenderer.DrawText(g, "第 " + item.Number + " 页", Font, footer, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Rectangle cr = CheckRect;
            if (isChecked)
            {
                Draw.FillRound(g, Theme.Accent, cr, Draw.Scale(this, 6));
                Draw.CheckMark(g, cr, Theme.AccentText);
            }
            else
            {
                Color boxFill = Theme.IsDark ? Color.FromArgb(190, 32, 32, 32) : Color.FromArgb(215, 255, 255, 255);
                Draw.FillRound(g, boxFill, cr, Draw.Scale(this, 6));
                Draw.StrokeRound(g, checkHover ? Theme.Accent : Color.FromArgb(150, 128, 128, 128), cr,
                    Draw.Scale(this, 6), checkHover ? 1.6f : 1.2f);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Cursor = Cursors.Hand;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            checkHover = false;
            Cursor = Cursors.Default;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool h = CheckRect.Contains(e.Location);
            if (h != checkHover)
            {
                checkHover = h;
                Cursor = h ? Cursors.Hand : Cursors.Default;
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                Checked = !Checked;
            base.OnMouseUp(e);
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && !CheckRect.Contains(e.Location))
            {
                if (PreviewRequested != null) PreviewRequested(this, EventArgs.Empty);
            }
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            ThumbnailGrid grid = Parent as ThumbnailGrid;
            if (grid != null) grid.ScrollWheel(e.Delta);
            base.OnMouseWheel(e);
        }
    }

    internal sealed class ThumbnailGrid : ScrollableControl
    {
        private readonly List<ThumbnailCard> cards = new List<ThumbnailCard>();
        private int imageHeight;

        public event EventHandler SelectionChanged;
        public event EventHandler PreviewRequested;

        public ThumbnailGrid()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoScroll = true;
            BackColor = Theme.Window;
            imageHeight = Draw.Scale(this, 152);
        }

        public IList<ThumbnailCard> Cards
        {
            get { return cards; }
        }

        private bool dragHover;

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

        public void ClearCards()
        {
            for (int i = 0; i < cards.Count; i++)
                cards[i].DisposeImage();
            for (int i = Controls.Count - 1; i >= 0; i--) Controls[i].Dispose();
            cards.Clear();
            AutoScrollMinSize = new Size(0, 0);
            Invalidate();
        }

        public void SetAspect(double widthOverHeight)
        {
            if (widthOverHeight <= 0.05 || widthOverHeight > 20) return;
            int cardWidth = Draw.Scale(this, 240);
            int h = (int)Math.Round(cardWidth / widthOverHeight);
            int pad = Draw.Scale(this, 12);
            h = h - pad * 2;
            int min = Draw.Scale(this, 120);
            int max = Draw.Scale(this, 320);
            if (h < min) h = min;
            if (h > max) h = max;
            imageHeight = h + pad * 2;
        }

        public ThumbnailCard AddPage(PageItem page, bool reflow)
        {
            ThumbnailCard card = new ThumbnailCard(page, Draw.Scale(this, 240), imageHeight);
            card.CheckedChanged += delegate
            {
                if (!silent && SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
            };
            card.PreviewRequested += delegate { if (PreviewRequested != null) PreviewRequested(card, EventArgs.Empty); };
            cards.Add(card);
            Controls.Add(card);
            if (reflow) Reflow();
            return card;
        }

        private bool silent;

        private void RaiseSelection()
        {
            if (SelectionChanged != null) SelectionChanged(this, EventArgs.Empty);
        }

        public void Reflow()
        {
            int pad = Draw.Scale(this, 16);
            int gap = Draw.Scale(this, 16);
            int cardW = Draw.Scale(this, 240);
            int cardH = imageHeight + Draw.Scale(this, 36);
            int cols = Math.Max(1, (ClientSize.Width - pad * 2 + gap) / (cardW + gap));

            for (int i = 0; i < cards.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                cards[i].Bounds = new Rectangle(
                    pad + col * (cardW + gap) - AutoScrollPosition.X,
                    pad + row * (cardH + gap) - AutoScrollPosition.Y,
                    cardW, cardH);
            }

            int rows = (cards.Count + cols - 1) / cols;
            AutoScrollMinSize = new Size(0, rows * (cardH + gap) + pad);
            Invalidate();
        }

        public void SetAllChecked(bool value)
        {
            silent = true;
            try
            {
                for (int i = 0; i < cards.Count; i++) cards[i].Checked = value;
            }
            finally
            {
                silent = false;
            }
            RaiseSelection();
        }

        public void InvertChecked()
        {
            silent = true;
            try
            {
                for (int i = 0; i < cards.Count; i++) cards[i].Checked = !cards[i].Checked;
            }
            finally
            {
                silent = false;
            }
            RaiseSelection();
        }

        public int CheckedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < cards.Count; i++) if (cards[i].Checked) n++;
                return n;
            }
        }

        public List<PageItem> GetCheckedPages()
        {
            List<PageItem> list = new List<PageItem>();
            for (int i = 0; i < cards.Count; i++)
                if (cards[i].Checked) list.Add(cards[i].Page);
            return list;
        }

        public void ScrollWheel(int delta)
        {
            int y = -AutoScrollPosition.Y;
            y -= delta / 3;
            if (y < 0) y = 0;
            int max = Math.Max(0, AutoScrollMinSize.Height - ClientSize.Height);
            if (y > max) y = max;
            AutoScrollPosition = new Point(0, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(Theme.Window))
                e.Graphics.FillRectangle(b, ClientRectangle);

            if (dragHover)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                Rectangle r = new Rectangle(Draw.Scale(this, 6), Draw.Scale(this, 6),
                    Width - Draw.Scale(this, 14), Height - Draw.Scale(this, 14));
                Draw.StrokeRound(e.Graphics, Theme.Accent, r, Draw.Scale(this, 10), 2f);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Reflow();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            BeginInvoke(new Action(delegate { Native.ApplyDarkScrollbars(Handle); }));
        }
    }
}
