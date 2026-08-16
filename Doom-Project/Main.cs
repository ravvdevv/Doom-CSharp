using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Doom_Project
{
    public partial class Main : Form
    {
        // by raven
        private readonly List<Image> _bgFrames = new List<Image>();
        private int _bgIndex;
        private int _bgNextIndex;
        private float _bgFadeAlpha = 1f;
        private readonly Timer _bgSwitchTimer = new Timer();
        private readonly Timer _bgFadeTimer = new Timer();
        private Point _titlePos;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            string imagesPath = Path.Combine(Application.StartupPath, "resources", "images");
            foreach (string file in Directory.GetFiles(imagesPath, "bg*.png").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
            {
                _bgFrames.Add(Image.FromFile(file));
            }

            if (_bgFrames.Count > 1)
            {
                _bgSwitchTimer.Interval = 6000;
                _bgSwitchTimer.Tick += BgSwitchTimer_Tick;
                _bgSwitchTimer.Start();

                _bgFadeTimer.Interval = 2;
                _bgFadeTimer.Tick += BgFadeTimer_Tick;
            }

            Resize += Main_Resize;
            LayoutMenu();
        }

        private void LayoutMenu()
        {
            const int margin = 60;
            const int gapTitleSubtitle = 8;
            const int gapSubtitleButtons = 44;
            const int gapButtons = 16;

            Size titleSize;
            using (Graphics g = CreateGraphics())
            {
                titleSize = Size.Ceiling(g.MeasureString("DOOM", lbTitle.Font));
            }

            int titleH = titleSize.Height;
            int subtitleH = lbSubtitle.Height;
            int btnH = btnStart.Height;
            int totalH = titleH + gapTitleSubtitle + subtitleH + gapSubtitleButtons + btnH + gapButtons + btnH;
            int top = ClientSize.Height / 2 - totalH / 2;

            int x = margin;
            _titlePos = new Point(x, top);
            lbSubtitle.Location = new Point(x, top + titleH + gapTitleSubtitle);
            btnStart.Location = new Point(x, top + titleH + gapTitleSubtitle + subtitleH + gapSubtitleButtons);
            btnQuit.Location = new Point(x, btnStart.Bottom + gapButtons);
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            LayoutMenu();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (_bgFrames.Count > 0)
            {
                Rectangle dest = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                e.Graphics.DrawImage(_bgFrames[_bgNextIndex], dest);
                if (_bgFadeAlpha < 1f)
                {
                    DrawImageAlpha(e.Graphics, _bgFrames[_bgIndex], dest, _bgFadeAlpha);
                }
            }
            else
            {
                base.OnPaint(e);
            }

            if (!_titlePos.IsEmpty)
            {
                DrawGlowTitle(e.Graphics);
            }
        }

        private static void DrawImageAlpha(Graphics g, Image image, Rectangle dest, float alpha)
        {
            using (ImageAttributes attrs = new ImageAttributes())
            {
                ColorMatrix cm = new ColorMatrix();
                cm.Matrix33 = alpha;
                attrs.SetColorMatrix(cm);
                g.DrawImage(image, dest, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attrs);
            }
        }

        private void DrawGlowTitle(Graphics g)
        {
            const string text = "DOOM";
            Font font = lbTitle.Font;
            int x = _titlePos.X;
            int y = _titlePos.Y;

            using (Brush shadow = new SolidBrush(Color.FromArgb(180, 20, 10, 5)))
            {
                g.DrawString(text, font, shadow, x + 6, y + 6);
            }

            for (int ring = 6; ring >= 1; ring--)
            {
                int alpha = ring == 1 ? 70 : 12;
                using (Brush glow = new SolidBrush(Color.FromArgb(alpha, 255, 205, 60)))
                {
                    for (int i = 0; i < 12; i++)
                    {
                        double a = i / 12.0 * Math.PI * 2;
                        int dx = (int)Math.Round(Math.Cos(a) * ring * 2.5);
                        int dy = (int)Math.Round(Math.Sin(a) * ring * 2.5);
                        g.DrawString(text, font, glow, x + dx, y + dy);
                    }
                }
            }

            using (Brush core = new SolidBrush(Color.FromArgb(255, 255, 214, 84)))
            {
                g.DrawString(text, font, core, x, y);
            }
        }

        private void BgSwitchTimer_Tick(object sender, EventArgs e)
        {
            _bgNextIndex = (_bgIndex + 1) % _bgFrames.Count;
            _bgFadeTimer.Start();
        }

        private void BgFadeTimer_Tick(object sender, EventArgs e)
        {
            _bgFadeAlpha -= 0.016f / 1.5f;
            if (_bgFadeAlpha <= 0f)
            {
                _bgFadeAlpha = 1f;
                _bgIndex = _bgNextIndex;
                _bgFadeTimer.Stop();
            }
            Invalidate();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _bgSwitchTimer.Stop();
            _bgSwitchTimer.Dispose();
            _bgFadeTimer.Stop();
            _bgFadeTimer.Dispose();
            foreach (Image frame in _bgFrames)
            {
                frame.Dispose();
            }
            _bgFrames.Clear();
            base.OnFormClosing(e);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Hide();
            using (Game game = new Game())
            {
                game.ShowDialog();
            }
            Show();
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void lbTitle_Click(object sender, EventArgs e)
        {

        }

        private void lbSubtitle_Click(object sender, EventArgs e)
        {

        }
    }
}
