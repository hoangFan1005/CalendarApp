using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CalendarApp.Business.DTOs;
using CalendarApp.Business.Services;
using FluentValidation;

namespace CalendarApp.UI.Forms
{
    public class AddAppointmentForm : Form
    {
        private readonly IAppointmentService _appointmentService;
        private readonly IValidator<AppointmentDto> _validator;
        private readonly Guid _userId;
        private readonly AppointmentDto? _existingAppointment;

        public DateTime PreFilledDate { get; set; }

        private TextBox txtName;
        private TextBox txtLocation;
        private DateTimePicker dtpStartTime;
        private DateTimePicker dtpEndTime;
        private CheckedListBox clbReminders;
        private Button btnSave;
        private Label lblError;

        public AddAppointmentForm(
            IAppointmentService appointmentService,
            IValidator<AppointmentDto> validator,
            Guid userId,
            DateTime preFilledDate,
            AppointmentDto? existingAppointment = null)
        {
            _appointmentService = appointmentService;
            _validator = validator;
            _userId = userId;
            PreFilledDate = preFilledDate;
            _existingAppointment = existingAppointment;

            InitializeComponent();

            if (_existingAppointment != null)
            {
                this.Text = "Chỉnh sửa lịch hẹn";
                txtName.Text = _existingAppointment.Name;
                txtLocation.Text = _existingAppointment.Location;
                dtpStartTime.Value = _existingAppointment.StartTime;
                dtpEndTime.Value = _existingAppointment.EndTime;
                
                for (int i = 0; i < clbReminders.Items.Count; i++)
                {
                    if (i == 0 && _existingAppointment.ReminderMinutes.Contains(10)) clbReminders.SetItemChecked(0, true);
                    if (i == 1 && _existingAppointment.ReminderMinutes.Contains(30)) clbReminders.SetItemChecked(1, true);
                    if (i == 2 && _existingAppointment.ReminderMinutes.Contains(60)) clbReminders.SetItemChecked(2, true);
                }
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Thêm lịch hẹn mới";
            this.Size = new Size(400, 450);
            this.StartPosition = FormStartPosition.CenterParent;

            int y = 20;

            this.Controls.Add(new Label { Text = "Tên cuộc hẹn:", Location = new Point(20, y), AutoSize = true });
            txtName = new TextBox { Location = new Point(120, y), Width = 230 };
            this.Controls.Add(txtName);
            y += 40;

            this.Controls.Add(new Label { Text = "Địa điểm:", Location = new Point(20, y), AutoSize = true });
            txtLocation = new TextBox { Location = new Point(120, y), Width = 230 };
            this.Controls.Add(txtLocation);
            y += 40;

            this.Controls.Add(new Label { Text = "Bắt đầu:", Location = new Point(20, y), AutoSize = true });
            dtpStartTime = new DateTimePicker { Location = new Point(120, y), Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 230 };
            dtpStartTime.Value = PreFilledDate;
            this.Controls.Add(dtpStartTime);
            y += 40;

            this.Controls.Add(new Label { Text = "Kết thúc:", Location = new Point(20, y), AutoSize = true });
            dtpEndTime = new DateTimePicker { Location = new Point(120, y), Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 230 };
            dtpEndTime.Value = PreFilledDate.AddHours(1);
            this.Controls.Add(dtpEndTime);
            y += 40;

            this.Controls.Add(new Label { Text = "Nhắc nhở:", Location = new Point(20, y), AutoSize = true });
            clbReminders = new CheckedListBox { Location = new Point(120, y), Width = 230, Height = 80 };
            clbReminders.Items.Add("Trước 10 phút");
            clbReminders.Items.Add("Trước 30 phút");
            clbReminders.Items.Add("Trước 1 giờ");
            this.Controls.Add(clbReminders);
            y += 100;

            lblError = new Label { Location = new Point(20, y), Width = 330, ForeColor = Color.Red, AutoSize = false, Height = 40 };
            this.Controls.Add(lblError);
            y += 50;

            btnSave = new Button { Text = "Lưu lại", Location = new Point(150, y), Size = new Size(100, 35), BackColor = Color.DodgerBlue, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            btnSave.Click += async (s, e) => 
            {
                try 
                {
                    await OnSaveClicked();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString(), "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            this.Controls.Add(btnSave);
        }

        private async Task OnSaveClicked()
        {
            lblError.Text = string.Empty;

            var dto = new AppointmentDto
            {
                Id = _existingAppointment?.Id ?? Guid.Empty,
                Name = txtName.Text,
                Location = txtLocation.Text,
                StartTime = dtpStartTime.Value,
                EndTime = dtpEndTime.Value
            };

            foreach (int index in clbReminders.CheckedIndices)
            {
                if (index == 0) dto.ReminderMinutes.Add(10);
                if (index == 1) dto.ReminderMinutes.Add(30);
                if (index == 2) dto.ReminderMinutes.Add(60);
            }

            // 1. Validation
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                lblError.Text = string.Join("\n", validationResult.Errors.Select(e => e.ErrorMessage));
                return;
            }

            // 2. Group Meeting Integration
            var matchingMeeting = await _appointmentService.FindMatchingGroupMeetingAsync(dto.Name, dto.Duration);
            if (matchingMeeting != null)
            {
                using var groupDialog = new GroupMeetingPromptDialog(matchingMeeting.MeetingName);
                if (groupDialog.ShowDialog() == DialogResult.OK && groupDialog.JoinGroup)
                {
                    await _appointmentService.JoinGroupMeetingAsync(_userId, matchingMeeting.Id);
                    MessageBox.Show("Tham gia Group Meeting thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    return;
                }
            }

            // 3. Conflict Resolution
            var excludeId = _existingAppointment?.Id;
            var conflictResult = await _appointmentService.CheckConflictAsync(_userId, dto.StartTime, dto.EndTime, excludeId);
            if (conflictResult.HasConflict)
            {
                var conflictingAppt = conflictResult.ConflictingAppointments.First();
                using var conflictDialog = new ConflictWarningDialog($"Bạn đã có lịch: {conflictingAppt.Name} vào thời gian này.");
                if (conflictDialog.ShowDialog() == DialogResult.OK)
                {
                    if (conflictDialog.SelectedAction == ConflictWarningDialog.ConflictAction.ChooseAvailableTime)
                    {
                        return; // Stay on form to choose another time
                    }
                    else if (conflictDialog.SelectedAction == ConflictWarningDialog.ConflictAction.Replace)
                    {
                        await _appointmentService.ReplaceAppointmentAsync(_userId, conflictingAppt.Id, dto);
                        MessageBox.Show("Đã ghi đè lịch thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.DialogResult = DialogResult.OK;
                        return;
                    }
                }
                else
                {
                    return; // Cancelled dialog
                }
            }

            // 4. Save normally
            if (_existingAppointment != null)
            {
                await _appointmentService.UpdateAppointmentAsync(_userId, dto);
                MessageBox.Show("Cập nhật lịch hẹn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                await _appointmentService.CreateAppointmentAsync(_userId, dto);
                MessageBox.Show("Thêm lịch hẹn thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            this.DialogResult = DialogResult.OK;
        }
    }
}
