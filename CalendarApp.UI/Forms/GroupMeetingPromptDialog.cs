using System;
using System.Drawing;
using System.Windows.Forms;

namespace CalendarApp.UI.Forms
{
    public class GroupMeetingPromptDialog : Form
    {
        public bool JoinGroup { get; private set; }

        private Label lblMessage;
        private Button btnJoin;
        private Button btnDecline;

        public GroupMeetingPromptDialog(string meetingName)
        {
            InitializeComponent();
            lblMessage.Text = $"Hệ thống phát hiện có một Group Meeting tên '{meetingName}' cùng thời lượng.\nBạn có muốn tham gia vào Group Meeting này thay vì tạo lịch cá nhân không?";
        }

        private void InitializeComponent()
        {
            this.Text = "Gợi ý tham gia Group Meeting";
            this.Size = new Size(450, 200);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            lblMessage = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(400, 60),
                Font = new Font("Segoe UI", 10F),
                TextAlign = ContentAlignment.TopCenter
            };
            this.Controls.Add(lblMessage);

            btnJoin = new Button
            {
                Text = "Tham gia",
                Location = new Point(70, 100),
                Size = new Size(120, 35),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnJoin.Click += (s, e) =>
            {
                JoinGroup = true;
                this.DialogResult = DialogResult.OK;
            };
            this.Controls.Add(btnJoin);

            btnDecline = new Button
            {
                Text = "Không, tạo mới",
                Location = new Point(250, 100),
                Size = new Size(120, 35),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnDecline.Click += (s, e) =>
            {
                JoinGroup = false;
                this.DialogResult = DialogResult.OK;
            };
            this.Controls.Add(btnDecline);
        }
    }
}
