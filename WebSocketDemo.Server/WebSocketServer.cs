using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketDemo.Server
{
    public class WebSocketServer
    {
        public async void Run(string httpListenerPrefix)
        {
            var httpListener = new HttpListener();
            httpListener.Prefixes.Add(httpListenerPrefix);
            httpListener.Start();
            Console.WriteLine("Listening...");

            while (true)
            {
                HttpListenerContext httpListenerContext = await httpListener.GetContextAsync();
                if (httpListenerContext.Request.IsWebSocketRequest)
                {
                    ProcessRequest(httpListenerContext);
                    Console.WriteLine(httpListenerContext.Request.Cookies[1]);
                }
                else
                {
                    httpListenerContext.Response.StatusCode = 400;
                    httpListenerContext.Response.Close();
                }
            }
        }

        private async void ProcessRequest(HttpListenerContext httpListenerContext)
        {
            WebSocketContext webSocketContext = null;
            try
            {
                webSocketContext = await httpListenerContext.AcceptWebSocketAsync(subProtocol: null);
                var ipAddress = httpListenerContext.Request.RemoteEndPoint.Address.ToString();
                Console.WriteLine("Connected: IPAddress {0}", ipAddress);
            }
            catch (Exception e)
            {
                httpListenerContext.Response.StatusCode = 500;
                httpListenerContext.Response.Close();
                Console.WriteLine("Exception: {0}", e);
                return;
            }

            WebSocket webSocket = webSocketContext.WebSocket;

            var taskReturmMsgBack = Task.Run(async () => {
                try
                {
                    byte[] receiveBuffer = new byte[1024];
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                        if (receiveResult.MessageType == WebSocketMessageType.Close)
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        else
                        {
                            Console.WriteLine("Message from user: " + Encoding.UTF8.GetString(receiveBuffer).TrimEnd('\0'));
                            await webSocket.SendAsync(new ArraySegment<byte>(receiveBuffer, 0, receiveResult.Count), WebSocketMessageType.Text, receiveResult.EndOfMessage, CancellationToken.None);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception: {0}", e);
                }
                finally
                {
                    if (webSocket != null)
                        webSocket.Dispose();
                }
            });

            var taskSendMsg = Task.Run(async () => {
                while (true)
                {
                    Console.Write("Send something to user: ");

                    var msg = Console.ReadLine();
                    var bytes = Encoding.UTF8.GetBytes(msg);

                    await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            });
        }
    }
}
