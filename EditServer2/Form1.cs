using ChatServer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EditServer2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


        // 전체 유저 목록
        public List<MyLibrary.Player> players = new List<MyLibrary.Player>();
        public List<MyLibrary.Room> roomList = new List<MyLibrary.Room>();


        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                MyLibrary.Listner lisnter = new MyLibrary.Listner(() => { return new ClientSession(null, listBox1, this); });

                Console.WriteLine("서버 실행...");
                button1.Enabled = false;
                BackColor = Color.White;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }
        }

 


    }

}
