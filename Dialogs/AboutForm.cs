using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class AboutForm : Form
    {
        private readonly Font titleFont = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
        private readonly Font versionFont = new Font("Microsoft YaHei UI", 9F);
        private readonly FluentButton okButton;

        public AboutForm()
        {
            Text = "关于";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Window;
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(420, 260);
            DoubleBuffered = true;

            okButton = new FluentButton();
            okButton.Text = "确定";
            okButton.AccentStyle = true;
            okButton.Click += delegate { Close(); };
            Controls.Add(okButton);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (okButton == null) return;
            int pad = Draw.Scale(this, 20);
            Size size = okButton.PreferredSize;
            okButton.Bounds = new Rectangle(ClientSize.Width - pad - size.Width,
                ClientSize.Height - pad - size.Height, size.Width, size.Height);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Native.ApplyWindowChrome(Handle, Theme.IsDark);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            int left = Draw.Scale(this, 28);

            Rectangle titleRect = new Rectangle(left, Draw.Scale(this, 26), ClientSize.Width - left * 2, Draw.Scale(this, 40));
            TextRenderer.DrawText(g, "SlideShot", titleFont, titleRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            Rectangle versionRect = new Rectangle(left, titleRect.Bottom + Draw.Scale(this, 2),
                ClientSize.Width - left * 2, Draw.Scale(this, 22));
            TextRenderer.DrawText(g, "版本 " + version, versionFont, versionRect, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            using (Pen p = new Pen(Theme.Divider))
                g.DrawLine(p, left, versionRect.Bottom + Draw.Scale(this, 16),
                    ClientSize.Width - left, versionRect.Bottom + Draw.Scale(this, 16));

            int y = versionRect.Bottom + Draw.Scale(this, 34);
            Rectangle dev = new Rectangle(left, y, ClientSize.Width - left * 2, Draw.Scale(this, 22));
            TextRenderer.DrawText(g, "开发者：明灯花月夜", Font, dev, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Rectangle site = new Rectangle(left, dev.Bottom + Draw.Scale(this, 6), ClientSize.Width - left * 2, Draw.Scale(this, 22));
            TextRenderer.DrawText(g, "网址：mdhyy.cn", Font, site, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
