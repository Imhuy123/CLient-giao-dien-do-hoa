using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
namespace CLient_giao_dien_do_hoa
{
    public partial class Form1 : Form
    {
        private Socket _clientSocket;
        private byte[] _buffer = new byte[1024];
        public Form1()
        {
            InitializeComponent();
        }







        private void Form1_Load(object sender, EventArgs e)
        {
           
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

                // Định nghĩa tên miền và cổng
                string domainName = "huynas123.synology.me"; // Tên miền cụ thể
                int portToRun = 8081; // Cổng 8081

                // Chuyển tên miền thành IP
                IPAddress[] ipAddresses = Dns.GetHostAddresses(domainName);
                IPAddress ipAddr = ipAddresses[0];
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, portToRun);

                // Tạo socket TCP/IP
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

                // Kiểm tra nếu server gửi danh sách người dùng
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
                    AppendMessage(text);
                }

                // Tiếp tục nhận dữ liệu từ server
                _clientSocket.BeginReceive(_buffer, 0, _buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), null);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error receiving data: " + ex.Message);
            }
        }


        private void AppendMessage(string message)
        {
            rtbChat.Invoke((MethodInvoker)(() =>
            {
                rtbChat.AppendText(message + Environment.NewLine);
            }));
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

    }
}
