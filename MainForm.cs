using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace BloxBoost
{
    public class MainForm : Form
    {
        private readonly Color Bg = Color.FromArgb(20, 22, 28);
        private readonly Color Card = Color.FromArgb(30, 33, 41);
        private readonly Color Accent = Color.FromArgb(64, 210, 125);
        private readonly Color AccentDark = Color.FromArgb(38, 160, 92);
        private readonly Color Fg = Color.FromArgb(235, 238, 244);
        private readonly Color Muted = Color.FromArgb(150, 156, 168);

        private RadioButton _maxFps;
        private RadioButton _balanced;
        private CheckBox _closeApps;
        private Panel _boost;
        private Button _revert;
        private Label _status;
        private TextBox _log;
        private bool _boostEnabled = true;
        private bool _boostHover;

        public MainForm()
        {
            Text = "BloxBoost - Roblox FPS Booster";
            BackColor = Bg;
            ForeColor = Fg;
            Font = new Font("Segoe UI", 9.5f);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(460, 560);
            DoubleBuffered = true;

            BuildUi();
            RefreshStatus();
        }

        private void BuildUi()
        {
            // --- Hero header with gradient + logo badge ---
            var hero = new Panel { Location = new Point(0, 0), Size = new Size(460, 108) };
            hero.Paint += PaintHero;

            _status = new Label
            {
                AutoSize = false,
                Size = new Size(412, 22),
                Location = new Point(24, 116),
                ForeColor = Muted,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // --- Preset card ---
            var presetCard = MakeCard(new Point(24, 146), new Size(412, 120), "1.  CHOOSE A MODE");
            _maxFps = MakeRadio("Max FPS  -  lowest graphics, most frames", new Point(18, 46), true);
            _balanced = MakeRadio("Balanced  -  decent look, still smoother", new Point(18, 78), false);
            presetCard.Controls.Add(_maxFps);
            presetCard.Controls.Add(_balanced);

            // --- Options card ---
            var optCard = MakeCard(new Point(24, 278), new Size(412, 76), "2.  EXTRA BOOST");
            _closeApps = new CheckBox
            {
                Text = "Close heavy background apps (Chrome, Discord, etc.)",
                ForeColor = Fg,
                AutoSize = true,
                Location = new Point(18, 44),
                FlatStyle = FlatStyle.Flat
            };
            optCard.Controls.Add(_closeApps);

            // --- BOOST (custom rounded gradient button) ---
            _boost = new Panel
            {
                Size = new Size(412, 56),
                Location = new Point(24, 370),
                Cursor = Cursors.Hand
            };
            Round(_boost, 14);
            _boost.Paint += PaintBoost;
            _boost.Click += OnBoost;
            _boost.MouseEnter += delegate { _boostHover = true; _boost.Invalidate(); };
            _boost.MouseLeave += delegate { _boostHover = false; _boost.Invalidate(); };

            _revert = new Button
            {
                Text = "Revert all changes",
                Size = new Size(412, 34),
                Location = new Point(24, 434),
                FlatStyle = FlatStyle.Flat,
                BackColor = Card,
                ForeColor = Muted,
                Cursor = Cursors.Hand
            };
            _revert.FlatAppearance.BorderColor = Color.FromArgb(55, 60, 72);
            _revert.Click += OnRevert;
            Round(_revert, 10);

            _log = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = Card,
                ForeColor = Muted,
                BorderStyle = BorderStyle.None,
                Size = new Size(412, 70),
                Location = new Point(24, 478),
                Font = new Font("Consolas", 8.5f)
            };

            Controls.AddRange(new Control[] { hero, _status, presetCard, optCard, _boost, _revert, _log });
        }

        // ---- gamey painters ----

        private void PaintHero(object sender, PaintEventArgs e)
        {
            var p = (Panel)sender;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var lg = new LinearGradientBrush(p.ClientRectangle,
                Color.FromArgb(26, 46, 36), Bg, LinearGradientMode.Vertical))
                g.FillRectangle(lg, p.ClientRectangle);

            // logo badge: rounded green square with a lightning bolt
            var badge = new Rectangle(24, 28, 52, 52);
            using (var path = RoundRect(badge, 14))
            using (var lg = new LinearGradientBrush(badge, Accent, AccentDark, LinearGradientMode.ForwardDiagonal))
                g.FillPath(lg, path);

            Point[] bolt =
            {
                new Point(54, 38), new Point(40, 58), new Point(49, 58),
                new Point(46, 72), new Point(62, 50), new Point(52, 50)
            };
            using (var b = new SolidBrush(Color.FromArgb(18, 26, 20)))
                g.FillPolygon(b, bolt);

            using (var tb = new SolidBrush(Fg))
            using (var f = new Font("Segoe UI", 22f, FontStyle.Bold))
                g.DrawString("BloxBoost", f, tb, 86, 28);
            using (var sb = new SolidBrush(Muted))
            using (var f = new Font("Segoe UI", 9.5f))
                g.DrawString("Boost your FPS in Roblox  -  free, no account risk", f, sb, 88, 70);
        }

        private void PaintBoost(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var r = _boost.ClientRectangle;

            Color c1, c2, tc;
            if (!_boostEnabled) { c1 = Color.FromArgb(54, 58, 68); c2 = Color.FromArgb(44, 48, 58); tc = Color.FromArgb(120, 126, 138); }
            else if (_boostHover) { c1 = Color.FromArgb(86, 230, 145); c2 = AccentDark; tc = Color.FromArgb(14, 22, 16); }
            else { c1 = Accent; c2 = AccentDark; tc = Color.FromArgb(14, 22, 16); }

            using (var path = RoundRect(new Rectangle(0, 0, r.Width - 1, r.Height - 1), 14))
            using (var lg = new LinearGradientBrush(r, c1, c2, LinearGradientMode.Vertical))
                g.FillPath(lg, path);

            if (_boostEnabled)
            {
                Point[] bolt =
                {
                    new Point(150, 16), new Point(140, 30), new Point(147, 30),
                    new Point(144, 42), new Point(158, 26), new Point(150, 26)
                };
                using (var b = new SolidBrush(tc)) g.FillPolygon(b, bolt);
            }

            using (var tb = new SolidBrush(tc))
            using (var f = new Font("Segoe UI", 14f, FontStyle.Bold))
            using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                g.DrawString(_boostEnabled ? "BOOST" : "Roblox not found", f, tb,
                    new RectangleF(12, 0, r.Width - 12, r.Height), sf);
        }

        private Panel MakeCard(Point loc, Size size, string header)
        {
            var card = new Panel { Location = loc, Size = size, BackColor = Card };
            Round(card, 14);
            card.Controls.Add(new Label
            {
                Text = header,
                ForeColor = Accent,
                AutoSize = true,
                Location = new Point(16, 12),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            });
            return card;
        }

        private RadioButton MakeRadio(string text, Point loc, bool check)
        {
            return new RadioButton
            {
                Text = text,
                ForeColor = Fg,
                AutoSize = true,
                Location = loc,
                Checked = check,
                FlatStyle = FlatStyle.Flat
            };
        }

        // rounded helpers
        private void Round(Control c, int radius)
        {
            c.Region = new Region(RoundRect(new Rectangle(0, 0, c.Width, c.Height), radius));
        }

        private static GraphicsPath RoundRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void RefreshStatus()
        {
            int n = FlagEngine.GetRobloxVersionDirs().Count;
            if (n > 0)
            {
                _status.Text = "Roblox detected" + (FlagEngine.IsApplied() ? "  -  boost is active" : "");
                _status.ForeColor = Accent;
                _boostEnabled = true;
            }
            else
            {
                _status.Text = "Roblox not found - install/run Roblox once, then reopen";
                _status.ForeColor = Color.FromArgb(225, 175, 95);
                _boostEnabled = false;
            }
            if (_boost != null) _boost.Invalidate();
        }

        private void Log(string line) { _log.AppendText(line + Environment.NewLine); }

        private void OnBoost(object sender, EventArgs e)
        {
            if (!_boostEnabled) return;
            _log.Clear();
            try
            {
                var preset = _maxFps.Checked ? FlagEngine.MaxFps : FlagEngine.Balanced;
                int dirs = FlagEngine.ApplyPreset(preset);
                Log("Graphics flags applied to " + dirs + " Roblox version(s).");

                if (WindowsOptimizer.ApplyHighPerformancePowerPlan())
                    Log("Power plan -> High Performance.");

                long freed = WindowsOptimizer.FreeMemory();
                Log("Memory trimmed across " + freed + " processes.");

                int boosted = WindowsOptimizer.BoostRobloxPriority();
                Log(boosted > 0 ? ("Roblox priority raised (" + boosted + ").") : "Roblox not running - priority set on next launch.");

                if (_closeApps.Checked)
                {
                    int closed = WindowsOptimizer.CloseBackgroundApps();
                    Log("Closed " + closed + " background app(s).");
                }

                Log("");
                Log("Done. Restart Roblox if it was open, then press Shift+F5 in-game to see FPS.");
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
            }
            RefreshStatus();
        }

        private void OnRevert(object sender, EventArgs e)
        {
            _log.Clear();
            try
            {
                int dirs = FlagEngine.Revert();
                Log("Graphics flags removed from " + dirs + " version(s).");
                if (WindowsOptimizer.RestorePowerPlan()) Log("Power plan restored.");
                Log("All BloxBoost changes reverted.");
            }
            catch (Exception ex)
            {
                Log("Error: " + ex.Message);
            }
            RefreshStatus();
        }
    }
}
