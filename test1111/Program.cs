using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Timers;
using System.Collections.Generic;
using System.Diagnostics;
using test1111.sqlite3_database;
using System.Text.RegularExpressions;

public class UserLog : WebSocketBehavior
{
    private static List<UserLog> clients = new List<UserLog>();
    private static string serverIp = GetLocalIPAddress();
    private static int _userEnterCount = 0;
    public static int userEnterCount
    {
        get { return _userEnterCount; }
        set { _userEnterCount = value;
            userEneterCountChanged?.Invoke(_userEnterCount);
        }
    }
    public static event Action<int> userEneterCountChanged;

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"New client connected: {clientIp}");

        Broadcast($"New client connected: {clientIp}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }
        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"client removed: {clientIp}");

        Broadcast($"client removed: {clientIp}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        string result = Db.queryScalar<string>(Db.connectionString, "select name from user where user.id = @data", new Dictionary<string, string> { { "@data", e.Data } });
        if (result != "null")
        {
            //ユーザーからのパスワード入力が成功した後の処理
            userEnterCount++;//参加したユーザーをカウントする
            Send("password is correct");
            //サーバーに伝える
            Console.WriteLine($"{result}が参加しました.");
        }
        else
        {
            Console.WriteLine($"だれかがパスワードを間違えたようです.");
            Send("password is not correct");
        }
    }

    private void Broadcast(string message)
    {
        lock (clients)
        {
            foreach (var client in clients)
            {
                client.Send(message);
            }
        }
    }

    private static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }
}

public class SimulationNotification : WebSocketBehavior
{
    
    protected override void OnMessage(MessageEventArgs e)
    {
        Send("Workshop: " + e.Data);
    }

    private static List<SimulationNotification> clients = new List<SimulationNotification>();
    

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"SimulationNotification->New client connected: {clientIp}");

        //Broadcast($"New client connected: {clientIp}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }
        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"SimulationNotification->client removed: {clientIp}");

        //Broadcast($"client removed: {clientIp}");
    }
}

public class WebsocketTimer : WebSocketBehavior
{
    
    private static List<WebsocketTimer> clients = new List<WebsocketTimer>();

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"WebsocketTimer->New client connected: {clientIp}");

        //Broadcast($"New client connected: {clientIp}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }
        string clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"WebsocketTimer->client removed: {clientIp}");

        //Broadcast($"client removed: {clientIp}");
    } 
    protected override void OnMessage(MessageEventArgs e)
    {
        // クライアントからのメッセージを受信した場合の処理
        Console.WriteLine("Message received: " + e.Data);
    }




}

public class Simulation : WebSocketBehavior
{

