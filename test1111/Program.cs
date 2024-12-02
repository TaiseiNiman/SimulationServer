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
using test1111;
using test1111.sqlite3_database;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

public class snsNotification: WebSocketBehavior
{
    private static List<snsNotification> clients = new List<snsNotification>();
    public static IniReader snsTextFile;
    public static int length;
    public static snsNotification Instance;

    public snsNotification()
    {
        Instance = this;
        int i;
        //snsTextFileの長さを求めておく
        for(i = 1; true; i++)
        {
            if (snsTextFile.GetIniValue(i.ToString(), "Start") == null) break ;
        }
        length = i-1;
    }

    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"snsNotification->New client connected: {clientIp}");

        //Broadcast($"New client connected: {clientIp}");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }

        try
        {
            if (Context != null && Context.UserEndPoint != null)
            {
                string clientIp = Context.UserEndPoint.Address.ToString();
                Console.WriteLine($"snsNotification->client removed: {clientIp}");

                // Broadcast($"client removed: {clientIp}");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"ObjectDisposedException caught: {ex.Message}");
        }
    }

    public void Broadcast(DateTime current)
    {
        string val;
        int hour;
        int minute;
        string message;
        DateTime startTime;
        if (snsTextFile == null) return;//ファイルが読み込みされていないなら常にreturn
        for (int i=length; i >= 1; i--) {
            val = snsTextFile.GetIniValue(i.ToString(),"Start");
            if (val == null) {
                Console.WriteLine("SNSメッセージファイルのセクションやキーが不正です。調べてください");
                Console.WriteLine("形式は[正の整数]Start=hour:minute Message=任意の文字列　でなければなりません");
                break;
            }
            else
            {
                try {
                    hour = int.Parse(snsTextFile.GetIniValue(i.ToString(), "Start").Split(':')[0]);
                    minute = int.Parse(snsTextFile.GetIniValue(i.ToString(), "Start").Split(':')[1]);
                    startTime = new DateTime(1997, 7, 1, hour, minute, 0);
                    if (startTime <= current)
                    {
                        message = snsTextFile.GetIniValue(i.ToString(), "Messages");
                        if (message != null)
                        {
                            Sessions.Broadcast(message);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("Messageの値が不正か存在しません。必ずMessageキーに値を設定してください。");
                            Console.WriteLine("例えば,Messages=今日は幹線道路が大渋滞であることが予測されています。と指定します");
                        }
                        break;
                    }
                }
                catch (FormatException)
                {
                    Console.WriteLine("Startの値が不正です。Start=hour:minuteの形式で書いてください。全て半角です");
                    Console.WriteLine("例えば,11時20分からメッセージを送りたいなら,Start=11:20と指定してください");
                }
                
            }
        }
       
    }
}

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
    private static HashSet<string> _userName;
    public static HashSet<string> userName { get; set; } = new HashSet<string>();


    protected override void OnOpen()
    {
        lock (clients)
        {
            clients.Add(this);
        }

        var clientIp = Context.UserEndPoint.Address.ToString();
        Console.WriteLine($"UserLog->New client connected: {clientIp}");

        
    }

    protected override void OnClose(CloseEventArgs e)
    {
        lock (clients)
        {
            clients.Remove(this);
        }

        try
        {
            if (Context != null && Context.UserEndPoint != null)
            {
                string clientIp = Context.UserEndPoint.Address.ToString();
                Console.WriteLine($"UserLog->client removed: {clientIp}");

                // Broadcast($"client removed: {clientIp}");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"ObjectDisposedException caught: {ex.Message}");
        }
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        string result = Program.database.queryScalar<string>("select name from user where user.id = @data", new Dictionary<string, string> { { "@data", e.Data } });
        if (result != null)
        {
            //ユーザーからのパスワード入力が成功した後の処理
            userName.Add(result);
            if(userEnterCount != userName.Count)
            {
                Send($"{result}:{e.Data}");//参加したユーザーの氏名を送信
                //サーバーに伝える
                Console.WriteLine($"{result}が参加しました.");
                //異なるユーザーからの認証が成功した
                userEnterCount = userName.Count;//参加したユーザーをカウントする

            }
            else
            {
                //同じユーザーからの認証
                Send("user is already joined");
            }
        }
        else
        {
            Console.WriteLine($"だれかがパスワードを間違えたようです.");
            Send("password is incorrect");
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

        try
        {
            if (Context != null && Context.UserEndPoint != null)
            {
                string clientIp = Context.UserEndPoint.Address.ToString();
                Console.WriteLine($"SimulationNotification->client removed: {clientIp}");

                // Broadcast($"client removed: {clientIp}");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"ObjectDisposedException caught: {ex.Message}");
        }
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

        try
        {
            if (Context != null && Context.UserEndPoint != null)
            {
                string clientIp = Context.UserEndPoint.Address.ToString();
                Console.WriteLine($"WebsocketTimer->client removed: {clientIp}");

                // Broadcast($"client removed: {clientIp}");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"ObjectDisposedException caught: {ex.Message}");
        }
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

        try
        {
            if (Context != null && Context.UserEndPoint != null)
            {
                string clientIp = Context.UserEndPoint.Address.ToString();
                Console.WriteLine($"Simulation->client removed: {clientIp}");

                // Broadcast($"client removed: {clientIp}");
            }
        }
        catch (ObjectDisposedException ex)
        {
            Console.WriteLine($"ObjectDisposedException caught: {ex.Message}");
        }
    }
    protected override void OnMessage(MessageEventArgs e)
    {
        string[] parts = e.Data.Split(':');//TEST:userId:111333という形式
        // クライアントからのメッセージを受信した場合の処理
        //認証を行う
        string query = "select user_group from simulation where id = @id";
        if (!Program.database.queryScalar(query, new Dictionary<string, string> { { "@id", parts[1] } })) { Send($"your id is incorrect:{e.Data}"); return; }//認証に失敗しました
        //帰宅遷移状況を表す文字列を認証する
        if (!Program.database.simulationStringCheack(parts[2])) {Send($"your simulation status is incorrect:{e.Data}"); return;}
        //選択された帰宅手段が書き込めるか認証する
        if (!Program.database.simulationSelectCheck(parts[2], parts[1])) {Send($"your selected simulation status is incorrect:{e.Data}");return; }
        if (parts[0] == "TEST")//テスト時
        {
            Send($"your selected simulation status is correct:{e.Data}");//送り返す
        }
        else if (parts[0] == "ACTION")//実際に書き込む
        {
            //帰宅遷移状況を更新
            Program.database.queryExcute("update simulation set status = @status where id = @id", new Dictionary<string, string> { {"@status" ,parts[2]},{ "@id",parts[1]} });
            Send($"your selected simulation status was updated:{e.Data}");
        }
        else
        {
            Send($"Messages are incorrect please <TEST|ACTION>:{e.Data}");
        }
    }

    //帰宅遷移状況を表す文字列が適切か調べる

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

    

}


public class Program
{
    // Win32 APIのSetConsoleCtrlHandler関数の宣言
    [DllImport("Kernel32")]
    private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

    // ハンドラルーチンのデリゲート型
    private delegate bool HandlerRoutine(CtrlTypes CtrlType);

    // ハンドラルーチンに渡される定数の定義
    private enum CtrlTypes
    {
        CTRL_C_EVENT = 0,
        CTRL_CLOSE_EVENT = 2,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT = 6
    }



    private static System.Timers.Timer broadcastTimer;
    private static System.Timers.Timer TimebroadcastTimer;
    private static Stopwatch stopwatch;
    private static DateTime startTime;
    public static TimeSpan elapsed;
    private static DateTime _currentTime = new DateTime(1997, 7, 1, 11, 0, 0);
    public static DateTime currentTime {
        get { return _currentTime; } 
        private set { 
            _currentTime = value;
            //時刻が変化するたびにイベントリスナーを実行する.
            TimerChanged?.Invoke(_currentTime);
        } 
    }
    
    public static event Action<DateTime> TimerChanged;
    private static WebSocketServer wssv; // 静的フィールドとして定義
    public static float SimulationTimeScale;
    public static Db database = new Db();


    private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
    {
        // ウィンドウが閉じられたときの処理
        if (ctrlType == CtrlTypes.CTRL_CLOSE_EVENT)
        {
            Console.WriteLine("コンソールウィンドウを閉じます");
            // 任意の処理をここに追加
            if(database.connectionString != null)
            {
                Console.WriteLine("シミュレーション結果をファイルに書き込みます");
                database.writeSqlite();//ファイルに書き込む
                Console.WriteLine("シュミレーション結果をデータベースから削除します");
                database.queryExcute("drop table simulation");//simulationテーブルを削除
            }
        }
        return true;
    }
    public static void Main(string[] args)
    {
        SetConsoleCtrlHandler(new HandlerRoutine(ConsoleCtrlCheck), true);//ウィンドウクローズ時の処理


        database.Run();//データベースの初期化を行う.
        //snsメッセージファイルを読み込む
        snsNotification.snsTextFile = new IniReader();//初期化
        while (true)
        {
            Console.WriteLine("snsメッセージファイルを読み込みます");
            Console.WriteLine("ファイルパスを指定してください");
            Console.WriteLine("もし不要なら'none'と打ち込んでください");
            string iniPath = Console.ReadLine();
            if (iniPath == "none") break;
            else if (snsNotification.snsTextFile.LoadIniFile(iniPath))
            {
                Console.WriteLine("読み込みに成功しました");
                break;
            }
            else
            {
                Console.WriteLine("読み込みに失敗しました");
            }
        }
        //websocket通信を確立する
        Console.OutputEncoding = Encoding.UTF8;
        wssv = new WebSocketServer("ws://0.0.0.0:8080");
        wssv.Log.Output = (_, __) => { };
        wssv.AddWebSocketService<UserLog>("/UserLog");
        wssv.AddWebSocketService<SimulationNotification>("/Notification");
        wssv.AddWebSocketService<WebsocketTimer>("/Timer");
        wssv.AddWebSocketService<Simulation>("/Simulation");//snsNotification
        wssv.AddWebSocketService<snsNotification>("/SNS");
        wssv.Start();
        Console.WriteLine("WebSocket server started at ws://localhost:8080/UserLog");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Workshop");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Timer");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/Simulation");
        Console.WriteLine("WebSocket server started at ws://localhost:8080/SNS");
        // ブロードキャストメッセージを定期的に送信
        broadcastTimer = new System.Timers.Timer(5000);
        broadcastTimer.Elapsed += BroadcastMessage;
        broadcastTimer.AutoReset = true;
        broadcastTimer.Enabled = true;

        //ユーザーが参加するまで待つ.
        UserLog.userEneterCountChanged += (int count) =>
        {
            if (count != database.userTableColumnCount) return;
            //ユーザーの参加が完了した後の処理
            Console.WriteLine("コマンドを入力してください（サーバーを終了するには 'stop' と入力）");
            Console.WriteLine("コマンドを入力してください（シミュレーションを開始するには 'start' と入力）：");

            while (true)
            {

                string input = Console.ReadLine();


                if (input == "stop")
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
                                Console.WriteLine("総時間を入力してください: ");
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
                            Thread timeThread = null;
                            object lockobj = new object();
                            //タイマーリスナーの設定
                            TimerChanged += (DateTime current) => {
                                
                                if (timeThread == null)//初期化
                                {
                                    timeThread = new Thread(() =>
                                    {
                                        Console.Write($"\r現在の時刻: {currentTime:HH時mm分ss秒}");
                                        Console.WriteLine("");
                                        while (true)
                                        {
                                            lock (lockobj)
                                            {
                                                Console.SetCursorPosition(0, Console.CursorTop - 1);
                                                Console.Write($"\r現在の時刻: {currentTime:HH時mm分ss秒}");
                                                Console.WriteLine("");
                                            }
                                            Thread.Sleep(2000);
                                        }
                                    });
                                    timeThread.IsBackground = true;
                                    timeThread.Start();
                                }
                            };
                            TimerChanged += snsNotification.Instance.Broadcast;
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

                            goto outroop;
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
            outroop:
            Console.WriteLine("参加者全員にメッセージを送信します.コピペ入力してください");
            Console.WriteLine("サーバーを終了したい場合は'stop'と入力してください");
            while (true)
            {
                
                string input = Console.ReadLine();
                if (input == "stop") break;
                else
                {
                    wssv.WebSocketServices["/SNS"].Sessions.Broadcast(input);
                }
            }
            wssv.Stop();
        };

        // メインスレッドをブロックする
        Console.WriteLine("アプリケーションを終了するには 'exit' と入力してください。");
        while (true)
        {
            string input = Console.ReadLine();
            if (input == "exit")
            {
                break;
            }
        }

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
        
    }
}