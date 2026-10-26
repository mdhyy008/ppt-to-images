using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class PreviewForm : Form
    {
        private readonly List<PageItem> pages;
        private readonly Font labelFont = new Font("Microsoft YaHei UI", 10F);
        private int index;
        private Bitmap image;

        public PreviewForm(IWin32Window owner, List<PageItem> pages, int startIndex)
        {
            this.pages = pages;
            index = Math.Max(0, Math.Min(startIndex, pages.Count - 1));

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;

            Control ownerControl = owner as Control;
            Screen screen = ownerControl != null ? Screen.FromControl(ownerControl) : Screen.PrimaryScreen;
            Bounds = screen.Bounds;
            BackColor = Color.FromArgb(16, 16, 16);
            KeyPreview = true;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            LoadImage();
        }

        private void LoadImage()
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
            if (index < 0 || index >= pages.Count) return;
            string path = pages[index].FullPath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (Image src = Image.FromStream(fs))
                    image = new Bitmap(src);
            }
            catch
            {
            }
            Invalidate();
        }

        private void Navigate(int delta)
        {
            int target = index + delta;
            if (target < 0) target = 0;
            if (target >= pages.Count) target = pages.Count - 1;
            if (target == index) return;
            index = target;
            LoadImage();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Close();
                return true;
            }
            if (keyData == Keys.Left || keyData == Keys.PageUp)
            {
                Navigate(-1);
                return true;
            }
            if (keyData == Keys.Right || keyData == Keys.PageDown || keyData == Keys.Space)
            {
                Navigate(1);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            Navigate(e.Delta > 0 ? -1 : 1);
            base.OnMouseWheel(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) Close();
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(BackColor))
                g.FillRectangle(b, ClientRectangle);

            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            int margin = Draw.Scale(this, 44);
            Rectangle area = new Rectangle(margin, margin, Width - margin * 2, Height - margin * 2);
            if (image != null && area.Width > 0 && area.Height > 0)
            {
                float scale = Math.Min((float)area.Width / image.Width, (float)area.Height / image.Height);
                int w = (int)(image.Width * scale);
                int h = (int)(image.Height * scale);
                Rectangle r = new Rectangle(area.Left + (area.Width - w) / 2, area.Top + (area.Height - h) / 2, w, h);
                g.DrawImage(image, r);
            }

            string label = string.Format("{0} / {1}", index + 1, pages.Count);
            Rectangle labelRect = new Rectangle(0, Draw.Scale(this, 10), Width, Draw.Scale(this, 24));
            TextRenderer.DrawText(g, label, labelFont,
                labelRect, Color.FromArgb(180, 180, 180),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top | TextFormatFlags.NoPrefix);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (image != null)
            {
                image.Dispose();
                image = null;
            }
            base.OnFormClosed(e);
        }
    }
}
