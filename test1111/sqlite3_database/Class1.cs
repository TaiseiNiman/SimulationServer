using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace test1111.sqlite3_database
{
    public class Db//データベースを用いて帰宅遷移状況を保存するクラス
    {
        private string currentGroup = null;
        public string groupPattern { get; private set; }//グループ名の正規表現
        public string entryPattern { get; private set; }//参加者=パスワードの正規表現
        public string KitakuStatus { get; private set; }//帰宅遷移状況(111333など)の文字列の正規表現
        public string connectionString { get; private set; }//データベース名
        public int userTableColumnCount { get; private set; }//userテーブルのカラム数
        //クエリ
        public Dictionary<string, string> query { get; private set; }

        public string resultPath;
        //参加者リストに不正がないかチェックする
        /*
         * [グループ1(グループ名)]
         * 参加者1(氏名)=パスワード1(4桁の数字)
         * 参加者2(氏名)=パスワード2(4桁の数字)
         * ,..
         * [グループ名2(グループ名)]
         * 参加者1(氏名)=パスワード1(4桁の数字)
         * 参加者2(氏名)=パスワード2(4桁の数字)
         * ,..
         * [グループ3(グループ名)]
         * ,...
         * の形式で指定すること
         * それ以外は不正になります.
         * 
         */
        //
        public Db()
        {
            //デフォルト
            groupPattern = @"^\[(.+)\]$";
            entryPattern = @"^\s*(.{1,20})=(\d{4})$";
            KitakuStatus = @"^1{1,14}[2-9]{0,13}$";
            connectionString = string.Empty;//runメソッドで初期化を行う
            userTableColumnCount = 0;
        }


        public bool simulationStringCheack(string status)
        {

            if (Regex.IsMatch(status, KitakuStatus))//帰宅遷移状況を表す文字列形式になっているか確認
            {
                //形式は妥当なので、次の処理を勧める
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool simulationSelectCheck(string status, string id)//書き込めるならtrueを書き込めないならfalseを返す
        {

            string lastSelect = status[status.Length - 1].ToString();
            int simulationTime = status.Length + 9;
            if (lastSelect == "1")
            {
                string query2 = "SELECT count(*) FROM simulation JOIN user ON simulation.user_group = user.user_group WHERE user.id = @id AND (simulation.status LIKE '%1' and simulation.id != @id and length(simulation.status) = cast(@length as int))";
                int count = queryScalar<int>(query2, new Dictionary<string, string> { { "@id", id }, { "@length", status.Length.ToString() } });
                //simulationTime = status.Length + 9;
                if (Regex.IsMatch(simulationTime.ToString(), "^(13)$"))
                {
                    if ((count == 3)) return false;
                }
                if (Regex.IsMatch(simulationTime.ToString(), "^(16)$"))
                {
                    if ((count == 1)) return false;
                }
                /*if (Regex.IsMatch(simulationTime.ToString(), "^(23)$"))
                {
                    if ((count == 1)) return false;
                }
                */
                //何もなければtrueを返す
                return true;
            }
            else if (Regex.IsMatch(lastSelect, "^(2|3|4|5|6)$"))
            {
                string query2 = "SELECT count(*) FROM simulation JOIN user ON simulation.user_group = user.user_group WHERE user.id = @id AND (simulation.status not LIKE '%1' and simulation.id != @id)";
                int count = queryScalar<int>(query2, new Dictionary<string, string> { { "@id", id }, { "@length", status.Length.ToString() } });
                //simulationTime = status.Length + 9;
                string query = "SELECT simulation.id FROM simulation JOIN user ON simulation.user_group = user.user_group WHERE user.id = @id AND (simulation.status IS NOT NULL AND simulation.status LIKE @lastSelect and simulation.id != @id)";

                //まず同じ帰宅手段を選んでいる人が同一グループに居ないか調べる
                if (queryScalar(query, new Dictionary<string, string> { { "@id", id }, { "@lastSelect", $"%{lastSelect}" } })) return false;
                //

                if (Regex.IsMatch(simulationTime.ToString(), "^(11|12|13)$"))
                {

                    if (!(count + 1 <= 1)) return false;

                }
                else if (Regex.IsMatch(simulationTime.ToString(), "^(|14|15|16)$"))
                {
                    if (!Regex.IsMatch(lastSelect, "^(3)$"))
                    {
                        //3を除いてカウントする.
                        query2 = "SELECT count(*) FROM simulation JOIN user ON simulation.user_group = user.user_group WHERE user.id = @id AND (simulation.status not LIKE '%1' and simulation.status not LIKE '%3' and simulation.id != @id)";
                        count = queryScalar<int>(query2, new Dictionary<string, string> { { "@id", id }, { "@length", status.Length.ToString() } });

                        if (!(count + 1 <= 2)) return false;
                    }
                }
                else if (Regex.IsMatch(simulationTime.ToString(), "^(17|18|19|20|21|22|23)$"))
                {
                    if (!(count + 1 <= 4)) return false;
                }

                if (Regex.IsMatch(lastSelect, "^(6)$"))
                {
                    if (simulationTime >= 14)
                    {
                        return !queryScalar("select id from simulation where status like '%6' and id != @id", new Dictionary<string, string> { { "@id", id } });
                    }
                    else return false;

                }

                if (Regex.IsMatch(lastSelect, "^(3)$"))
                {
                    if (simulationTime <= 13 || simulationTime >= 17)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                //何もなければtrueを返す
                return true;
            }

            else return false;
        }

        public static void UniqueCheck(string filePath)
        {

            var lines = File.ReadAllLines(filePath);

            HashSet<string> uniqueStrings = new HashSet<string>();
            HashSet<int> uniqueNumbers = new HashSet<int>();
            HashSet<string> uniqueGroups = new HashSet<string>();

            string groupPattern = @"^\[(.+)\]$";
            string entryPattern = @"^(.{1,20})=(\d{4})$";

            foreach (var line in lines)
            {
                if (Regex.IsMatch(line, groupPattern))
                {
                    var match = Regex.Match(line, groupPattern);
                    string groupName = match.Groups[1].Value;

                    if (!uniqueGroups.Add(groupName))
                    {
                        Console.WriteLine($"重複したグループ名: {groupName}");
                        Console.WriteLine($"参加者リストを修正してください");
                    }
                }
                else if (Regex.IsMatch(line, entryPattern))
                {
                    var match = Regex.Match(line, entryPattern);
                    string str = match.Groups[1].Value;
                    int number = int.Parse(match.Groups[2].Value);

                    if (!uniqueStrings.Add(str))
                    {
                        Console.WriteLine($"重複した文字列: {str}");
                        Console.WriteLine($"参加者リストを修正してください");
                    }

                    if (!uniqueNumbers.Add(number))
                    {
                        Console.WriteLine($"重複した数値: {number}");
                        Console.WriteLine($"参加者リストを修正してください");
                    }
                }
            }

            Console.WriteLine("読み込んだ参加者リストは不正がありませんでした。");
        }

        public bool CheckTableExists(string tableName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string tableCheckQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name=@tableName";
                using (var command = new SQLiteCommand(tableCheckQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    var result = command.ExecuteScalar();
                    return result != null;//存在するならtrue,存在しないならfalse
                }
            }
        }

        public bool CheckNameColumnConstraints(string tableName, string ColumnName, string ColumnType, bool IsUniqu)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string pragmaQuery = $"PRAGMA table_info({tableName})";
                using (var pragmaCommand = new SQLiteCommand(pragmaQuery, connection))
                {
                    using (var reader = pragmaCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["name"].ToString();
                            string columnType = reader["type"].ToString();
                            bool isUnique = reader["pk"].ToString() == "1"; // Primary key implies uniqueness

                            if (columnName == ColumnName && columnType == ColumnType && isUnique == IsUniqu)
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        public void DisplayAllRows(string tableName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = $"SELECT * FROM {tableName}";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        // カラム名を取得して表示
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            Console.Write("{0,-20}", reader.GetName(i));
                        }
                        Console.WriteLine();
                        Console.WriteLine(new string('-', 20 * reader.FieldCount));

                        // 各行のデータを表示
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                Console.Write("{0,-20}", reader[i].ToString());
                            }
                            Console.WriteLine();
                        }
                    }
                }
            }
        }

        public void queryExcute(string query, Dictionary<string, string> queryPrams)
        {
            //単に戻り値を必要としないクエリ文を実行する.
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    foreach (var param in queryPrams)
                    {
                        command.Parameters.AddWithValue($"{param.Key}", param.Value);
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        public void queryExcute(string query)
        {
            //単に戻り値を必要としないクエリ文を実行する. クエリパラメータなし
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        //クエリの結果からレコードを一つでも返すならtrue,そうでないならfalse
        //※count(*)は使うな、必ずカラムを指定してレコードが存在するかを調べること
        public bool queryScalar(string query, Dictionary<string, string> queryPrams)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    foreach (var param in queryPrams)
                    {
                        command.Parameters.AddWithValue($"{param.Key}", param.Value);
                    }
                    var result = command.ExecuteScalar();
                    //Console.WriteLine($"Query result: {result}");
                    return result != null && !(result is DBNull);// 存在するならtrue,存在しないならfalse
                }
            }
        }

        public bool queryScalar(string query)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();//varだと文字列や数値を認識できても,コレクションなどでは型推論二失敗し空文字列が渡されてしまう.
                    return result != null;//存在するならtrue,存在しないならfalse
                }
            }
        }


        public T queryScalar<T>(string query, Dictionary<string, string> queryPrams)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    foreach (var param in queryPrams)
                    {
                        command.Parameters.AddWithValue($"{param.Key}", param.Value);
                    }
                    var result = command.ExecuteScalar();
                    if (result == null || result is DBNull)
                    {
                        return default(T);
                    }

                    try
                    {
                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException($"Cannot convert result to type {typeof(T)}.");
                    }
                }
            }
        }

        public T queryScalar<T>(string query)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    if (result == null || result is DBNull)
                    {
                        return default(T);
                    }

                    try
                    {
                        return (T)Convert.ChangeType(result, typeof(T));
                    }
                    catch (InvalidCastException)
                    {
                        throw new InvalidCastException($"Cannot convert result to type {typeof(T)}.");
                    }
                }
            }
        }


        public void Run()
        {
            /*
            //参加者リストを確認します.
            Console.Write("ワークショップに用いる参加者リストのファイル名を指定: ");
            string userListName = Console.ReadLine();
            while (!File.Exists($"{userListName}.text"))
            {
                //ファイルが存在しないので再度指定させる
                Console.WriteLine("指定されたファイル名の参加者リストが存在しません。もう一度指定してください");
                Console.Write("ワークショップに用いる参加者リストのファイル名を指定: ");
                userListName = Console.ReadLine();
                    
            }
            //参加者リストに不正があるか確認します.
            */

            Console.Write("読み込むデータベース名を入力してください: ");
            string dbName = Console.ReadLine();
            while (true)
            {
                connectionString = $"Data Source={dbName}";
                if (!File.Exists(dbName))
                {
                    //ファイルが存在しないので再度指定させる
                    Console.WriteLine("データベースが存在しません。再度指定してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (!CheckTableExists("user"))//userテーブルが存在するか確認
                {
                    //userテーブルが存在しないので再度指定させる
                    Console.WriteLine("userテーブルが存在しません.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (!CheckNameColumnConstraints("user", "id", "TEXT", true))
                {
                    //userテーブルのidが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのidが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (!CheckNameColumnConstraints("user", "user_group", "TEXT", false))
                {
                    //userテーブルのgroupが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのgroupが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (!CheckNameColumnConstraints("user", "name", "TEXT", false))
                {
                    //userテーブルのnameが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのnameが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else break;

            }

            Console.WriteLine("データベースが確認できました。読み込みます。");

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();//データベースを開く
                //userテーブルのデータを表示
                Console.WriteLine("シミュレーションの参加者リストを表示します");
                DisplayAllRows("user");
                userTableColumnCount = queryScalar<int>("select count(*) from user");
                //simulationテーブルがなければ作成する
                if (!CheckTableExists("simulation"))
                {
                    Console.WriteLine("simulationテーブルがないので作成します.");
                    //simulationテーブルを作成する
                    queryExcute("create table simulation(status text, id text primary key, user_group text not null)");//statusは帰宅遷移状況を表す文字列を表す
                }
                //simulationテーブルを初期化
                string insertQuery = @"
                INSERT INTO simulation (id, user_group)
                SELECT id, user_group
                FROM user
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM simulation
                    WHERE simulation.id = user.id
                )";//シミュレーションテーブルにはないが,userテーブルにはあるidをもつレコードをsimulationテーブルに追加
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    queryExcute(insertQuery);
                }
                //書き込むファイルを指定
                while (true)
                {
                    Console.WriteLine("シミュレーションの結果を書き込むテキストファイルのパスを指定してください");
                    Console.WriteLine("シミュレーション結果を保存する必要がないなら'none'と打ってください");
                    resultPath = Console.ReadLine();
                    if (resultPath == "none") { resultPath = null; break; }
                    else if (File.Exists(resultPath)) break;
                    else Console.WriteLine("指定したパスのテキストファイルは存在しません");
                }

            }

        }
        static void CreateDatabase(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // テーブルの作成
                string createTableQuery = "CREATE TABLE IF NOT EXISTS user (id INTEGER PRIMARY KEY, name TEXT NOT NULL, hash TEXT NOT NULL)";
                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }

                // データの挿入
                InsertUser(connection, "Alice");
                InsertUser(connection, "Bob");
                InsertUser(connection, "Charlie");

                connection.Close();
            }

            Console.WriteLine("新しいデータベースを作成し、データを挿入しました。");
        }

        static void LoadDatabase(string connectionString)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // データの確認
                string selectQuery = "SELECT id, name, hash FROM user";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["id"]}, Name: {reader["name"]}, Hash: {reader["hash"]}");
                    }
                }

                connection.Close();
            }

            Console.WriteLine("既存のデータベースを読み込みました。");
        }

        static void InsertUser(SQLiteConnection connection, string name)
        {
            string hash = ComputeSha256Hash(name);
            string insertDataQuery = "INSERT INTO user (name, hash) VALUES (@name, @hash)";
            using (var command = new SQLiteCommand(insertDataQuery, connection))
            {
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@hash", hash);
                command.ExecuteNonQuery();
            }
        }

        static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public void writeSqlite()
        {
            if (resultPath == null) return;
            //string connectionString = "Data Source=your_database.db;Version=3;";
            string query = "SELECT * FROM simulation";

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                using (SQLiteDataReader reader = command.ExecuteReader())
                using (StreamWriter writer = new StreamWriter(resultPath))
                {
                    // カラム名を書き込む
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        writer.Write(reader.GetName(i));
                        if (i < reader.FieldCount - 1)
                            writer.Write("\t");
                    }
                    writer.WriteLine();

                    // データを書き込む
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            writer.Write(reader[i].ToString());
                            if (i < reader.FieldCount - 1)
                                writer.Write("\t");
                        }
                        writer.WriteLine();
                    }
                }
            }

            Console.WriteLine("データの書き込みが完了しました。");
        }
    }
}