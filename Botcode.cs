using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Threading.Tasks;
using System.IO;
using Npgsql;
using Telegram.Bot.Types.InputFiles;

namespace ExpertBot
{
    internal class Program
    {
        
        public static string? name;
        
        public static List<string> args = new List<string> { "Edwardsooon", "polina_kuvaleva", "kirill_sheremetev", "Beauty", "MegaMind", "RiverSpider" };
        public static List<string> moders = new List<string>();
        public static List<string> experts = new List<string>();
        public static List<string> workers = new List<string>();
        public static SortedSet<long> ChatIDs = new();
        static void Main(string[] args)
        {
            
            
            string connectionString = ConfigurationManager.ConnectionStrings["MyConnectionString"].ConnectionString;
            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT moder, expert, worker FROM users";
                using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                {
                    using (NpgsqlDataReader reader = command.ExecuteReader())
                    {
                        

                        while (reader.Read())
                        {
                            string[] moderValues = (string[])reader.GetValue(0); 
                            string[] expertValues = (string[])reader.GetValue(1);
                            string[] workerValues = (string[])reader.GetValue(2);
                            moders.AddRange(moderValues);
                            experts.AddRange(expertValues);
                            workers.AddRange(workerValues);


                        }
                    }
                }
            }

            var client =new TelegramBotClient("5844793789:AAEx-E8FfHw9SBbJuMroR94npIv-J2rI0_E");
            client.StartReceiving(Update,Error);
            Console.ReadLine();

        }

        static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
        {
            throw new NotImplementedException();
        }
        
        async static Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
        {
            var message = update.Message;

            if (message != null)
            {
                if (message.Text != null)
                {
                    Console.WriteLine(message.Text);

                    if (message.Text.ToLower() == "/start")
                    {
                        if (moders.Contains(message.Chat.Username))
                        {
                            ChatIDs.Add(message.Chat.Id);
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите вашу роль\n1.Эксперт - введите /expert\n2.Проверяющий - введите /checker");
                    }

                    if (message.Text.ToLower() == "/expert" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                        "Как эксперт вы можете:\n" + 
                        "Просмотреть каталоги: /directory_watch\n" +
                        "Просмотреть документы: /docx_watch\n" + 
                        "Зайти как проверяющий: /checker");//?
                    }

                    if (message.Text.ToLower() == "/checker" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, 
                        "Как проверяющий вы можете:\n" + 
                        "Добавить каталог: /add_directory\n" +
                        "Просмотреть каталоги: /directory_watch\n" +
                        "Удалить каталог: /directory_delete\n" + 
                        "Добавить документ: /add_docx\n" +
                        "Просмотреть документы: /docx_watch");
                    }

                    if (message.Text.ToLower() == "/docx_watch" && ChatIDs.Contains(message.Chat.Id))
                    {
                        StreamReader StreamR = new StreamReader("C:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt");
                        string Reply = "";
                        if (Convert.ToInt32(StreamR.ReadLine()) == 0)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "В данный момент в системе нет файлов");
                        }
                        else
                        {
                            for (int i = 0; i < Convert.ToInt32(StreamR.ReadLine()); i++)
                            {
                                string Str = StreamR.ReadLine();
                                Reply = Reply + Convert.ToString(i + 1) + ". " + Str + "\n";
                            }
                            StreamR.Close();
                            Reply = "Вам доступно " + Convert.ToString(Convert.ToInt32(StreamR.ReadLine())) + " файлов.\n" + Reply;
                            await botClient.SendTextMessageAsync(message.Chat.Id, Reply);
                            await botClient.SendTextMessageAsync(message.Chat.Id, 
                            "Для скачивания файла введите: /docx_download [number_docx]\n" +
                            "Для удаления файла введите: /docx_delete [number_docx]");
                        }
                    }

