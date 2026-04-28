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
            foreach (var appt in dayAppts)
            {
                double startMin = appt.StartTime.Hour * 60 + appt.StartTime.Minute;
                double endMin = appt.EndTime.Hour * 60 + appt.EndTime.Minute;
                if (endMin <= startMin) endMin = startMin + 30; // min 30 min display

                int topY = (int)(startMin / 60.0 * hourHeight) + 20;
                int chipHeight = Math.Max((int)((endMin - startMin) / 60.0 * hourHeight), 50);

                string reminderText = "";
                if (appt.Reminders != null && appt.Reminders.Any())
                {
                    var min = appt.Reminders.Min(r => r.MinutesBefore);
                    reminderText = $"  🔔 Nhắc trước {min} phút";
                }

                bool completed = appt.IsCompleted;
                var chipBg = completed ? Color.FromArgb(189, 189, 189) : GoogleEventBlue;
                var chipHoverBg = completed ? Color.FromArgb(158, 158, 158) : GoogleEventHover;

                var chipPanel = new Panel
                {
                    Location = new Point(timeColWidth + 4, topY),
                    Size = new Size(innerPanel.Width - timeColWidth - 20, chipHeight),
                    BackColor = chipBg,
                    Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                    Cursor = Cursors.Hand,
                    Padding = new Padding(8, 4, 8, 4)
                };

                // Rounded corners
                chipPanel.Paint += (s, pe) =>
                {
                    pe.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var brush = new SolidBrush(chipPanel.BackColor);
                    var rect = new Rectangle(0, 0, chipPanel.Width, chipPanel.Height);
                    using var path = RoundedRect(rect, 6);
                    pe.Graphics.FillPath(brush, path);
                };

                var nameStyle = completed ? FontStyle.Strikeout | FontStyle.Bold : FontStyle.Bold;
                var lblApptName = new Label
                {
                    Text = completed ? $"✔ {appt.Name}" : appt.Name,
                    Font = new Font("Segoe UI", 10, nameStyle),
                    ForeColor = completed ? Color.FromArgb(220, 220, 220) : Color.White,
                    Location = new Point(8, 4),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                chipPanel.Controls.Add(lblApptName);

                var lblApptTime = new Label
                {
                    Text = $"{appt.StartTime:HH:mm} - {appt.EndTime:HH:mm}" +
                           (string.IsNullOrEmpty(appt.Location) ? "" : $"  📍 {appt.Location}") +
                           reminderText,
                    Font = new Font("Segoe UI", 8, completed ? FontStyle.Strikeout : FontStyle.Regular),
                    ForeColor = completed ? Color.FromArgb(210, 210, 210) : Color.FromArgb(220, 230, 255),
                    Location = new Point(8, 24),
                    AutoSize = true,
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };
                chipPanel.Controls.Add(lblApptTime);

                // ── Action Buttons (inline, subtle) ──
                var capturedApptId = appt.Id;
                int btnY = 44;

                if (!completed)
                {
                    var btnComplete = new Label
                    {
                        Text = "✔ Xong",
                        Font = new Font("Segoe UI", 7, FontStyle.Regular),
                        ForeColor = Color.FromArgb(200, 230, 201),
                        BackColor = Color.FromArgb(2, 136, 209),
                        AutoSize = false,
                        Size = new Size(50, 16),
                        TextAlign = ContentAlignment.MiddleCenter,
                        Cursor = Cursors.Hand,
                        Location = new Point(8, btnY)
                    };
                    btnComplete.MouseEnter += (s, e) => btnComplete.BackColor = Color.FromArgb(1, 119, 189);
                    btnComplete.MouseLeave += (s, e) => btnComplete.BackColor = Color.FromArgb(2, 136, 209);
                    btnComplete.Click += async (s, e) =>
                    {
                        if (MessageBox.Show("Đánh dấu cuộc hẹn đã hoàn thành?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            using var cScope = _serviceProvider.CreateScope();
                            var svc = cScope.ServiceProvider.GetRequiredService<IAppointmentService>();
                            await svc.CompleteAppointmentAsync(capturedApptId);
                            RenderMiniCalendar();
                            await RenderDayViewAsync();
                        }
                    };
                    chipPanel.Controls.Add(btnComplete);
                    btnComplete.BringToFront();
                }

                var btnDelete = new Label
                {
                    Text = "✕ Xóa",
                    Font = new Font("Segoe UI", 7, FontStyle.Regular),
                    ForeColor = Color.FromArgb(255, 205, 210),
                    BackColor = completed ? Color.FromArgb(158, 158, 158) : Color.FromArgb(2, 136, 209),
                    AutoSize = false,
                    Size = new Size(46, 16),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Cursor = Cursors.Hand,
                    Location = new Point(completed ? 8 : 62, btnY)
                };
                var delNormal = btnDelete.BackColor;
                btnDelete.MouseEnter += (s, e) => btnDelete.BackColor = Color.FromArgb(1, 119, 189);
                btnDelete.MouseLeave += (s, e) => btnDelete.BackColor = delNormal;
                btnDelete.Click += async (s, e) =>
                {
                    if (MessageBox.Show("Bạn có chắc muốn xóa cuộc hẹn này?", "Xác nhận xóa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    {
                        using var dScope = _serviceProvider.CreateScope();
                        var svc = dScope.ServiceProvider.GetRequiredService<IAppointmentService>();
                        await svc.DeleteAppointmentAsync(capturedApptId);
                        RenderMiniCalendar();
                        await RenderDayViewAsync();
                    }
                };
                chipPanel.Controls.Add(btnDelete);
                btnDelete.BringToFront();

                // Hover
                void SetHover(Control ctrl)
                {
                    ctrl.MouseEnter += (s, e) => { chipPanel.BackColor = chipHoverBg; chipPanel.Invalidate(); };
                    ctrl.MouseLeave += (s, e) => { chipPanel.BackColor = chipBg; chipPanel.Invalidate(); };
                }
                SetHover(chipPanel);
                SetHover(lblApptName);
                SetHover(lblApptTime);

                // DoubleClick to edit
                var apptDto = mapper.Map<AppointmentDto>(appt);
                void SetEdit(Control ctrl)
                {
                    ctrl.DoubleClick += async (s, e) =>
                    {
                        using var editScope = _serviceProvider.CreateScope();
                        var svc = editScope.ServiceProvider.GetRequiredService<IAppointmentService>();
                        var val = editScope.ServiceProvider.GetRequiredService<IValidator<AppointmentDto>>();
                        using var form = new AddAppointmentForm(svc, val, _currentUserId, apptDto.StartTime, apptDto);
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            RenderMiniCalendar();
                            await RenderDayViewAsync();
                        }
                    };
                }
                SetEdit(chipPanel);
                SetEdit(lblApptName);
                SetEdit(lblApptTime);

                innerPanel.Controls.Add(chipPanel);
                chipPanel.BringToFront();
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
