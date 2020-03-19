
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
        //Ссылка на бота 
        // @Toyohisa_bot
        // @Toyohisa_bot
        public ObservableCollection<BoolStringClass> TheList { get; set; }
        static TelegramBotClient client;
        static string pathDB= "ID_Chat_PersonDB.sqlite";
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

                foreach (var item in TheList)
                    item.PropertyChanged += TheList_Item_PropertyChanged;

                this.DataContext = this;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }
        // Функция загружает через sqlite пользователей которые добавили бота на экран.
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



        //Те пользователи которые запустили бота отправляют start и их добавляют в базу данных 
        private void AnswerTheQuestion(object sender, MessageEventArgs e)
        {
            try
            {
                string UserName = e.Message.Chat.Username;
                if (e.Message.Text == "/start" && !IsClientInDB(pathDB, Convert.ToInt32(e.Message.Chat.Id)))
                {
                    AddChatIDTableForDataBase(Convert.ToInt32(e.Message.Chat.Id), pathDB);

                    string un = e.Message.Chat.Username; ;
                    int ID = GetCurentIDInDBTables(pathDB);
                    if (un == null)
                    {
                        un = e.Message.Chat.Id.ToString();
                    }
                    AddUserConfigInDataBase(un, ID, pathDB);
                    


                    Dispatcher.BeginInvoke(new Action(delegate { TheList.Add(new BoolStringClass { IsSelected = false, TheText = un.ToString() }); }));

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }
        //Проверка на наличие пользователя в базе данных, дабы не добавлять пользователей которые пишут старт уже будучи в базе данных
        public bool IsClientInDB(string pathToDB,int IdChat)
        {
            bool tmp= false;

            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand("SELECT [ChatsID] FROM ChatIDTable", connection))
                {

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        try
                        {

                            while (reader.Read())
                            {
                               if (reader.GetInt32(0)== IdChat)
                               {
                                    tmp = true;
                               }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, ex.Message);
                        }

                    }
                }
            }
            return tmp;
        }
        

        //Получения id чата из базы данных
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




        //Отправка сообщения всем пользователям 
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


        //Отправка сообщения конкретным пользователям 
        public void SendMassageToConcreteUsers()
        {
            try
            {
                foreach (BoolStringClass item in TheList)
                {
                    if (item.IsSelected == true)
                    {
                        int tmpK = GetUserChatID(pathDB, item.TheText.ToString());
                        if (tmpK > 0)
                        {
                            SendMessgeToUser(tmpK);
                        }
                        else
                        {
                            MessageBox.Show("Chat ID not Found!","DataBase Error");
                        }
                      
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, ex.Message);
            }
        }

        //Получение имени пользователя, если оно есть
        static public int GetUserChatID(string pathToDB, string UserName)
        {
            int IDChat = -1;
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source={pathToDB}"))
            {
                connection.Open();
                using (SQLiteCommand command = new SQLiteCommand
                    ($"SELECT UserConfig.UserName,ChatIDTable.ChatsID,UserConfig.[IDChatIDTable] FROM UserConfig UserConfig Inner " +
                    $"Join ChatIDTable ChatIDTable ON ChatIDTable.ID=UserConfig.[IDChatIDTable] WHERE UserConfig.[UserName]='{UserName}'",
                    connection))
                {
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {

                            IDChat = reader.GetInt32(1);
                        }
                    }
                }
            }
            return IDChat;
        }
        //Отправка сообщения простому пользователю
        public void SendMessgeToUser(int ChatID)
        {
            try
            {
                client.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(ChatID), TxtBx.Text);
                TxtBx.Text = String.Empty;
            }
            catch(Exception ex)
            {

                MessageBox.Show(ex.Message); 
            }
        }





        //Проверка на начие нужных таблиц 
        //При их отсутствии функция создаст новые 
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
                        MessageBox.Show(ex.Message);
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



        //Добавление в таблицу в id чатов новый чат
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
                        MessageBox.Show(ex.Message);
                    }
                }
            }
        }

        //Добавление в таблицу в пользователей нового пользователя
        private static void AddUserConfigInDataBase(string username, int idChatIDTable, string path_to_db)
        {
            using (SQLiteConnection connection = new SQLiteConnection($"Data Source = {path_to_db}"))
            {
                connection.Open();

                using (SQLiteCommand command = new SQLiteCommand($"INSERT INTO UserConfig([UserName],[IDChatIDTable]) VALUES ('{username}',{idChatIDTable})", connection))
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
        //Отправка сообщения
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (CheckForSelectAll.IsChecked == true)
            {
                TmpHistory.Text += Environment.NewLine + TxtBx.Text+">> TO ALL";
                SendMessagesToAll(pathDB);
                
            }
            else
            {
                TmpHistory.Text += Environment.NewLine + TxtBx.Text;
                SendMassageToConcreteUsers();
            }
        }
        void TheList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //foreach (BoolStringClass item in e.NewItems)
            //    UnselectOtherItems(item);

            //ЧТО-ТО ПЛОХОЕ(но пока не убирать)
        }

        static bool IsSelectedAllAlready = false;
        void TheList_Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (CheckForSelectAll.IsChecked == true && (sender as BoolStringClass).IsSelected == false)
            {
                IsSelectedAllAlready = true;
                CheckForSelectAll.IsChecked = false;
            }

            //ЧТО-ТО ПЛОХОЕ(но пока не убирать)
        }

        ///select all
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
          
            SelectAll(true);
            
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!IsSelectedAllAlready)
            {
                SelectAll(false);
            }
            IsSelectedAllAlready = false;


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
