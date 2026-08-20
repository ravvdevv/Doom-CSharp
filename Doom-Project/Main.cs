using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  MAIN MENU - the title screen when you first open the game
    //  by raven
    // tribute to rene batterbonia mr.mvp 67
    //  shows animated background, glowing title, start/quit buttons
    //  after game over, shows your last score briefly
    // ================================================================

    public partial class Main : Form
    {
        // background animation - cycles through bg0.png, bg1.png, etc
        private readonly List<Image> _bgFrames = new List<Image>();
        private int _bgIndex;           // which background is currently showing
        private int _bgNextIndex;       // which background we're fading to
        private float _bgFadeAlpha = 1f; // fade progress (1 = fully visible, 0 = invisible)
        private readonly Timer _bgSwitchTimer = new Timer();  // when to switch backgrounds
        private readonly Timer _bgFadeTimer = new Timer();    // fade animation timer
        private Point _titlePos;         // where to draw the "DOOM" title

        // last run score - shows briefly after game over
        // fades in with gold text, then fades out after 5 seconds
        private Label _lbLastRun;
        private Timer _lastRunFadeTimer;
        private float _lastRunAlpha = 0f;

        public Main()
        {
            InitializeComponent();
        }

        // ================================================================
        //  LOAD - set up the menu when the form first appears
        // ================================================================

        private void Main_Load(object sender, EventArgs e)
        {
            // load all background images (bg0.png, bg1.png, etc)
            string imagesPath = Path.Combine(Application.StartupPath, "resources", "images");
            string[] files = Directory.GetFiles(imagesPath, "bg*.png");

            // sort files by name so they play in order
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);

            // load each image into our list
            foreach (string file in files)
            {
                _bgFrames.Add(Image.FromFile(file));
            }

            // set up background switching if we have more than one image
            if (_bgFrames.Count > 1)
            {
                _bgSwitchTimer.Interval = 6000;  // switch every 6 seconds
                _bgSwitchTimer.Tick += BgSwitchTimer_Tick;
                _bgSwitchTimer.Start();

                _bgFadeTimer.Interval = 2;  // fade speed
                _bgFadeTimer.Tick += BgFadeTimer_Tick;
            }

            // set up layout and last run label
            Resize += Main_Resize;
            LayoutMenu();
            SetupLastRunLabel();
        }

        // ================================================================
        //  LAST RUN LABEL - shows your score after game over
        // ================================================================

        // create the "last run" label - hidden by default
        // only shows after player dies and returns to menu
        private void SetupLastRunLabel()
        {
            _lbLastRun = new Label();
            _lbLastRun.AutoSize = true;
            _lbLastRun.BackColor = Color.Transparent;
            _lbLastRun.Font = new Font("Arial", 11f, FontStyle.Italic);
            _lbLastRun.ForeColor = Color.FromArgb(160, 200, 160, 90);
            _lbLastRun.Visible = false;
            Controls.Add(_lbLastRun);

            _lastRunFadeTimer = new Timer();
            _lastRunFadeTimer.Interval = 30;
            _lastRunFadeTimer.Tick += LastRunFadeTick;
        }

        // fade out the last run label over time
        private void LastRunFadeTick(object sender, EventArgs e)
        {
            _lastRunAlpha -= 0.01f;
            if (_lastRunAlpha <= 0f)
            {
                _lastRunFadeTimer.Stop();
                _lbLastRun.Visible = false;
                return;
            }
            // update the label color with fading alpha
            _lbLastRun.ForeColor = Color.FromArgb((int)(160 * _lastRunAlpha), 200, 160, 90);
            Invalidate();
        }

        // show the last run score below the quit button
        private void ShowLastRun(int score, int wave)
        {
            if (score <= 0)
            {
                return;
            }
            _lbLastRun.Text = "Last run: " + score + " pts  |  Wave " + wave;
            _lbLastRun.Visible = true;
            _lastRunAlpha = 1f;
            _lbLastRun.ForeColor = Color.FromArgb(160, 200, 160, 90);

            // position below quit button
            int x = btnQuit.Left;
            int y = btnQuit.Bottom + 20;
            _lbLastRun.Location = new Point(x, y);

            // show for a few seconds then fade out
            _lastRunFadeTimer.Stop();
            _lastRunFadeTimer.Start();
        }

        // ================================================================
        //  LAYOUT - position all menu elements in the center
        // ================================================================

        private void LayoutMenu()
        {
            const int margin = 60;
            const int gapTitleSubtitle = 8;
            const int gapSubtitleButtons = 44;
            const int gapButtons = 16;

            // measure how tall the title text is
            Size titleSize;
            using (Graphics g = CreateGraphics())
            {
                titleSize = Size.Ceiling(g.MeasureString("DOOM", lbTitle.Font));
            }

            // stack everything vertically in the center of the screen
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

        // ================================================================
        //  PAINT - draw the menu background and glowing title
        // ================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            // draw background image
            if (_bgFrames.Count > 0)
            {
                Rectangle dest = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                e.Graphics.DrawImage(_bgFrames[_bgNextIndex], dest);

                // draw fading old background on top (for crossfade effect)
                if (_bgFadeAlpha < 1f)
                {
                    DrawImageAlpha(e.Graphics, _bgFrames[_bgIndex], dest, _bgFadeAlpha);
                }
            }
            else
            {
                base.OnPaint(e);
            }

            // draw the glowing "DOOM" title
            if (!_titlePos.IsEmpty)
            {
                DrawGlowTitle(e.Graphics);
            }
        }

        // draw an image with transparency (alpha = 0 to 1)
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

        // ================================================================
        //  GLOWING TITLE - the "DOOM" text with a fire-like glow
        // ================================================================

        private void DrawGlowTitle(Graphics g)
        {
            const string text = "DOOM";
            Font font = lbTitle.Font;
            int x = _titlePos.X;
            int y = _titlePos.Y;

            // dark shadow behind everything
            using (Brush shadow = new SolidBrush(Color.FromArgb(180, 20, 10, 5)))
            {
                g.DrawString(text, font, shadow, x + 6, y + 6);
            }

            // draw multiple rings of glow (bigger = more transparent)
            for (int ring = 6; ring >= 1; ring--)
            {
                int alpha = ring == 1 ? 70 : 12;
                using (Brush glow = new SolidBrush(Color.FromArgb(alpha, 255, 205, 60)))
                {
                    // draw 12 copies of the text around each ring
                    for (int i = 0; i < 12; i++)
                    {
                        double a = i / 12.0 * Math.PI * 2;
                        int dx = (int)Math.Round(Math.Cos(a) * ring * 2.5);
                        int dy = (int)Math.Round(Math.Sin(a) * ring * 2.5);
                        g.DrawString(text, font, glow, x + dx, y + dy);
                    }
                }
            }

            // bright gold core on top
            using (Brush core = new SolidBrush(Color.FromArgb(255, 255, 214, 84)))
            {
                g.DrawString(text, font, core, x, y);
            }
        }

        // ================================================================
        //  BACKGROUND CROSSFADE - switch between background images
        // ================================================================

        // timer to switch backgrounds
        private void BgSwitchTimer_Tick(object sender, EventArgs e)
        {
            // pick the next background and start fading to it
            _bgNextIndex = (_bgIndex + 1) % _bgFrames.Count;
            _bgFadeTimer.Start();
        }

        // fade from old background to new one
        private void BgFadeTimer_Tick(object sender, EventArgs e)
        {
            _bgFadeAlpha -= 0.016f / 1.5f;
            if (_bgFadeAlpha <= 0f)
            {
                // fade complete - switch to new background
                _bgFadeAlpha = 1f;
                _bgIndex = _bgNextIndex;
                _bgFadeTimer.Stop();
            }
            Invalidate();  // redraw to show the fade
        }

        // ================================================================
        //  CLEANUP - dispose timers and images when menu closes
        // ================================================================

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

        // ================================================================
        //  BUTTONS - start game or quit
        // ================================================================

        // when player clicks start, hide menu and open game
        // after game over, read final score from game and show it on menu
        private void btnStart_Click(object sender, EventArgs e)
        {
            Hide();

            int finalScore = 0;
            int finalWave = 0;

            // create and show the game form
            using (Game game = new Game())
            {
                game.ShowDialog();

                // read the final score before the game is disposed
                finalScore = game._finalScore;
                finalWave = game._finalWave;
            }

            // show menu again with the score
            Show();
            ShowLastRun(finalScore, finalWave);
        }

        private void btnQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}
