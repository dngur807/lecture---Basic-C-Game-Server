using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreeNet
{
    class CListener
    {
        // 비동기 Accept를 위한 EventArgs
        SocketAsyncEventArgs accept_args;

        // 클라이언트의 접속을 처리할 소켓
        Socket listen_socket;

        // Accept처리의 순서를 제어하기 위한 이벤트 변수
        AutoResetEvent flow_control_event;

        // 새로운 클라이언트가 접속했을 때 호출되는 콜백
        public delegate void NewclientHandler(Socket client_socket, object token);
        public NewclientHandler callback_on_newclient;

        public CListener()
        {
            this.callback_on_newclient = null;
        }

        public void start(string host, int port, int backlog)
        {
            // 소켓을 생성합니다.
            this.listen_socket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);

            IPAddress address;
            if (host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endPoint = new IPEndPoint(address, port);

            try
            {
                // 소켓에 host정보를 바인딩 시킨뒤 Listen매소드를 호출하여 준비를 합니다.
                this.listen_socket.Bind(endPoint);
                this.listen_socket.Listen(backlog);

                this.accept_args = new SocketAsyncEventArgs();
                this.accept_args.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);

                // 클라이언트가 들어오기를 기다립니다.
                // 비동기 매소드 이므로 블로킹 되지 않고 바로 리턴됩니다.
                // 콜백 매소드를 통해서 접속 통보를 처리하면 됩니다.
                // this.listen_socket.AcceptAsync(this.accept_args);
                Thread listen_thread = new Thread(do_listen);
                listen_thread.Start();
            }
            catch (Exception e)
            {

            }

            void do_listen()
            {
                // accept처리 제어를 위해 이벤트 객체를 생성합니다.
                this.flow_control_event = new AutoResetEvent(false);

                while (true)
                {
                    // SocketAsyncEventArgs를 재사용 하기 위해서 null로 만들어 준다.
                    this.accept_args.AcceptSocket = null;

                    bool pending = true;
                    try
                    {

                    }
                }
            }
        }
    }
}
