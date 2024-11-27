using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace test1111.sqlite3_database
{
    class Db//データベースの初期化を行うスタティックなクラス
    {
        string currentGroup = null;
        string groupPattern = @"^\[(.+)\]$";//グループ名の正規表現
        string entryPattern = @"^\s*(.{1,20})=(\d{4})$";//参加者=パスワードの正規表現
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
        public static string connectionString;
        public static int userTableColumnCount;
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

        public static bool CheckTableExists(string connectionString, string tableName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string tableCheckQuery = "SELECT name FROM sqlite_master WHERE type='table' AND name='@tableName'";
                using (var command = new SQLiteCommand(tableCheckQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    var result = command.ExecuteScalar();
                    return result != null;//存在するならtrue,存在しないならfalse
                }
            }
        }

        public static bool CheckNameColumnConstraints(string connectionString,string tableName, string ColumnName, string ColumnType, bool IsUniqu)//ユニークならtrueを指定
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string pragmaQuery = "PRAGMA table_info(@tableName)";
                using (var command = new SQLiteCommand(pragmaQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
                    using (var reader = command.ExecuteReader())
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

        public static void DisplayAllRows(string connectionString, string tableName)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                string selectQuery = "SELECT * FROM @tableName";
                using (var command = new SQLiteCommand(selectQuery, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);
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

        public static void queryExcute(string connectionString, string query, Dictionary<string, string> queryPrams)
        {
            //単に戻り値を必要としないクエリ文を実行する.
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    foreach(var param in queryPrams)
                    {
                        command.Parameters.AddWithValue($"{param.Key}", param.Value);
                    }
                    command.ExecuteNonQuery();
                }
            }
        }

        public static void queryExcute(string connectionString, string query)
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
        public static bool queryScalar(string connectionString, string query, Dictionary<string, string> queryPrams)
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
                    return result != null;//存在するならtrue,存在しないならfalse
                }
            }
        }

        public static T queryScalar<T>(string connectionString, string query, Dictionary<string, string> queryPrams)
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
                    return (T)(object)result;//ジェネリックで指定した型で値を返す.
                }
            }
        }

        public static T queryScalar<T>(string connectionString, string query)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                using (var command = new SQLiteCommand(query, connection))
                {
                    var result = command.ExecuteScalar();
                    return (T)(object)result;//ジェネリックで指定した型で値を返す.
                }
            }
        }


        public static void Run()
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
                if (!File.Exists($"{dbName}.text"))
                {
                    //ファイルが存在しないので再度指定させる
                    Console.WriteLine("データベースが存在しません。再度指定してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (!CheckTableExists($"Data Source={dbName}.db", "user"))//userテーブルが存在するか確認
                {
                    //userテーブルが存在しないので再度指定させる
                    Console.WriteLine("userテーブルが存在しません.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (CheckNameColumnConstraints($"Data Source={dbName}.db", "user", "id", "TEXT", true))
                {
                    //userテーブルのidが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのidが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (CheckNameColumnConstraints($"Data Source={dbName}.db", "user", "group", "TEXT", false))
                {
                    //userテーブルのgroupが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのgroupが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else if (CheckNameColumnConstraints($"Data Source={dbName}.db", "user", "name", "TEXT", true))
                {
                    //userテーブルのnameが存在しないので再度指定させる
                    Console.WriteLine("userテーブルのnameが存在しないか値が不正です.データベースを確認してください");
                    Console.Write("読み込むデータベース名を入力してください: ");
                    dbName = Console.ReadLine();
                }
                else break;

            }
            
            Console.WriteLine("データベースが確認できました。読み込みます。");
            connectionString = $"Data Source={dbName}.db";
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();//データベースを開く
                //userテーブルのデータを表示
                Console.WriteLine("シミュレーションの参加者リストを表示します");
                DisplayAllRows(connectionString,"user");
                userTableColumnCount = queryScalar<int>(connectionString, "select count(*) from user");
                //simulationテーブルがなければ作成する
                if (!CheckTableExists(connectionString, "simulation"))
                {
                    Console.WriteLine("simulationテーブルがないので作成します.");
                    //simulationテーブルを作成する
                    queryExcute(connectionString, "create table simulation(status text, id text primary key, group text not null)");//statusは帰宅遷移状況を表す文字列を表す
                }
                //simulationテーブルを初期化
                string insertQuery = @"
                INSERT INTO simulation (id, group)
                SELECT id, group
                FROM user
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM simulation
                    WHERE simulation.id = user.id
                )";//シミュレーションテーブルにはないが,userテーブルにはあるidをもつレコードをsimulationテーブルに追加
                using (var command = new SQLiteCommand(insertQuery, connection))
                {
                    queryExcute(connectionString,insertQuery);
                }
                //
                string selectQuery = "SELECT id, name, hash FROM user";
                using (var command = new SQLiteCommand(selectQuery, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Console.WriteLine($"ID: {reader["id"]}, Name: {reader["name"]}, Hash: {reader["hash"]}");
                    }
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
        }
    } 