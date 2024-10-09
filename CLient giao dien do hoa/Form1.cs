using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace CLient_giao_dien_do_hoa
{
    public partial class Form1 : Form
    {
        private Socket _clientSocket;
        private byte[] _buffer = new byte[1024];
        private Dictionary<string, List<string>> _chatHistory = new Dictionary<string, List<string>>(); // Lưu trữ lịch sử tin nhắn
        private string _currentChatUser = null; // Người dùng hiện tại đang chat

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Mã khởi tạo nếu cần
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                string userName = txtUserName.Text.Trim();
                if (string.IsNullOrEmpty(userName))
                {
                    MessageBox.Show("Please enter your name.");
                    return;
                }

                string domainName = "huynas123.synology.me";
                int portToRun = 8081;

                IPAddress[] ipAddresses = Dns.GetHostAddresses(domainName);
                IPAddress ipAddr = ipAddresses[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, portToRun);

                _clientSocket = new Socket(ipAddr.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                _clientSocket.Connect(localEndPoint);
                AppendMessage($"Connected to server {domainName}:{portToRun}");

                // Gửi tên người dùng tới server
                byte[] messageSent = Encoding.ASCII.GetBytes(userName + "<EOF>");
                _clientSocket.Send(messageSent);

                // Bắt đầu nhận dữ liệu từ server
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error connecting to server: " + ex.Message);
            }
        }

        private void ReceiveCallback(IAsyncResult AR)
        {
            try
            {
                int received = _clientSocket.EndReceive(AR);
                string text = Encoding.ASCII.GetString(_buffer, 0, received);

                if (text.StartsWith("UserList:"))
                {
                    string userList = text.Replace("UserList:", "").Replace("<EOF>", "");
                    string[] users = userList.Split(',');

                    // Cập nhật danh sách người dùng trên giao diện
                    lstUsers.Invoke((MethodInvoker)(() =>
                    {
                        lstUsers.Items.Clear();
                        lstUsers.Items.AddRange(users);
                    }));
                }
                else
                {
                    // Tin nhắn định dạng "FromUser:MessageContent"
                    string[] splitMessage = text.Replace("<EOF>", "").Split(new char[] { ':' }, 2);
                    if (splitMessage.Length == 2)
                    {
                        string fromUser = splitMessage[0];
                        string messageContent = splitMessage[1];

                        // Lưu trữ tin nhắn vào lịch sử chat của người gửi
                        if (!_chatHistory.ContainsKey(fromUser))
                        {
                            _chatHistory[fromUser] = new List<string>();
                        }
                        _chatHistory[fromUser].Add($"{fromUser}: {messageContent}");

                        // Nếu người nhận là người dùng hiện tại, hiển thị tin nhắn ngay lập tức
                        if (_currentChatUser == fromUser)
                        {
                            AppendMessage($"{fromUser}: {messageContent}");
                        }
                    }
                }

                // Tiếp tục nhận dữ liệu từ server
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving data: " + ex.Message);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(_currentChatUser))
                {
                    MessageBox.Show("Please select a user to send the message.");
                    return;
                }

                string message = txtMessage.Text.Trim();
                if (string.IsNullOrEmpty(message))
                {
                    MessageBox.Show("Please enter a message.");
                    return;
                }

                // Gửi tin nhắn với định dạng "RecipientUser:Message<EOF>"
                string formattedMessage = $"{_currentChatUser}:{message}<EOF>";
                byte[] messageSent = Encoding.ASCII.GetBytes(formattedMessage);
                _clientSocket.Send(messageSent);

                // Lưu tin nhắn vào lịch sử chat của người dùng hiện tại
                if (!_chatHistory.ContainsKey(_currentChatUser))
                {
                    _chatHistory[_currentChatUser] = new List<string>();
                }
                _chatHistory[_currentChatUser].Add($"You: {message}");

                // Hiển thị tin nhắn đã gửi
                AppendMessage($"You: {message}");
                txtMessage.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error sending message: " + ex.Message);
            }
        }

        private void AppendMessage(string message)
        {
            // Thêm tin nhắn vào rtbChat và tự động cuộn xuống dòng cuối cùng
            rtbChat.Invoke((MethodInvoker)(() =>
            {
                rtbChat.AppendText(message + Environment.NewLine);
                rtbChat.ScrollToCaret();
            }));
        }

        private void lstUsers_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstUsers.SelectedItem != null)
            {
                _currentChatUser = lstUsers.SelectedItem.ToString();

                // Hiển thị lịch sử tin nhắn của người dùng hiện tại
                rtbChat.Clear();
                if (_chatHistory.ContainsKey(_currentChatUser))
                {
                    foreach (string msg in _chatHistory[_currentChatUser])
                    {
                        AppendMessage(msg);
                    }
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Đóng kết nối khi form đóng
            if (_clientSocket != null && _clientSocket.Connected)
            {
                _clientSocket.Shutdown(SocketShutdown.Both);
                _clientSocket.Close();
            }
        }

        private void rtbChat_TextChanged(object sender, EventArgs e)
        {

        }
    }
}