    private static List<Simulation> clients = new List<Simulation>();

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"Simulation->New client connected: {clientIp}");

        //Broadcast($"New client connected: {clientIp}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }
        string clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"Simulation->client removed: {clientIp}");

        //Broadcast($"client removed: {clientIp}");
    }
    protected override void OnMessage(MessageEventArgs e)
    {
        string[] parts = e.Data.Split(':');//TEST:userId:111333という形式
        // クライアントからのメッセージを受信した場合の処理
        //if()
        if (parts[0] == "TEST")//テスト時
        {

        }
        else if (parts[0] == "ACTION")//実際に書き込む
        {

        }
        else
        {
            Send("simulation->OnMessages is incorrect");
        }
    }

    //帰宅遷移状況を表す文字列が適切か調べる
    private bool simulationStringCheack(string status)
    {

        if (Regex.IsMatch(@"^1{1,14}[2-9]{0,13}$", status))//帰宅遷移状況を表す文字列形式になっているか確認
        {
            //形式は妥当なので、次の処理を勧める
            return true;
        }
        else
        {
            return false;
        }
    }
    //選択できる帰宅遷移状況かを調べる
    /*
     * 現在のルールは以下の通り
     * ・規制時間　3分間隔　15秒→30秒
　　　　　1分半間隔　9秒→20秒

・手段⑤として、社用車
　ルール　全ての班全体で1台のみ使える（決めるのはくじ引き）
　　　　　時間14時から16時、17時以降で使える

・例えば、11時で選択したら11時の帰宅資料が出るがその処理
　＃＃＃話す

・家に帰った時の画像（河村がやる）

・ルール
11時から13時に1人帰宅
　14時から16時に1人帰宅（ここから社用車が使える）
　17時から23時に1人帰宅かホテル
　　　　　　　　　1人までが会社に宿泊可能
　
　帰宅手段は班内で1つずつしか使えない
　ホテル、社用車はアナログでくじ引き
     */

    private bool simulationSelectCheck(string status, string id)
    {
        string query2 = "SELECT simulation.* FROM simulation JOIN user ON simulation.group = user.group WHERE user.id = @id AND simulation.status NOT LIKE '%1'";
        int count = Db.queryScalar<int>(Db.connectionString, query2, new Dictionary<string, string> { { "@id", id } });
        string lastSelect = status[status.Length - 1].ToString();
        int simulationTime;
        if (lastSelect == "1")
        {
            simulationTime = status.Length + 10;
            if (Regex.IsMatch("^(13)$", simulationTime.ToString()))
            {
                if (!(count >= 1)) return false;
            }
            if (Regex.IsMatch("^(16)$", simulationTime.ToString()))
            {
                if (!(count >= 2)) return false;
            }
            if (Regex.IsMatch("^(23)$", simulationTime.ToString()))
            {
                if (!(count >= 3)) return false;
            }
            //何もなければtrueを返す
            return true;
        }
        else if (Regex.IsMatch("^(2|3|4|5|6)$", lastSelect))
        {
            simulationTime = status.Length + 9;
            string query = "SELECT simulation.* FROM simulation JOIN user ON simulation.group = user.group WHERE user.id = @id AND simulation.status LIKE @lastSelect";

            //まず同じ帰宅手段を選んでいる人が同一グループに居ないか調べる
            if (!Db.queryScalar(Db.connectionString, query, new Dictionary<string, string> { { "@id", id }, { "@lastSelect", $"%{lastSelect}" } })) return false;
            //
          
            if (Regex.IsMatch("^(11|12|13)$", simulationTime.ToString()))
            {

                if (!(count +1 <= 1)) return false;

            }
            else if (Regex.IsMatch("^(|14|15|16)$", simulationTime.ToString()))
            {
                if (!(count + 1 <= 2)) return false;
            }
            else if (Regex.IsMatch("^(17|18|19|20|21|22|23)$", simulationTime.ToString()))
            {
                if (!(count + 1 <= 3)) return false;
            }
            //何もなければtrueを返す
            return true;
        }
        else return false;
    }

}


public class Program
{
    private static System.Timers.Timer broadcastTimer;
    private static System.Timers.Timer TimebroadcastTimer;
    private static Stopwatch stopwatch;
    private static DateTime startTime;
    public static TimeSpan elapsed;
    public static DateTime currentTime = new DateTime(1997, 7, 1, 11, 0, 0);
    private static WebSocketServer wssv; // 静的フィールドとして定義
    public static float SimulationTimeScale;

    public static void Main(string[] args)
    {
        Db.Run();//データベースの初期化を行う.
        //websocket通信を確立する
        Console.OutputEncoding = Encoding.UTF8;
        wssv = new WebSocketServer("ws://0.0.0.0:8080");
        wssv.Log.Output = (_, __) => { };
        wssv.AddWebSocketService<UserLog>("/UserLog");
        wssv.AddWebSocketService<SimulationNotification>("/Notification");
        wssv.AddWebSocketService<WebsocketTimer>("/Timer");
        wssv.AddWebSocketService<Simulation>("/Simulation");
        wssv.Start();
        Console.WriteLine("WebSocket server started at ws://localhost:8080/UserLog");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Workshop");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Timer");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Simulation");
        // ブロードキャストメッセージを定期的に送信
        broadcastTimer = new System.Timers.Timer(5000);
        broadcastTimer.Elapsed += BroadcastMessage;
        broadcastTimer.AutoReset = true;
        broadcastTimer.Enabled = true;

