using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class FluentStatusBar : Control
    {
        private string message = "";
        private bool error;
        private bool progressVisible;
        private double progress;
        private readonly FluentButton cancelButton;
        private readonly FluentButton openButton;

        public FluentStatusBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Height = Draw.Scale(this, 46);

            cancelButton = new FluentButton();
            cancelButton.Text = "取消";
            cancelButton.Visible = false;
            cancelButton.Click += delegate { if (CancelClicked != null) CancelClicked(this, EventArgs.Empty); };
            Controls.Add(cancelButton);

            openButton = new FluentButton();
            openButton.Text = "打开文件夹";
            openButton.Visible = false;
            openButton.Click += delegate { if (OpenFolderClicked != null) OpenFolderClicked(this, EventArgs.Empty); };
            Controls.Add(openButton);

            Font = new Font("Microsoft YaHei UI", 9F);
        }

        public event EventHandler CancelClicked;
        public event EventHandler OpenFolderClicked;

        public void SetMessage(string text, bool isError)
        {
            message = text ?? "";
            error = isError;
            Invalidate();
        }

        public void SetProgress(bool visible, double value)
        {
            progressVisible = visible;
            progress = value;
            Invalidate();
        }

        public bool CancelVisible
        {
            get { return cancelButton.Visible; }
            set { cancelButton.Visible = value; LayoutButtons(); }
        }

        public bool OpenFolderVisible
        {
            get { return openButton.Visible; }
            set { openButton.Visible = value; LayoutButtons(); }
        }

        private void LayoutButtons()
        {
            if (cancelButton == null || openButton == null) return;
            int pad = Draw.Scale(this, 12);
            int y = (Height - openButton.PreferredSize.Height) / 2;
            int x = Width - pad;
            if (openButton.Visible)
            {
                Size s = openButton.PreferredSize;
                x -= s.Width;
                openButton.Bounds = new Rectangle(x, y, s.Width, s.Height);
                x -= Draw.Scale(this, 8);
            }
            if (cancelButton.Visible)
            {
                Size s = cancelButton.PreferredSize;
                x -= s.Width;
                cancelButton.Bounds = new Rectangle(x, y, s.Width, s.Height);
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutButtons();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (SolidBrush b = new SolidBrush(Theme.Window))
                g.FillRectangle(b, ClientRectangle);
            using (Pen p = new Pen(Theme.Divider))
                g.DrawLine(p, 0, 0, Width, 0);

            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            int pad = Draw.Scale(this, 16);
            int left = pad;
            int barWidth = Draw.Scale(this, 120);

            if (progressVisible)
            {
                Rectangle track = new Rectangle(left, (Height - Draw.Scale(this, 4)) / 2, barWidth, Draw.Scale(this, 4));
                Draw.FillRound(g, Theme.Border, track, track.Height / 2);
                int filled = (int)(track.Width * Math.Max(0, Math.Min(1, progress)));
                if (filled > 0)
                {
                    Rectangle fill = new Rectangle(track.X, track.Y, Math.Max(track.Height, filled), track.Height);
                    Draw.FillRound(g, Theme.Accent, fill, track.Height / 2);
                }
                left = track.Right + Draw.Scale(this, 12);
            }

            int right = Width - pad;
            if (cancelButton.Visible) right = Math.Min(right, cancelButton.Left - Draw.Scale(this, 12));
            if (openButton.Visible) right = Math.Min(right, openButton.Left - Draw.Scale(this, 12));

            Rectangle textRect = new Rectangle(left, Draw.Scale(this, 2), Math.Max(0, right - left), Height - Draw.Scale(this, 4));
            TextRenderer.DrawText(g, message, Font, textRect, error ? Theme.Danger : Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
