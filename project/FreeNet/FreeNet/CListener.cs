﻿using System;
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
        public delegate void NewClientHandler(Socket client_socket, object token);
        public NewClientHandler callback_on_newclient;

        public CListener()
        {
            this.callback_on_newclient = null;
        }

        public void start(string host , int port , int backlog)
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
                // 소켓에 host정보를 바인딩 시킨뒤 Listen 매소드를 호출하여 준비를 합니다.
                this.listen_socket.Bind(endPoint);
                this.listen_socket.Listen(backlog);

                this.accept_args = new SocketAsyncEventArgs();
                this.accept_args.Completed += new EventHandler<SocketAsyncEventArgs>(on_accept_completed);

                // 클라이언트가 들어오기를 기다립니다.
                // 비동기 매소드 이므로 블로킹 되지 않고 바로 리턴됩니다.
                // 콜백 매소드를 통해서 접속 통보를 처리하면 됩니다.
                // this.listen_socket.AcceptAsync(this.accpet_args);

                Thread listen_thread = new Thread(do_listen);
                listen_thread.Start();      
            }
            catch(Exception e)
            {

            }
        }
        void do_listen()
        {
            // accept 처리를 제어를 위해 이벤트 객체를 생성합니다.
            this.flow_control_event = new AutoResetEvent(false);

            while (true)
            {
                //SocketAssyncEventArgs를 재사용 하기 위해서 null로 만들어 준다.
                this.accept_args.AcceptSocket = null;

                bool pending = true;
                try
                {
                    // 비동기 accept를 호출하여 클라이언트의 접속을 받아들입니다.
                    // 비동기 매소드 이지만 동기적으로 수행이 완료될 경우도 있으니
                    // 리턴 값을 확인하여 분기시켜야 합니다.
                    pending = listen_socket.AcceptAsync(this.accept_args);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }

                // 즉시 완료 되면 이벤트가 발생하지 않으므로 리턴값이 false일 경우 콜백 매소드를 직접 호출해 줍니다.
                // pending 상태라면 비동기 요청이 
                if (!pending)
                {
                    on_accept_completed(null, this.accept_args);
                }

                // 클라이언트 접속 처리가 완료되면 이벤트 객체의 신호를 전달받아 다시 루프를 수행하도록 합니다.
                this.flow_control_event.WaitOne();
            }
        }

        void on_accept_completed(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // 새로 생긴 소켓을 보관해 놓은뒤
                Socket client_socket = e.AcceptSocket;

                // 다음 연결을 받아들인다.
                this.flow_control_event.Set();

                // 이 클래스에서는 accept까지의 역할만 수행하고 클라이언트의 접속 이후의 처리는 
                // 외부로 넘기기 위해서 콜백 매소드를 호출해 주도록 합니다.
                // 이유는 소켓 처리부와 컨텐츠 구현부를 분리하기 위함입니다.
                // 컨텐츠 구현부분은 자주 바뀔 가능성이 있지만, 소켓 Accept부분은 상대적으로 변경이 적은 부분이기 때문에
                // 양쪽을 분리시켜주는 것이 좋습니다.
                // 또한 클래스 설계 방침에 따라 Listen에 관련된 코드만 존재하도록 하기 위한 이유도 있습니다.

                if (this.callback_on_newclient != null)
                {
                    this.callback_on_newclient(client_socket, e.UserToken);
                }
                return;
            }
            else
            {
                // Accept 실패 처리
                Console.WriteLine("Failed to accept client.");
            }


        }
    }
}
