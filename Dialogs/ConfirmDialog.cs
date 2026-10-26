using System;
using System.Drawing;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class ConfirmDialog : Form
    {
        private const int DialogWidth = 560;

        private readonly string message;
        private readonly int messageHeight;
        private readonly Font titleFont = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        private readonly FluentButton yesButton;
        private readonly FluentButton noButton;
        private int result = -1;

        private ConfirmDialog(string title, string message, string yesText, string noText)
        {
            Text = title;
            this.message = message;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Theme.Window;
            Font = new Font("Microsoft YaHei UI", 9F);
            messageHeight = TextRenderer.MeasureText(message, Font, new Size(DialogWidth - 24 * 2, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix).Height;
            ClientSize = new Size(DialogWidth, Math.Max(170, 120 + messageHeight));
            DoubleBuffered = true;

            noButton = new FluentButton();
            noButton.Text = noText;
            noButton.Click += delegate { result = 1; Close(); };
            Controls.Add(noButton);

            yesButton = new FluentButton();
            yesButton.Text = yesText;
            yesButton.AccentStyle = true;
            yesButton.Click += delegate { result = 0; Close(); };
            Controls.Add(yesButton);
        }

        public static int Ask(IWin32Window owner, string title, string message, string yesText, string noText)
        {
            using (ConfirmDialog dialog = new ConfirmDialog(title, message, yesText, noText))
            {
                dialog.ShowDialog(owner);
                return dialog.result;
            }
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            if (yesButton == null || noButton == null) return;
            int pad = Draw.Scale(this, 18);
            int bottom = ClientSize.Height - pad;
            Size yesSize = yesButton.PreferredSize;
            Size noSize = noButton.PreferredSize;
            yesButton.Bounds = new Rectangle(ClientSize.Width - pad - yesSize.Width, bottom - yesSize.Height, yesSize.Width, yesSize.Height);
            noButton.Bounds = new Rectangle(yesButton.Left - Draw.Scale(this, 10) - noSize.Width, yesButton.Top, noSize.Width, noSize.Height);
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
            int pad = Draw.Scale(this, 24);
            Rectangle titleRect = new Rectangle(pad, Draw.Scale(this, 22), ClientSize.Width - pad * 2, Draw.Scale(this, 26));
            TextRenderer.DrawText(g, Text, titleFont, titleRect, Theme.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            Rectangle msg = new Rectangle(pad, titleRect.Bottom + Draw.Scale(this, 10),
                ClientSize.Width - pad * 2, messageHeight);
            TextRenderer.DrawText(g, message, Font, msg, Theme.TextMuted,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.WordBreak | TextFormatFlags.NoPrefix);
        }
    }
}