                    if (message.Text.ToLower() == "/add_docx" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Отправьте файл");
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/docx_delete") && (message.Text.ToLower().Split(" ").Length == 2) && ChatIDs.Contains(message.Chat.Id))
                    {
                        StreamReader StreamR = new StreamReader("C:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt");
                        int count = Convert.ToInt32(StreamR.ReadLine());
                        string Reply = StreamR.ReadToEnd();
                        StreamR.Close();
                        string file_name = Reply.Split("\n")[Convert.ToInt32(message.Text.ToLower().Split(" ")[1]) - 1];
                        Reply = Reply.Replace("\n" + file_name, "");
                        StreamWriter StreamW = new StreamWriter("C:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt", false);
                        StreamW.Write(Convert.ToString(count - 1) + "\n" + Reply);
                        StreamW.Close();
                        FileInfo fileInfo = new FileInfo("C:\\Users\\Полина\\source\\repos\\ExpertBot\\" + file_name);
                        fileInfo.Delete();
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Файл удалён");
                    }
                    
                    if ((message.Text.ToLower().Split(" ")[0] == "/docx_download") && (message.Text.ToLower().Split(" ").Length == 2) && ChatIDs.Contains(message.Chat.Id))
                    {

                        StreamReader StreamR = new StreamReader("C:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt");
                        string Reply = StreamR.ReadToEnd();
                        StreamR.Close();
                        string file_name = Reply.Split("\n")[Convert.ToInt32(message.Text.ToLower().Split(" ")[1])];
                        await using Stream stream = System.IO.File.OpenRead("C:\\Users\\Полина\\source\\repos\\ExpertBot\\" + file_name);
                        await botClient.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, file_name));

                    }

                    if (message.Text.ToLower() == "/add_directory" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Для создания калатога введите: /n/add_directory [name_directory]");
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/add_directory") && (message.Text.ToLower().Split(" ").Length == 2) && ChatIDs.Contains(message.Chat.Id))
                    {   
                        string path = "C:\\Users\\Полина\\source\\repos\\ExpertBot\\Bot_directory\\";
                        string text = message.Text;
                        string[] subs = text.Split(' ');
                        if (!Directory.Exists(path + subs[1]))
                        {
                            Directory.CreateDirectory(path + subs[1]);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директория " + subs[1] + " создана");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директория " + subs[1] + " уже существует");
                        }
                    }

                    if(message.Text.ToLower() == "/directory_watch" && ChatIDs.Contains(message.Chat.Id))
                    {
                        string dirName = "C:\\Users\\Полина\\source\\repos\\ExpertBot\\Bot_directory\\";
                        if (Directory.Exists(dirName))
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Подкаталоги:");
                            string[] dirs = Directory.GetDirectories(dirName);//?

                            foreach (string s in dirs)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, s.Split("\\")[7]);
                            }
                        }
                    }

                    if(message.Text.ToLower() == "/directory_delete" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Для удаления калатога введите: /directory_delete [name_directory]");
                    }

                    if((message.Text.ToLower().Split(" ")[0] == "/directory_delete") && (message.Text.ToLower().Split(" ").Length == 2) && ChatIDs.Contains(message.Chat.Id))
                    {
                        string[] subs = message.Text.ToLower().Split(" ");
                        string dirName = "C:\\Users\\Полина\\source\\repos\\ExpertBot\\Bot_directory\\" + subs[1];
                        if (Directory.Exists(dirName))
                        {
                            Directory.Delete(dirName, true);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Каталог " + subs[1] + " удален");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Каталог "  + subs[1] + " не существует");
                        }
                    }
                    
                    if (message.Text.ToLower() == "/add_questions" && ChatIDs.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Отправьте название документа и вопросы(Документ: name без расширения\n Вопросы: вопрос1\nвопрос2)");
                    }

                    if (message.Text.ToLower().Contains("документ:") && ChatIDs.Contains(message.Chat.Id))
                    {
                        string text = message.Text;
                        string[] subs = text.Split('\n');
                        string filePath = "C:\\Users\\Полина\\source\\repos\\ExpertBot\\Bot_forms\\";
                        int found = subs[0].IndexOf(":");
                        string name = subs[0].Substring(found + 2);
                        filePath = filePath + name + ".txt";
                        int found2 = subs[1].IndexOf(":");
                        subs[1] = subs[1].Substring(found2 + 2);
                        
                        using (StreamWriter fileStream = System.IO.File.Exists(filePath) ? System.IO.File.AppendText(filePath) : System.IO.File.CreateText(filePath))
                        {
                            for (int i = 1; i < subs.Length; i++)
                            {
                                fileStream.WriteLine(subs[i]);
                            }
                        }
                    }

                }

                if (message.Document != null && ChatIDs.Contains(message.Chat.Id))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Файл принят");

                    var fileId = update.Message.Document.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    var filePath = fileInfo.FilePath;

                    string destinationFilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\{message.Document.FileName}";
                    await using Stream fileStream = System.IO.File.OpenWrite(destinationFilePath);
                    await botClient.DownloadFileAsync(filePath, fileStream);
                    fileStream.Close();
                    StreamReader StreamR = new StreamReader("C:\\UC:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt");
                    int count = Convert.ToInt32(StreamR.ReadLine());
                    string Reply = StreamR.ReadToEnd();
                    StreamR.Close();
                    StreamWriter StreamW = new StreamWriter("C:\\Users\\Полина\\source\\repos\\ExpertBot\\BotLibr.txt", false);
                    StreamW.Write(Convert.ToString(count + 1) + "\n" + Reply + "\n" + message.Document.FileName);
                    StreamW.Close();
                }

            }

        }
    }
}

