using System;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace MyProject
{
    public class Echo : WebSocketBehavior
    {
        protected override void OnMessage(MessageEventArgs e)
        {
            Send("Echo: " + e.Data);
        }
    }

    public class WebsocketMain
    {
        public static void Main()
        {
            var wssv = new WebSocketServer("ws://localhost:8081");

            // エンドポイントを /Test に変更
            wssv.AddWebSocketService<Echo>("/Test");
            wssv.Start();
            Console.WriteLine("WebSocket server started at ws://localhost:8080/Test");

            Console.ReadKey(true);
            wssv.Stop();
        }
    }

}