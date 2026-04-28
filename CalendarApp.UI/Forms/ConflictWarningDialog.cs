using System;
using System.Drawing;
using System.Windows.Forms;

namespace CalendarApp.UI.Forms
{
    public class ConflictWarningDialog : Form
    {
        public enum ConflictAction
        {
            ChooseAvailableTime,
            Replace
        }

        public ConflictAction SelectedAction { get; private set; }

        private Label lblMessage;
        private Button btnChooseTime;
        private Button btnReplace;

        public ConflictWarningDialog(string message)
        {
            InitializeComponent();
            lblMessage.Text = message;
        }

        private void InitializeComponent()
        {
            this.Text = "Trùng lịch hẹn";
            this.Size = new Size(400, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblMessage = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(340, 60),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.TopCenter
            };
            this.Controls.Add(lblMessage);

            btnChooseTime = new Button
            {
                Text = "Chọn giờ khác",
                Location = new Point(40, 100),
                Size = new Size(130, 35),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnChooseTime.Click += (s, e) =>
            {
                SelectedAction = ConflictAction.ChooseAvailableTime;
                this.DialogResult = DialogResult.OK;
            };
            this.Controls.Add(btnChooseTime);

            btnReplace = new Button
            {
                Text = "Ghi đè lịch cũ",
                Location = new Point(210, 100),
                Size = new Size(130, 35),
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnReplace.Click += (s, e) =>
            {
                SelectedAction = ConflictAction.Replace;
                this.DialogResult = DialogResult.OK;
            };
            this.Controls.Add(btnReplace);
        }
    }
}
