
using System;
using System.Configuration;
using System.Data.SQLite;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Telegram.Bot;
using Telegram.Bot.Args;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;

namespace ClientTCPbot
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<BoolStringClass> TheList { get; set; }
        static TelegramBotClient client;
        static string pathDB= "ID_Chat_PersonDB.sqlite";
        static string TextMessage="Hello there";
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                TheList = new ObservableCollection<BoolStringClass>();

                TheList.CollectionChanged += TheList_CollectionChanged;
                string smth = ConfigurationSettings.AppSettings.Get("TeleBotClientKey");
                client = new TelegramBotClient(smth);
                client.OnMessage += AnswerTheQuestion;
                client.StartReceiving();
                if (!CheckExistDataBase(pathDB))
                {
                    CreateDataBase(pathDB);

                }
                else
                {

                    LoadUserListFromDB(pathDB);
                }



                //TheList.Add(new BoolStringClass { IsSelected = true, TheText = "Some text for item #1" });
                //TheList.Add(new BoolStringClass { IsSelected = false, TheText = "Some text for item #2" });
                //TheList.Add(new BoolStringClass { IsSelected = false, TheText = "Some text for item #3" });
                //TheList.Add(new BoolStringClass { IsSelected = false, TheText = "Some text for item #4" });
                //TheList.Add(new BoolStringClass { IsSelected = false, TheText = "Some text for item #5" });
                //TheList.Add(new BoolStringClass { IsSelected = true, TheText = "Some text for item #6" });
                //TheList.Add(new BoolStringClass { IsSelected = false, TheText = "Some text for item #7" });

                foreach (var item in TheList)
                    item.PropertyChanged += TheList_Item_PropertyChanged;

                this.DataContext = this;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void LoadUserListFromDB(string pathToDB)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT [UserName] FROM UserConfig", connection))
                {

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {


                        try
                        {

                            while (reader.Read())
                            {
                               TheList.Add(new BoolStringClass { IsSelected = false, TheText = reader.GetValue(0).ToString() });
                                UserBox.Items.Add(new CheckBox());
                                (UserBox.Items[UserBox.Items.Count - 1] as CheckBox).Content = reader.GetValue(0).ToString();
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, ex.Message);
                        }

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
                    if (un == null)
                    {
                        un = e.Message.Chat.Id.ToString();
                    }
                    AddUserConfigInDataBase(un, ID, pathDB);
                    


                    Dispatcher.BeginInvoke(new Action(delegate { TheList.Add(new BoolStringClass { IsSelected = false, TheText = un.ToString() }); UserBox.Items.Add(new CheckBox()); (UserBox.Items[UserBox.Items.Count - 1] as CheckBox).Content = un.ToString(); }));

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }


        


        static int GetCurentIDInDBTables(string pathToDB)
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
                                MessageBox.Show(reader.GetInt32(0).ToString(), "");
                                result = reader.GetInt32(0);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);

            }
            return result;
        }





        public void SendMessagesToAll(string pathToDB)
        {
            try
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
                                MessageBox.Show(reader.GetValue(1).ToString(), "Smth");
                                SendMessgeToUser(reader.GetInt32(1));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }

        }



        public void SendMassageToConcreteUsers(ListBox UserBoxTmp)
        {
            try
            {
                foreach (BoolStringClass item in TheList)
                {
                    if (item.IsSelected == true)
                    {
                        SendMessgeToUser(GetUserChatID(pathDB, item.TheText.ToString()));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }


        static public int GetUserChatID(string pathToDB, string UserName)
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

        public void SendMessgeToUser(int ChatID)
        {
            client.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(ChatID), TxtBx.Text);
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


        private static void AddUserConfigInDataBase(string username, int idChatIDTable, string path_to_db)
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
        void TheList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //foreach (BoolStringClass item in e.NewItems)
            //    UnselectOtherItems(item);

            //ЧТОТО ПЛОХОЕ
        }

        void TheList_Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //UnselectOtherItems((BoolStringClass)sender);

            //ЧТОТО ПЛОХОЕ
        }


        void UnselectOtherItems(BoolStringClass TheChangedItem)
        {
            //if (TheChangedItem.IsSelected)
            //{
            //    var OtherSelectedItems =
            //        TheList.Where(
            //                i => !ReferenceEquals(i, TheChangedItem)
            //            ).AsEnumerable<BoolStringClass>();

            //    foreach (BoolStringClass item in OtherSelectedItems)
            //        item.IsSelected = false;
            //}

            //ЧТОТО ПЛОХОЕ
        }


        ///select all
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SelectAll(true);
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SelectAll(false);
        }

        private void SelectAll(bool select)
        {
            var res = (
                        from item in TheList
                        select item
                    ).ToList<BoolStringClass>();

            if (res != null)
            {
                foreach (var source in res)
                {
                    source.IsSelected = select;
                }
            }
        }
    }

    public class BoolStringClass : INotifyPropertyChanged
    {
        public string TheText { get; set; }

        private bool _fIsSelected = false;
        public bool IsSelected
        {
            get { return _fIsSelected; }
            set
            {
                _fIsSelected = value;
                this.OnPropertyChanged("IsSelected");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string strPropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(strPropertyName));
        }

    }
}
