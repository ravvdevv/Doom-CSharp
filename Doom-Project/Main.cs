using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Doom_Project
{
    // ================================================================
    //  MAIN MENU - the title screen when you first open the game
    //  by raven
    // tribute to rene batterbonia mr.mvp 67
    //  shows animated background, title, start/quit buttons
    //  after game over, shows your last score briefly
    // ================================================================

    public partial class Main : Form
    {
        // background animation - cycles through bg0.png, bg1.png, etc
        private readonly List<Image> _bgFrames = new List<Image>();
        private int _bgIndex;           // which background is currently showing
        private readonly Timer _bgSwitchTimer = new Timer();  // when to switch backgrounds

        // last run score - shows briefly after game over
        private Label _lbLastRun;

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
        }

        // hide the last run label
        private void HideLastRun(object sender, EventArgs e)
        {
            Timer timer = (Timer)sender;
            timer.Stop();
            timer.Dispose();
            _lbLastRun.Visible = false;
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

            // position below quit button
            int x = btnQuit.Left;
            int y = btnQuit.Bottom + 20;
            _lbLastRun.Location = new Point(x, y);

            // hide after 3 seconds
            Timer hideTimer = new Timer();
            hideTimer.Interval = 3000;
            hideTimer.Tick += HideLastRun;
            hideTimer.Start();
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

            // stack everything vertically in the center of the screen
            int titleH = lbTitle.Height;
            int subtitleH = lbSubtitle.Height;
            int btnH = btnStart.Height;
            int totalH = titleH + gapTitleSubtitle + subtitleH + gapSubtitleButtons + btnH + gapButtons + btnH;
            int top = ClientSize.Height / 2 - totalH / 2;

            int x = margin;
            lbTitle.Location = new Point(x, top);
            lbSubtitle.Location = new Point(lbTitle.Left + lbTitle.Width / 2 - lbSubtitle.Width / 2, top + titleH + gapTitleSubtitle);
            btnStart.Location = new Point(x, top + titleH + gapTitleSubtitle + subtitleH + gapSubtitleButtons);
            btnQuit.Location = new Point(x, btnStart.Bottom + gapButtons);
        }

        private void Main_Resize(object sender, EventArgs e)
        {
            LayoutMenu();
        }

        // ================================================================
        //  PAINT - draw the menu background
        // ================================================================

        protected override void OnPaint(PaintEventArgs e)
        {
            // draw background image
            if (_bgFrames.Count > 0)
            {
                Rectangle dest = new Rectangle(0, 0, ClientSize.Width, ClientSize.Height);
                e.Graphics.DrawImage(_bgFrames[_bgIndex], dest);
            }
            else
            {
                base.OnPaint(e);
            }

        }

        // ================================================================
        //  BACKGROUND - switch between background images
        // ================================================================

        // timer to switch backgrounds
        private void BgSwitchTimer_Tick(object sender, EventArgs e)
        {
            // switch to next background instantly
            _bgIndex = (_bgIndex + 1) % _bgFrames.Count;
            Invalidate();  // redraw to show new background
        }

        // ================================================================
        //  CLEANUP - dispose timers and images when menu closes
        // ================================================================

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _bgSwitchTimer.Stop();
            _bgSwitchTimer.Dispose();
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
