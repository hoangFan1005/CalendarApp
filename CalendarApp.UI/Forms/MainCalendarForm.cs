using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CalendarApp.Business.DTOs;
using CalendarApp.Business.Services;
using CalendarApp.Data.Repositories;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarApp.UI.Forms
{
    public class MainCalendarForm : Form
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Guid _currentUserId;

        private DateTime _selectedDate;
        private int _miniCalYear;
        private int _miniCalMonth;

        // Google Colors
        private static readonly Color GoogleBlue = Color.FromArgb(26, 115, 232);
        private static readonly Color GoogleGray = Color.FromArgb(60, 64, 67);
        private static readonly Color GoogleLightGray = Color.FromArgb(218, 220, 224);
        private static readonly Color GoogleBg = Color.FromArgb(248, 249, 250);
        private static readonly Color GoogleHoverBg = Color.FromArgb(241, 243, 244);
        private static readonly Color GoogleRed = Color.FromArgb(217, 48, 37);
        private static readonly Color GoogleEventBlue = Color.FromArgb(3, 155, 229);
        private static readonly Color GoogleEventHover = Color.FromArgb(2, 106, 180);

        // 24 distinct event colors
        private static readonly Color[] EventColors = {
            Color.FromArgb(3, 155, 229),   Color.FromArgb(142, 36, 170),
            Color.FromArgb(230, 124, 115),  Color.FromArgb(51, 182, 121),
            Color.FromArgb(246, 191, 38),   Color.FromArgb(121, 134, 203),
            Color.FromArgb(97, 97, 97),     Color.FromArgb(213, 0, 0),
            Color.FromArgb(3, 137, 129),    Color.FromArgb(244, 81, 30),
            Color.FromArgb(63, 81, 181),    Color.FromArgb(0, 150, 136),
            Color.FromArgb(156, 39, 176),   Color.FromArgb(33, 150, 243),
            Color.FromArgb(255, 152, 0),    Color.FromArgb(76, 175, 80),
            Color.FromArgb(233, 30, 99),    Color.FromArgb(0, 188, 212),
            Color.FromArgb(139, 195, 74),   Color.FromArgb(255, 87, 34),
            Color.FromArgb(103, 58, 183),   Color.FromArgb(0, 137, 123),
            Color.FromArgb(194, 24, 91),    Color.FromArgb(48, 63, 159)
        };

        private static Color DarkenColor(Color c, double factor)
        {
            return Color.FromArgb(c.A, (int)(c.R * (1 - factor)), (int)(c.G * (1 - factor)), (int)(c.B * (1 - factor)));
        }

        // UI Elements
        private Panel leftPanel = null!;
        private Panel rightPanel = null!;
        private TableLayoutPanel miniCalGrid = null!;
        private Label lblMiniCalMonth = null!;
        private Panel dayViewContainer = null!;
        private Label lblDayHeader = null!;

        public MainCalendarForm(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _currentUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            _selectedDate = DateTime.Today;
            _miniCalYear = _selectedDate.Year;
            _miniCalMonth = _selectedDate.Month;

            InitializeComponent();
            this.Load += async (s, e) =>
            {
                RenderMiniCalendar();
                await RenderDayViewAsync();
            };
        }

        private void InitializeComponent()
        {
            this.Text = "Calendar";
            this.MinimumSize = new Size(1000, 700);
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.DoubleBuffered = true;
            this.Font = new Font("Segoe UI", 9);

            // ═══════════════════════════════════════
            // LEFT PANEL (Mini Calendar)
            // ═══════════════════════════════════════
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = 280,
                BackColor = Color.White,
                Padding = new Padding(16, 16, 16, 16)
            };
            leftPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(GoogleLightGray, 1);
                e.Graphics.DrawLine(pen, leftPanel.Width - 1, 0, leftPanel.Width - 1, leftPanel.Height);
            };
            this.Controls.Add(leftPanel);

            // Mini Calendar Header
            var miniCalHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.White
            };
            leftPanel.Controls.Add(miniCalHeader);

            lblMiniCalMonth = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = GoogleGray,
                Location = new Point(4, 8),
                AutoSize = true
            };
            miniCalHeader.Controls.Add(lblMiniCalMonth);

            var btnMiniPrev = new Label
            {
                Text = "‹",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = GoogleGray,
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(miniCalHeader.Width - 64, 6)
            };
            btnMiniPrev.MouseEnter += (s, e) => btnMiniPrev.BackColor = GoogleHoverBg;
            btnMiniPrev.MouseLeave += (s, e) => btnMiniPrev.BackColor = Color.Transparent;
            btnMiniPrev.Click += (s, e) =>
            {
                _miniCalMonth--;
                if (_miniCalMonth < 1) { _miniCalMonth = 12; _miniCalYear--; }
                RenderMiniCalendar();
            };
            miniCalHeader.Controls.Add(btnMiniPrev);

            var btnMiniNext = new Label
            {
                Text = "›",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = GoogleGray,
                Size = new Size(28, 28),
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(miniCalHeader.Width - 34, 6)
            };
            btnMiniNext.MouseEnter += (s, e) => btnMiniNext.BackColor = GoogleHoverBg;
            btnMiniNext.MouseLeave += (s, e) => btnMiniNext.BackColor = Color.Transparent;
            btnMiniNext.Click += (s, e) =>
            {
                _miniCalMonth++;
                if (_miniCalMonth > 12) { _miniCalMonth = 1; _miniCalYear++; }
                RenderMiniCalendar();
            };
            miniCalHeader.Controls.Add(btnMiniNext);

            // Resize arrows position
            miniCalHeader.Resize += (s, e) =>
            {
                btnMiniPrev.Location = new Point(miniCalHeader.Width - 64, 6);
                btnMiniNext.Location = new Point(miniCalHeader.Width - 34, 6);
            };

            // Mini Calendar Grid placeholder
            miniCalGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 200,
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = new Padding(0)
            };
            leftPanel.Controls.Add(miniCalGrid);
            miniCalGrid.BringToFront();

            // ═══════════════════════════════════════
            // RIGHT PANEL (Day View)
            // ═══════════════════════════════════════
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0)
            };
            this.Controls.Add(rightPanel);
            rightPanel.BringToFront();

            // Day View Header
            var dayHeaderPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White
            };
            dayHeaderPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(GoogleLightGray, 1);
                e.Graphics.DrawLine(pen, 0, dayHeaderPanel.Height - 1, dayHeaderPanel.Width, dayHeaderPanel.Height - 1);
            };
            rightPanel.Controls.Add(dayHeaderPanel);

            lblDayHeader = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 18, FontStyle.Regular),
                ForeColor = GoogleGray,
                Location = new Point(20, 14),
                AutoSize = true
            };
            dayHeaderPanel.Controls.Add(lblDayHeader);

            // Day View Scrollable Container
            dayViewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };
            rightPanel.Controls.Add(dayViewContainer);
            dayViewContainer.BringToFront();
        }

        // ─────────────────────────────────────────
        // MINI CALENDAR
        // ─────────────────────────────────────────
        private void RenderMiniCalendar()
        {
            lblMiniCalMonth.Text = $"{GetMonthName(_miniCalMonth)} {_miniCalYear}";
            miniCalGrid.Controls.Clear();
            miniCalGrid.ColumnStyles.Clear();
            miniCalGrid.RowStyles.Clear();

            miniCalGrid.ColumnCount = 7;
            int daysInMonth = DateTime.DaysInMonth(_miniCalYear, _miniCalMonth);
            var firstDay = new DateTime(_miniCalYear, _miniCalMonth, 1);
            int startDow = (int)firstDay.DayOfWeek;
            int totalCells = startDow + daysInMonth;
            int rows = (int)Math.Ceiling(totalCells / 7.0);
            miniCalGrid.RowCount = rows + 1;
            miniCalGrid.Height = (rows + 1) * 30 + 4;

            for (int c = 0; c < 7; c++)
                miniCalGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7f));
            for (int r = 0; r <= rows; r++)
                miniCalGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // Weekday header
            string[] days = { "S", "M", "T", "W", "T", "F", "S" };
            for (int i = 0; i < 7; i++)
            {
                miniCalGrid.Controls.Add(new Label
                {
                    Text = days[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 8, FontStyle.Bold),
                    ForeColor = (i == 0 || i == 6) ? GoogleRed : Color.FromArgb(112, 117, 122),
                    Margin = new Padding(0)
                }, i, 0);
            }

            // Day cells
            int prevMonthYear = _miniCalYear;
            int prevMonth = _miniCalMonth - 1;
            if (prevMonth < 1) { prevMonth = 12; prevMonthYear--; }
            int prevMonthDays = DateTime.DaysInMonth(prevMonthYear, prevMonth);

            int nextMonthDay = 1;
            int dayNum = 1;
            var today = DateTime.Today;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    int cellIndex = row * 7 + col;
                    Label lbl;

                    if (cellIndex < startDow)
                    {
                        // Previous month
                        int d = prevMonthDays - startDow + cellIndex + 1;
                        lbl = CreateMiniCalDayLabel(d.ToString(), Color.FromArgb(180, 180, 180), false, false);
                    }
                    else if (dayNum > daysInMonth)
                    {
                        // Next month
                        lbl = CreateMiniCalDayLabel(nextMonthDay.ToString(), Color.FromArgb(180, 180, 180), false, false);
                        nextMonthDay++;
                    }
                    else
                    {
                        int currentDay = dayNum;
                        bool isToday = (today.Year == _miniCalYear && today.Month == _miniCalMonth && today.Day == currentDay);
                        bool isSelected = (_selectedDate.Year == _miniCalYear && _selectedDate.Month == _miniCalMonth && _selectedDate.Day == currentDay);
                        bool isWeekend = (col == 0 || col == 6);

                        Color fc = isWeekend ? GoogleRed : GoogleGray;
                        lbl = CreateMiniCalDayLabel(currentDay.ToString(), fc, isToday, isSelected);

                        lbl.Cursor = Cursors.Hand;
                        lbl.Click += async (s, e) =>
                        {
                            _selectedDate = new DateTime(_miniCalYear, _miniCalMonth, currentDay);
                            RenderMiniCalendar();
                            await RenderDayViewAsync();
                        };
                        lbl.MouseEnter += (s, e) => { if (!isToday && !isSelected) lbl.BackColor = GoogleHoverBg; };
                        lbl.MouseLeave += (s, e) => { if (!isToday && !isSelected) lbl.BackColor = Color.Transparent; };
                        dayNum++;
                    }

                    miniCalGrid.Controls.Add(lbl, col, row + 1);
                }
            }
        }

        private Label CreateMiniCalDayLabel(string text, Color foreColor, bool isToday, bool isSelected)
        {
            var lbl = new Label
            {
                Text = text,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9, (isToday || isSelected) ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = isToday ? Color.White : (isSelected ? GoogleBlue : foreColor),
                BackColor = Color.Transparent,
                Margin = new Padding(2)
            };

            if (isToday)
            {
                lbl.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    int d = Math.Min(lbl.Width, lbl.Height) - 4;
                    int x = (lbl.Width - d) / 2;
                    int y = (lbl.Height - d) / 2;
                    using var brush = new SolidBrush(GoogleBlue);
                    pe.Graphics.FillEllipse(brush, x, y, d, d);
                    TextRenderer.DrawText(pe.Graphics, lbl.Text, lbl.Font, lbl.ClientRectangle, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
            }
            else if (isSelected)
            {
                lbl.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    int d = Math.Min(lbl.Width, lbl.Height) - 4;
                    int x = (lbl.Width - d) / 2;
                    int y = (lbl.Height - d) / 2;
                    using var brush = new SolidBrush(Color.FromArgb(210, 227, 252));
                    pe.Graphics.FillEllipse(brush, x, y, d, d);
                    TextRenderer.DrawText(pe.Graphics, lbl.Text, lbl.Font, lbl.ClientRectangle, GoogleBlue,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
            }

            return lbl;
        }

        // ─────────────────────────────────────────
        // DAY VIEW (Hourly Timeline)
        // ─────────────────────────────────────────
        private async System.Threading.Tasks.Task RenderDayViewAsync()
        {
            dayViewContainer.Controls.Clear();
            string dayName = _selectedDate.ToString("dddd");
            lblDayHeader.Text = $"{dayName}, ngày {_selectedDate.Day} tháng {_selectedDate.Month}, {_selectedDate.Year}";

            using var scope = _serviceProvider.CreateScope();
            var appointmentRepo = scope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
            var mapper = scope.ServiceProvider.GetRequiredService<AutoMapper.IMapper>();
            var allAppts = await appointmentRepo.GetByUserIdAsync(_currentUserId);
            var dayAppts = allAppts
                .Where(a => a.StartTime.Date == _selectedDate.Date)
                .OrderBy(a => a.StartTime)
                .ToList();

            // Inner panel with fixed height for scrolling
            int hourHeight = 60;
            int totalHeight = 24 * hourHeight + 40;
            var innerPanel = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(dayViewContainer.ClientSize.Width - 20, totalHeight),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            dayViewContainer.Controls.Add(innerPanel);

            int timeColWidth = 70;

            // Draw hour rows
            for (int h = 0; h < 24; h++)
            {
                int y = h * hourHeight + 20;

                // Time label
                var lblTime = new Label
                {
                    Text = $"{h:D2}:00",
                    Location = new Point(4, y - 8),
                    Size = new Size(timeColWidth - 8, 20),
                    Font = new Font("Segoe UI", 8, FontStyle.Regular),
                    ForeColor = Color.FromArgb(112, 117, 122),
                    TextAlign = ContentAlignment.TopRight
                };
                innerPanel.Controls.Add(lblTime);

                // Hour line
                var line = new Label
                {
                    Location = new Point(timeColWidth, y),
                    Size = new Size(innerPanel.Width - timeColWidth - 10, 1),
                    BackColor = GoogleLightGray,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                innerPanel.Controls.Add(line);

                // Clickable area for adding appointment
                int capturedHour = h;
                var clickArea = new Panel
                {
                    Location = new Point(timeColWidth, y + 1),
                    Size = new Size(innerPanel.Width - timeColWidth - 10, hourHeight - 1),
                    BackColor = Color.Transparent,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Cursor = Cursors.Default
                };
                clickArea.MouseEnter += (s, e) => { clickArea.BackColor = Color.FromArgb(232, 240, 254); };
                clickArea.MouseLeave += (s, e) => { clickArea.BackColor = Color.Transparent; };
                clickArea.DoubleClick += async (s, e) =>
                {
                    var date = _selectedDate.Date.AddHours(capturedHour);
                    using var addScope = _serviceProvider.CreateScope();
                    var svc = addScope.ServiceProvider.GetRequiredService<IAppointmentService>();
                    var val = addScope.ServiceProvider.GetRequiredService<IValidator<AppointmentDto>>();
                    using var form = new AddAppointmentForm(svc, val, _currentUserId, date);
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        RenderMiniCalendar();
                        await RenderDayViewAsync();
                    }
                };
                innerPanel.Controls.Add(clickArea);
            }

            // Current time indicator (red line)
            if (_selectedDate.Date == DateTime.Today)
            {
                var now = DateTime.Now;
                double minutesSinceMidnight = now.Hour * 60 + now.Minute;
                int redLineY = (int)(minutesSinceMidnight / 60.0 * hourHeight) + 20;

                var redCircle = new Panel
                {
                    Size = new Size(12, 12),
                    Location = new Point(timeColWidth - 6, redLineY - 6),
                    BackColor = GoogleRed
                };
                redCircle.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(GoogleRed);
                    pe.Graphics.FillEllipse(brush, 0, 0, 11, 11);
                };
                innerPanel.Controls.Add(redCircle);
                redCircle.BringToFront();

                var redLine = new Label
                {
                    Location = new Point(timeColWidth, redLineY),
                    Size = new Size(innerPanel.Width - timeColWidth - 10, 2),
                    BackColor = GoogleRed,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
                };
                innerPanel.Controls.Add(redLine);
                redLine.BringToFront();
            }

            // Render appointments
            int colorIdx = 0;
            foreach (var appt in dayAppts)
            {
                double startMin = appt.StartTime.Hour * 60 + appt.StartTime.Minute;
                double endMin = appt.EndTime.Hour * 60 + appt.EndTime.Minute;
                if (endMin <= startMin) endMin = startMin + 30;
                int topY = (int)(startMin / 60.0 * hourHeight) + 20;
                int chipH = Math.Max((int)((endMin - startMin) / 60.0 * hourHeight), 26);

                bool done = appt.IsCompleted;
                var cColor = done ? Color.FromArgb(189,189,189) : EventColors[colorIdx % EventColors.Length];
                var cHover = done ? Color.FromArgb(158,158,158) : DarkenColor(cColor, 0.15);
                colorIdx++;

                string rem = "";
                if (appt.Reminders != null && appt.Reminders.Any())
                    rem = $"🔔{appt.Reminders.Min(r => r.MinutesBefore)}p";

                var chip = new Panel {
                    Location = new Point(timeColWidth + 4, topY),
                    Size = new Size(innerPanel.Width - timeColWidth - 20, chipH),
                    BackColor = cColor,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Cursor = Cursors.Hand
                };
                chip.Paint += (s, pe) => {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var br = new SolidBrush(chip.BackColor);
                    using var pa = RoundedRect(new Rectangle(0,0,chip.Width,chip.Height), 4);
                    pe.Graphics.FillPath(br, pa);
                };

                var ts = done ? FontStyle.Strikeout : FontStyle.Regular;
                var tc = done ? Color.FromArgb(220,220,220) : Color.White;
                var dc = done ? Color.FromArgb(200,200,200) : Color.FromArgb(230,240,255);
                int x = 8, ly = Math.Max((chipH - 16) / 2, 2);

                chip.Controls.Add(new Label { Text=$"{appt.StartTime:HH:mm}-{appt.EndTime:HH:mm}", Font=new Font("Segoe UI",8,FontStyle.Bold|ts), ForeColor=tc, Location=new Point(x,ly), AutoSize=true, BackColor=Color.Transparent, Cursor=Cursors.Hand });
                x += ((Label)chip.Controls[chip.Controls.Count-1]).PreferredWidth + 6;
                chip.Controls.Add(new Label { Text=done?$"✔{appt.Name}":appt.Name, Font=new Font("Segoe UI",9,FontStyle.Bold|ts), ForeColor=tc, Location=new Point(x,ly-1), AutoSize=true, BackColor=Color.Transparent, Cursor=Cursors.Hand });
                x += ((Label)chip.Controls[chip.Controls.Count-1]).PreferredWidth + 6;
                if (!string.IsNullOrEmpty(appt.Location)) {
                    chip.Controls.Add(new Label { Text=$"📍{appt.Location}", Font=new Font("Segoe UI",8,ts), ForeColor=dc, Location=new Point(x,ly), AutoSize=true, BackColor=Color.Transparent, Cursor=Cursors.Hand });
                    x += ((Label)chip.Controls[chip.Controls.Count-1]).PreferredWidth + 6;
                }
                if (!string.IsNullOrEmpty(rem)) {
                    chip.Controls.Add(new Label { Text=rem, Font=new Font("Segoe UI",8,ts), ForeColor=dc, Location=new Point(x,ly), AutoSize=true, BackColor=Color.Transparent, Cursor=Cursors.Hand });
                    x += ((Label)chip.Controls[chip.Controls.Count-1]).PreferredWidth + 6;
                }

                var aid = appt.Id;
                if (!done) {
                    var bc = new Label { Text="✔Xong", Font=new Font("Segoe UI",7), ForeColor=Color.FromArgb(200,230,201), BackColor=Color.Transparent, AutoSize=true, Location=new Point(x,ly+1), Cursor=Cursors.Hand };
                    bc.Click += async (s,e) => { if (MessageBox.Show("Hoàn thành?","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes) { using var sc=_serviceProvider.CreateScope(); await sc.ServiceProvider.GetRequiredService<IAppointmentService>().CompleteAppointmentAsync(aid); RenderMiniCalendar(); await RenderDayViewAsync(); } };
                    chip.Controls.Add(bc); x += bc.PreferredWidth + 4;
                }
                var bd = new Label { Text="✕Xóa", Font=new Font("Segoe UI",7), ForeColor=Color.FromArgb(255,200,200), BackColor=Color.Transparent, AutoSize=true, Location=new Point(x,ly+1), Cursor=Cursors.Hand };
                bd.Click += async (s,e) => { if (MessageBox.Show("Xóa cuộc hẹn?","Xác nhận",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)==DialogResult.Yes) { using var sc=_serviceProvider.CreateScope(); await sc.ServiceProvider.GetRequiredService<IAppointmentService>().DeleteAppointmentAsync(aid); RenderMiniCalendar(); await RenderDayViewAsync(); } };
                chip.Controls.Add(bd);

                var dto = mapper.Map<AppointmentDto>(appt);
                foreach (Control c in chip.Controls) {
                    c.MouseEnter += (s,e) => { chip.BackColor=cHover; chip.Invalidate(); };
                    c.MouseLeave += (s,e) => { chip.BackColor=cColor; chip.Invalidate(); };
                    c.DoubleClick += async (s,e) => { using var sc=_serviceProvider.CreateScope(); var sv=sc.ServiceProvider.GetRequiredService<IAppointmentService>(); var vl=sc.ServiceProvider.GetRequiredService<IValidator<AppointmentDto>>(); using var f=new AddAppointmentForm(sv,vl,_currentUserId,dto.StartTime,dto); if(f.ShowDialog()==DialogResult.OK){RenderMiniCalendar();await RenderDayViewAsync();} };
                }
                chip.MouseEnter += (s,e) => { chip.BackColor=cHover; chip.Invalidate(); };
                chip.MouseLeave += (s,e) => { chip.BackColor=cColor; chip.Invalidate(); };
                chip.DoubleClick += async (s,e) => { using var sc=_serviceProvider.CreateScope(); var sv=sc.ServiceProvider.GetRequiredService<IAppointmentService>(); var vl=sc.ServiceProvider.GetRequiredService<IValidator<AppointmentDto>>(); using var f=new AddAppointmentForm(sv,vl,_currentUserId,dto.StartTime,dto); if(f.ShowDialog()==DialogResult.OK){RenderMiniCalendar();await RenderDayViewAsync();} };

                innerPanel.Controls.Add(chip);
                chip.BringToFront();
            }



            // Scroll to current hour if today
            if (_selectedDate.Date == DateTime.Today)
            {
                int scrollTo = Math.Max(0, (DateTime.Now.Hour - 1) * hourHeight);
                dayViewContainer.AutoScrollPosition = new Point(0, scrollTo);
            }
            else
            {
                dayViewContainer.AutoScrollPosition = new Point(0, 8 * hourHeight); // default scroll to 8am
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private string GetMonthName(int month)
        {
            string[] months = { "", "January", "February", "March", "April", "May", "June",
                               "July", "August", "September", "October", "November", "December" };
            return months[month];
        }
    }
}
