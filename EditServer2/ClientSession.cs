using EditServer2;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatServer
{
  
    public class ClientSession : MyLibrary.Session
    {
        SqlConnection sqlconnection;
        ListBox _ListBox;
      //  LinkedList<MyLibrary.Player> players = new LinkedList<MyLibrary.Player>();
        public Form1 form;

        object Listobj = new object();
        public ClientSession(SqlConnection _sqlConnection, ListBox listBox, Form1 form)
        {
            this.form = form;
            sqlconnection = _sqlConnection;
            _ListBox = listBox;
        }

        LinkedList<MyLibrary.Player> playerList = new LinkedList<MyLibrary.Player>();
        
        public override void OnConnect(Socket socket)
        {

        }

        public override void OnDispose()
        {

        }

        public override void OnRecv(int recvByte, byte[] buffer)
        {
            Console.WriteLine("OnRecv");
            string str = Encoding.UTF8.GetString(buffer);

            string packetType = "";

            int i = 0;

            for (i = 0; i < str.Length; i++)
            {
                if (str[i] == ',')
                {
                    i++;
                    break;
                }
                packetType += str[i];
            }
            if (packetType.Equals("JoinPacket"))
            {
                string ID = "";
                string PW = "";
                string NickName = "";
                for (; i < str.Length; i++)
                {
                    if (str[i] == ',')
                    {
                        i++;
                        break;
                    }
                    NickName += str[i];

                }

                for (; i < str.Length; i++)
                {
                    if (str[i] == ',')
                    {

                        i++;
                        break;
                    }
                    ID += str[i];

                }


                for (; i < str.Length; i++)
                {
                    if (str[i] == ',')
                    {
                        break;
                    }
                    PW += str[i];

                }


                Console.WriteLine($"{ID} 유저가 가입 요청.");


                string joinSQL = $"INSERT INTO Account VALUES('{ID}','{PW}','{NickName}');";

                Console.WriteLine($"{joinSQL}");


                int effectedQuery = 0;
                // 쿼리 오류
                try
                {
                    SqlCommand Command = new SqlCommand(joinSQL, sqlconnection);
                    effectedQuery = Command.ExecuteNonQuery();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception.Message);
                }

                Console.WriteLine($"{effectedQuery}");
                string sendString = "";

                if (effectedQuery == 0)
                    sendString = "fail,";
                else
                    sendString = "success,";

                byte[] sendMessage = Encoding.UTF8.GetBytes(sendString);


                _socket.Send(sendMessage);


                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();

                _socket = null;
            }
            else if (packetType.Equals("Access"))
            {
                Console.WriteLine($"{_socket.RemoteEndPoint} 유저가 로그인 요청.");
                string ID = "";
                string PW = "";



                for (; i < str.Length; i++)
                {
                    if (str[i] == ',')
                    {

                        i++;
                        break;
                    }
                    ID += str[i];

                }


                for (; i < str.Length; i++)
                {
                    if (str[i] == ',')
                    {
                        break;
                    }
                    PW += str[i];

                }


                // notepad에 사용할 서버 내용 #############################
                if (ID.Contains("note") && PW.Contains("note"))
                {

                    _socket.Send(Encoding.UTF8.GetBytes("success," + ID + ","));

                    // 서버 메모리에 플레이어 목록 저장하기
                    MyLibrary.Player p = new MyLibrary.Player();

                    p.socket = _socket;
                    p.NickName = ID;
                    p.Session = this;
                    p.ID = ID;


                    form.players.Add(p);
                    playerList.AddFirst(p);



                    // 입장하는 유저에게 현재 방 목록 전해주기
                    MyLibrary.DataList dataList = new MyLibrary.DataList();
                    List<string> roomlist = new List<string>();
                    foreach (MyLibrary.Room room in form.roomList)
                    {
                        roomlist.Add(room.roomName);
                    }
                    dataList.playerList = roomlist;
                    dataList.playerNumber = roomlist.Count;
                    dataList.type = "room";
                    Send(dataList.Read());
                }
                else
                {
                    string joinSQL = $"SELECT COUNT(*) FROM dbo.Account WHERE ID = '{ID}' AND PW ='{PW}';";

                    Console.WriteLine($"{joinSQL}");


                    int effectedQuery = 0;
                    // 쿼리 오류
                    try
                    {
                        SqlCommand Command = new SqlCommand(joinSQL, sqlconnection);
                        effectedQuery = Convert.ToInt32(Command.ExecuteScalar());


                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }

                    Console.WriteLine($"{effectedQuery}");
                    string sendString = "";

                    if (effectedQuery == 1)
                        sendString = "success,";
                    else
                        sendString = "fail,";

                    byte[] sendMessage = Encoding.UTF8.GetBytes(sendString);


                    //_socket.Send(sendMessage);

                    // 닉네임 불러오기
                    string nickSQL = $"SELECT NickName FROM dbo.Account WHERE ID = '{ID}' AND PW ='{PW}';";

                    string nickName;
                    string nickName2;
                    SqlDataReader sqlDataReader = null;
                    // 쿼리 오류
                    try
                    {
                        SqlCommand Command = new SqlCommand(nickSQL, sqlconnection);
                        int effectColumn = Command.ExecuteNonQuery();
                        if (effectedQuery == 0)
                        {
                            _socket.Send(Encoding.UTF8.GetBytes(sendString + "empty,"));
                            return;

                        }
                        sqlDataReader = Command.ExecuteReader();
                        sqlDataReader.Read();
                        Console.WriteLine("닉네임:{0}", sqlDataReader["NickName"]);
                        nickName = sqlDataReader["NickName"].ToString().Trim();
                        nickName2 = nickName + ",";
                        _socket.Send(Encoding.UTF8.GetBytes(sendString + nickName2.Trim()));

                        sqlDataReader.Close();
                        // 서버 메모리에 플레이어 목록 저장하기
                        MyLibrary.Player p = new MyLibrary.Player();
                        p.socket = _socket;
                        p.NickName = nickName;
                        p.Session = this;
                        p.ID = ID;

                        form.players.Add(p);
                        playerList.AddFirst(p);


                        lock (Listobj)
                        {
                            form.Invoke(new Action(() =>
                            {

                                _ListBox.Items.Add(nickName);
                            }
                       ));
                        }

                        byte[] successMessage = new byte[1024];


                        // 입장하는 유저에게 현재 방 목록 전해주기
                        MyLibrary.DataList dataList = new MyLibrary.DataList();
                        List<string> roomlist = new List<string>();
                        foreach (MyLibrary.Room room in form.roomList)
                        {
                            roomlist.Add(room.roomName);
                        }
                        dataList.playerList = roomlist;
                        dataList.playerNumber = roomlist.Count;
                        dataList.type = "room";
                        Send(dataList.Read());

                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception.Message);
                    }
                    sqlDataReader.Close();

                    // 소켓을 연결해둔다. 
                    //socket.Shutdown(SocketShutdown.Both);
                    //socket.Close();
                }
            }
            else if (packetType.Equals("ProcessExit"))
            {

                MyLibrary.ProcessExitPacket processExitPacket = new MyLibrary.ProcessExitPacket();
                processExitPacket.Write(buffer);


                Console.WriteLine("종료 메시지");


                // 종료 인덱스 제거
                lock (Listobj)
                {
                    form.Invoke(new Action(() =>
                    {
                        Console.WriteLine("do invoke");
                        for (int index = 0; index < _ListBox.Items.Count; index++)
                        {

                            if (_ListBox.Items[index].ToString().Equals(processExitPacket.NickName))
                            {
                                _ListBox.Items.RemoveAt(index);
                                Console.WriteLine($"index = {index}");
                                break;
                            }
                        }
                    }
                    ));
                }
            }
            else if (packetType.Equals("MakeRoom"))
            {
                int idx = 0;
                try
                {
                    // 같은 이름의 방이 있는지 확인하고
                    // TODO 
                    MyLibrary.MakeRoom makeRoomPacket = new MyLibrary.MakeRoom();
                    makeRoomPacket.Write(buffer);

                    Console.WriteLine($"{makeRoomPacket.ID} 유저가 방 만들기 요청");
                    // 그렇게 보낸 유저찾고
                    form.Invoke(new Action(() =>
                    {
                        //      form.listBox2.Items.Add(makeRoomPacket.roomName);
                    }));

                    for (; idx < form.players.Count; idx++)
                    {
                        if (form.players[idx].ID.Equals(makeRoomPacket.ID))
                        {
                            // 방 만들고(해당 유저 방장으로)
                            MyLibrary.Room room = new MyLibrary.Room(makeRoomPacket.roomName, form.players[idx], null);
                            // 방만 만들고 유저는 다음에 추가
                            //    room.AddPlayer(form.players[idx]); 

                            form.roomList.Add(room);

                            break;
                        }

                    }


                    // 해당 유저에게 입장 허락 패킷 보내기
                    form.players[idx].Session.Send(Encoding.UTF8.GetBytes($"MakeRoomSuccess,{makeRoomPacket.roomName},"));
                    Console.WriteLine($"'{makeRoomPacket.ID}'유저가 만들기 요청한 '{makeRoomPacket.roomName}'방 생성 완료.");

                    // 만들었다고 패킷을 모든 유저에게 보내기(메인 폼에서 현재 방목록 갱신)
                    foreach (MyLibrary.Player player in form.players)
                    {
                        player.Session.Send(makeRoomPacket.Read());
                    }



                }
                catch
                {
                    form.players[idx].Session.Send(Encoding.UTF8.GetBytes("MakeRoomFail"));


                }
            }
            else if (packetType.Equals("JoinRoom"))
            {
                try
                {
                    MyLibrary.JoinRoom joinPacket = new MyLibrary.JoinRoom();
                    joinPacket.Write(buffer);


                    Console.WriteLine($"{joinPacket.NickName}유저가 {joinPacket.roomName}방 입장 요청");

                    // 서버 메모리에 입장상태 만들기

                    // 플레이어 먼저 뽑아내기
                    MyLibrary.Player player = new MyLibrary.Player();

                    for (int playidx = 0; playidx < form.players.Count; playidx++)
                    {
                        if (form.players[playidx].ID.Equals(joinPacket.ID))
                        {
                            player = form.players[playidx];
                            break;
                        }
                    }

                    for (int l = 0; l < form.roomList.Count; l++)
                    {
                        if (form.roomList[l].roomName.Equals(joinPacket.roomName))
                        {
                            form.roomList[l].AddPlayer(player);
                        }
                    }
                    Send(joinPacket.Read());

                    Console.WriteLine($"{joinPacket.NickName}유저가 {joinPacket.roomName}방 입장 완료");


                }
                catch
                {
                    Console.WriteLine("방 입장 오류");
                }

            }
            else if (packetType.Equals("SendMessage"))//메시지 받기
            {
                MyLibrary.SendMessage sendMessage = new MyLibrary.SendMessage();
                sendMessage.Write(buffer);
                Console.WriteLine($"{sendMessage.roomName}|{sendMessage.nickName}: {sendMessage.message}");

                // 방에있는 모두에게 message보내기
                for (int l = 0; l < form.roomList.Count; l++)
                {
                    if (form.roomList[l].roomName.Equals(sendMessage.roomName))
                    {
                        foreach (var user in form.roomList[l].playerList)
                        {
                            user.Session.Send(sendMessage.Read());
                        }
                    }
                }
            }
            else if (packetType.Equals("ExitRoom"))
            {
                MyLibrary.ExitRoom exitRoom = new MyLibrary.ExitRoom();
                exitRoom.Write(buffer);
                Console.WriteLine($"{exitRoom.roomName}|{exitRoom.nickName}유저가 방에서 나감");

                // 메모리에서 해당 방 찾아서 유저 빼기
                for (int roomIndex = 0; roomIndex < form.roomList.Count; roomIndex++)
                {
                    if (form.roomList[roomIndex].roomName.Equals(exitRoom.roomName))
                    {//
                        for (int playerIndex = 0; playerIndex < form.roomList.Count; playerIndex++)
                        {
                            if (form.roomList[roomIndex].playerList[playerIndex].ID.Equals(exitRoom.id))
                            {
                                form.roomList[roomIndex].playerList.RemoveAt(playerIndex);
                                break;
                            }
                        }

                        // 남아있는 유저가 없으면 메모리에 있는 방 파괴
                        if (form.roomList[roomIndex].playerList.Count == 0)
                        {
                            Console.WriteLine($"{form.roomList[roomIndex].roomName}방 유저가 없으므로 삭제");


                            for (int deleteRoomIndex = 0; deleteRoomIndex < form.roomList.Count; i++)
                            {
                                //if(form.listBox2.Items[deleteRoomIndex].Equals(exitRoom.roomName))
                                //{
                                //    form.Invoke(new Action(() =>
                                //    {
                                //        form.listBox2.Items.RemoveAt(deleteRoomIndex);
                                //    }));
                                //    break;
                                //}
                            }

                            // 다른 유저들에게 방이 파괴됐다는 신호를 준다.
                            for (int playerIndex = 0; playerIndex < form.players.Count; playerIndex++)
                            {
                                form.players[playerIndex].Session.Send(buffer);
                            }

                            form.roomList.RemoveAt(roomIndex);

                        }
                    }
                }


            }
            else if (packetType.Equals("SendArgs"))
            {
                MyLibrary.SendArgs sendMessage = new MyLibrary.SendArgs();
                sendMessage.Write(buffer);
                Console.WriteLine($"{sendMessage.roomName}|{sendMessage.nickName}: {sendMessage.ordered}");

                // 방에있는 모두에게 message보내기
                Console.WriteLine($"방 갯수: {form.roomList.Count}");
                
                for (int l = 0; l < form.roomList.Count; l++)
                {
                    if (form.roomList[l].roomName.Equals(sendMessage.roomName))
                    {
                        Console.WriteLine($"방에 사람 인원수: {form.roomList[l].playerList}, 중 1명에 전송");
                        foreach (var user in form.roomList[l].playerList)
                        {
                            user.Session.Send(sendMessage.Read());
                        }
                    }
                }
            }



            }

        public override void OnSend(int sendBytes)
        {

        }
    }
}