        //ユーザーが参加するまで待つ.
        UserLog.userEneterCountChanged += (int count) =>
        {
            if (count == Db.userTableColumnCount) return;
            //ユーザーの参加が完了した後の処理
            Console.WriteLine("コマンドを入力してください（サーバーを終了するには 'サーバーストップ' と入力）");
            Console.WriteLine("コマンドを入力してください（シミュレーションを開始するには 'start' と入力）：");

            while (true)
            {

                string input = Console.ReadLine();


                if (input == "サーバーストップ")
                {
                    break;
                }

                switch (input)
                {
                    case "start":
                        {
                            while (true)
                            {
                                Console.WriteLine("シミュレーションの総時間を指定してください.21.5394分なら21.5394と打ってください.分単位です.");
                                Console.WriteLine("標準時間は17.75です.それ以外は残り〇〇秒で表示がおかしくなる可能性あり.1分以上推奨");
                                Console.Write("総時間を入力してください: ");
                                string input2 = Console.ReadLine();

                                // 入力が数値に変換できるか確認
                                if (float.TryParse(input2, out SimulationTimeScale))
                                {
                                    if (SimulationTimeScale > 0)
                                    {
                                        //0以上のfloat値が入力された場合、ループを抜ける
                                        break;
                                    }
                                    else
                                    {
                                        Console.WriteLine("0以下の数値を入力しないでください.");
                                    }

                                }
                                else
                                {
                                    // 数値以外が入力された場合、再入力を促す
                                    Console.WriteLine("無効な入力です。もう一度入力してください。");
                                }
                            }

                            Console.WriteLine("シミュレーションを5秒後に開始します。");
                            for (int i = 5; i >= 0; i--)
                            {
                                Console.Write($"\rカウントダウン: {i}秒");
                                wssv.WebSocketServices["/Notification"].Sessions.Broadcast($"残り{i}秒でシミュレーションを開始します.");
                                Thread.Sleep(1000); // 1秒待機
                            }
                            wssv.WebSocketServices["/Notification"].Sessions.Broadcast("WorkshopSimulationStart");
                            Console.WriteLine("");//改行を行う.
                            Console.WriteLine("シミュレーションを開始しました。");

                            if (TimebroadcastTimer == null)
                            {
                                // タイマーの設定
                                TimebroadcastTimer = new System.Timers.Timer(100); // 0.1秒間隔
                                TimebroadcastTimer.Elapsed += OnTimedEvent;
                                TimebroadcastTimer.AutoReset = true;
                                TimebroadcastTimer.Enabled = true;

                                // ストップウォッチの設定
                                stopwatch = new Stopwatch();
                                stopwatch.Start();

                                // 特定の時刻を設定（11時）
                                //startTime = new DateTime(1997, 7, 1, 11, 0, 0);
                            }

                            break;
                        }

                    case "ユーザーを追加する":
                        {
                            Console.WriteLine("さようなら！");
                            break;
                        }

                    default:
                        {
                            Console.WriteLine("不明なコマンドです。");
                            break;
                        }
                }
            }
            wssv.Stop();
        };

    }

    private static void BroadcastMessage(Object source, ElapsedEventArgs e)
    {
        var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        var broadcastMessage = Encoding.UTF8.GetBytes("WebSocketServerWorkshop:8080");
        udpClient.Send(broadcastMessage, broadcastMessage.Length, new IPEndPoint(IPAddress.Broadcast, 8888));
    }

    private static void OnTimedEvent(Object source, ElapsedEventArgs e)
    {
        // 経過時間を取得
        TimeSpan elapsed = stopwatch.Elapsed;
        stopwatch.Restart(); // ストップウォッチをリセット

        int hour = currentTime.Hour;
        float timeMultiplier;

        if (hour >= 11 && hour < 13)
        {
            timeMultiplier = 30f; // 1時間が実時間2分で進む
        }
        else if (hour >= 13 && hour < 18)
        {
            timeMultiplier = 40f; // 1時間が実時間1.5分で進む
        }
        else if (hour >= 18 && hour < 24)
        {
            timeMultiplier = 60f; // 1時間が実aa時間1分で進む
        }
        else
        {
            timeMultiplier = 240f; // 1時間が15秒で進む
        }
        //シミュレーション総時間に合わせてスケールする
        timeMultiplier *= (float)17.75 / SimulationTimeScale;

        TimeSpan multipliedElapsed = TimeSpan.FromTicks((long)(elapsed.Ticks * timeMultiplier));
        currentTime = currentTime.Add(multipliedElapsed);

        wssv.WebSocketServices["/Timer"].Sessions.Broadcast(currentTime.ToString());
        Console.Write($"\r現在の時刻: {currentTime:HH時mm分ss秒} > ");
    }
}