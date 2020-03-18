
using System;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Telegram.Bot;
using Telegram.Bot.Args;

namespace ClientTCPbot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static TelegramBotClient client;
        static string pathDB= "ID_Chat_PersonDB.sqlite";
        static string TextMessage="Hello there";
        public MainWindow()
        {
            InitializeComponent();
            string smth = ConfigurationSettings.AppSettings.Get("TeleBotClientKey");
            client = new TelegramBotClient(smth);
            client.OnMessage+=AnswerTheQuestion;
            client.StartReceiving();
            if (!CheckExistDataBase(pathDB))
            {
                CreateDataBase(pathDB);
                UserBox.Items.Add(new CheckBox());
                (UserBox.Items[0] as CheckBox).Content = "Smth";
            }
            else
            {

                LoadUserListFromDB(pathDB);
            }
    



        }
     
        private void LoadUserListFromDB(string pathToDB)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT [UserName] FROM UserConfig",connection))
                {
                    try
                    {

                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {

                            ////
                            ////Предусмотреть отсутсттвие USERNAME
                            ////

                            while (reader.Read())
                            {
                                
                                UserBox.Items.Add(new CheckBox());
                                (UserBox.Items[UserBox.Items.Count-1] as CheckBox).Content = reader.GetValue(0).ToString();
                            }
                            ////
                            ////Предусмотреть отсутсттвие USERNAME
                            ////

                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, ex.Message);
                    }

                }
            }
        }




        private void AnswerTheQuestion(object sender, MessageEventArgs e)
        {
            try
            {
                string UserName = e.Message.Chat.Username;
                if (e.Message.Text == "/start")
                {

                    AddChatIDTableForDataBase(Convert.ToInt32(e.Message.Chat.Id), pathDB);
          
                    string un = e.Message.Chat.Username; ;
                    int ID = GetCurentIDInDBTables(pathDB);

                    AddUserConfigInDataBase(un, ID, pathDB);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }

        static int GetCurentIDInDBTables(string pathToDB )
        {
            int result = -1;
            try
            {
               
                using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
                {
                    connection.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand("select seq from sqlite_sequence where name='ChatIDTable'", connection))
                    {

                        using (SQLiteDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                MessageBox.Show(reader.GetInt32(0).ToString(),"");
                                result = reader.GetInt32(0); }
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message,ex.Message);

                } 
            return result;
        }

        



        static public void SendMessagesToAll(string pathToDB)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT * FROM ChatIDTable", connection))
                {
                    
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MessageBox.Show(reader.GetValue(1).ToString(),"Smth");
                            SendMessgeToUser(reader.GetInt32(1));
                        }
                    }
                }
            }

        }



        static public void SendMassageToConcreteUsers(ListBox UserBoxTmp)
        {
            try
            {
                foreach (CheckBox item in UserBoxTmp.Items)
                {
                    if (item.IsChecked == true)
                    {
                        SendMessgeToUser(GetUserChatID(pathDB, item.Content.ToString()));
                    }
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }


        static public int GetUserChatID(string pathToDB,string UserName)
        {
            int IDChat = -1;
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand($"SELECT UserConfig.UserName,ChatIDTable.ChatsID,UserConfig.[IDChatIDTable] FROM UserConfig UserConfig Inner Join ChatIDTable ChatIDTable ON ChatIDTable.ID=UserConfig.[IDChatIDTable] WHERE UserConfig.[UserName]='{UserName}'", connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                          
                            MessageBox.Show(reader.GetValue(1).ToString(), "Smth");
                            IDChat = reader.GetInt32(1);
                        }
                    }
                }
            }
            return IDChat;
        }

        static public void SendMessgeToUser(int ChatID)
        {
            client.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(ChatID),TextMessage);
        }






        private static bool CheckExistDataBase(string path) => File.Exists(path);
        private static void CreateDataBase(string path)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS ChatIDTable" +
                     "([id] INTEGER PRIMARY KEY AUTOINCREMENT," +
                     "[ChatsID] INTEGER NOT NULL);", connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }

                using (SQLiteCommand command = new SQLiteCommand("CREATE TABLE IF NOT EXISTS UserConfig" +
                       "([id] INTEGER PRIMARY KEY AUTOINCREMENT," +
                       "[UserName] VARCHAR(255) NOT NULL," +
                       "[IDChatIDTable] INTEGER," +
                       "FOREIGN KEY(IDChatIDTable) REFERENCES ChatIDTable(ID));", connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }




        private static void AddChatIDTableForDataBase(int chat_id, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path_to_db}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"INSERT INTO ChatIDTable([ChatsID]) VALUES ({chat_id})", connection))
                {
                    try
                    {
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }


        private static void AddUserConfigInDataBase( string username, int idChatIDTable, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path_to_db}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"INSERT INTO UserConfig([UserName],[IDChatIDTable]) VALUES ('{username}',{idChatIDTable})", connection))
                {
                    try
                    {
                        //command.Parameters.Add(new SQLiteParameter("@text", question));
                        command.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SendMassageToConcreteUsers(UserBox);
        }
    }
}
