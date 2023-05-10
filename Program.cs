using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Sheets.v4;
using Google.Apis.Util.Store;
using Google.Apis.Services;
using Google.Apis.Sheets.v4.Data;
using System.Text;
using Npgsql;


class Program
{

    //private static Dictionary<string, Dictionary<string, List<string>>> answer_to_questions = new Dictionary<string, Dictionary<string, List<string>>>();
    private static int lineNumber = 0;
    private static string libr_path = "D:\\ConsoleApp2\\BotLibr.txt";
    private static string forms_path = "D:\\ConsoleApp2\\Bot_forms\\";
    private static string dir_path = "D:\\ConsoleApp2\\Bot_directory\\";
    private static string project_path = "D:\\ConsoleApp2";
    private static string files_path = project_path + "\\files";
    private static string desired_url;
    private static string sheetId;
    private static string text;

    // переменная для доступа к аккаунту пользователя
    private static UserCredential credential;
    private static SheetsService service;
    private static DriveService driveService;
    private static string cred = "D:\\ConsoleApp2\\client_secret_378576405434-opc6r5d4jprntr4bhf9qcpalib8708d2.apps.googleusercontent.com.json";
    private static string sheetName = "Sheet1";

    private static Dictionary<string, string> FileFormDict = new Dictionary<string, string>();
    private static Dictionary<string, string> url_answer = new Dictionary<string, string>();
    private static string connectionString;

    /* Тут мы создаём список пользователей и чатайдишников */
    private static string? name;
    /* Вот эти айди */
    private static List<string> moders = new List<string>();
    private static List<string> experts = new List<string>(); // Добавь проверку на эксперта и воркера(это те кто файлы добавляют)
    private static List<string> workers = new List<string>();
    private static SortedSet<long> ChatIDs = new();
    private static SortedSet<long> ChatID_expert = new();
    private static SortedSet<long> ChatID_checker = new();

    private static string[] enter = new string[] { "expert", "checker" };
    private static string[] chec = new string[] { "directory_watch", "docs_watch", "docx_watch", "add_directory", "directory_delete",
    "add_docx", "adduser", "showing_answers"};
    private static void Start()
    {
        using (var stream = new FileStream(cred, FileMode.Open, FileAccess.Read))
        {
            string credPath = "token.json";
            credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                GoogleClientSecrets.Load(stream).Secrets,
                new[] { DriveService.Scope.Drive, SheetsService.Scope.Spreadsheets },
                "user",
                System.Threading.CancellationToken.None,
                new FileDataStore(credPath, true)).Result;

        }

        //credential = GoogleCredential.GetApplicationDefault().CreateScoped(SheetsService.Scope.Spreadsheets);

        driveService = new DriveService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Google Sheets API",
        });

        service = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential,
            ApplicationName = "Google Sheets API",
        });
    }

    static void Main(string[] args)
    {

        /*Тута подключаемся к ДБ и получаем айдишники */
        // работает. метод лежит в system.configuration
        connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ExpertWork"].ConnectionString;
        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
        {
            connection.Open();

            string query1 = "SELECT moder, expert, worker FROM users";
            using (NpgsqlCommand command = new NpgsqlCommand(query1, connection))
            {
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {


                    while (reader.Read())
                    {
                        string moderValues = (string)reader.GetValue(0); /* Везде содержатся ваши айди */
                        string expertValues = (string)reader.GetValue(1);
                        string workerValues = (string)reader.GetValue(2);
                        moders.Add(moderValues);
                        experts.Add(expertValues);
                        workers.Add(workerValues);
                    }
                }
            }

            string query2 = "SELECT name, url FROM answers";
            using (NpgsqlCommand command = new NpgsqlCommand(query2, connection))
            {
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {


                    while (reader.Read())
                    {
                        string nameValues = (string)reader.GetValue(0);
                        string urlValues = (string)reader.GetValue(1);
                        url_answer.Add(nameValues, urlValues);
                    }
                }
            }
        }

        var client = new TelegramBotClient("5844793789:AAEx-E8FfHw9SBbJuMroR94npIv-J2rI0_E");

        client.StartReceiving(Update, Error);
        Console.ReadLine();
    }

    private static Task Error(ITelegramBotClient arg1, Exception arg2, CancellationToken arg3)
    {
        throw new NotImplementedException();
    }

    private static async Task Update(ITelegramBotClient botClient, Update update, CancellationToken token)
    {
        try
        {
            var message = update.Message;
            var button = update.CallbackQuery;

            if (message != null)
            {
                if (message.Text != null)
                {
                    Console.WriteLine(message.Text);
                    if (message.Text.ToLower() == "/start")
                    {
                        //Start();
                        if (experts.Contains(message.Chat.Username))
                        {
                            ChatID_expert.Add(message.Chat.Id);
                        }
                        if (workers.Contains(message.Chat.Username))
                        {
                            ChatID_checker.Add(message.Chat.Id);
                        }
                        if (workers.Contains(message.Chat.Username) || experts.Contains(message.Chat.Username))
                        {
                            ChatIDs.Add(message.Chat.Id);
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Добро пожаловать");
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Выберите вашу роль\n1.Эксперт - введите /expert\n2.Проверяющий - введите /checker", replyMarkup: CreateButtons(2, enter));
                    }

                    if (message.Text.ToLower() == "/expert" && ChatID_expert.Contains(message.Chat.Id))
                    {

                        await botClient.SendTextMessageAsync(message.Chat.Id,
                        "Как эксперт вы можете:\n" +
                        "Просмотреть каталоги: /directory_watch\n" +
                        "Просмотреть документы в каталоге: /docs_watch\n" +
                        "Просмотреть документы и скачать нужный: /docx_watch\n" +
                        "Зайти как проверяющий: /checker", replyMarkup: CreateButtons(3, chec));//?
                    }

                    if (message.Text.ToLower() == "/checker" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        Start();
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                        "Как проверяющий вы можете:\n" +
                        "Просмотреть каталоги: /directory_watch\n" +
                        "Просмотреть документы в каталоге: /docs_watch\n" +
                        "Просмотреть документы и скачать нужный: /docx_watch\n" +
                        "Добавить каталог: /add_directory\n" +
                        "Удалить каталог: /directory_delete\n" +
                        "Добавить документ: /add_docx\n" +
                        "Добавить нового пользователя: /adduser\n" +
                        "Просмотреть ответы к документу: /showing_answers", replyMarkup: CreateButtons(8, chec));
                    }

                    if (message.Text.ToLower() == "/docs_watch" && ChatIDs.Contains(message.Chat.Id))
                    {

                        await botClient.SendTextMessageAsync(message.Chat.Id, "Для просмотра файлов каталога введите:\n /docs_watch [name_directory]");

                    }

                    if (message.Text.ToLower().Split(" ")[0] == "/docs_watch" && ChatIDs.Contains(message.Chat.Id) && message.Text.ToLower().Split(" ").Length == 2)
                    {
                        string path = dir_path + message.Text.ToLower().Split(" ")[1] + "\\FileName.txt";
                        using (FileStream fs = System.IO.File.OpenRead(path))
                        {
                            byte[] b = new byte[1024];
                            UTF8Encoding temp = new UTF8Encoding(true);
                            int readLen;
                            while ((readLen = fs.Read(b, 0, b.Length)) > 0)
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, temp.GetString(b, 0, readLen));
                            }
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Для скачивания и удаления файла введите сначала: /docx_watch [number_docx]");
                    }

                    if (message.Text.ToLower() == "/docx_watch" && ChatIDs.Contains(message.Chat.Id))
                    {
                        StreamReader StreamR = new StreamReader(project_path + "\\BotLibr.txt");
                        int count = Convert.ToInt32(StreamR.ReadLine());
                        string Reply = "";
                        if (count == 0)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "В данный момент в системе нет файлов");
                        }
                        else
                        {
                            for (int i = 0; i < count; i++)
                            {
                                string Str = StreamR.ReadLine();
                                Reply = Reply + Convert.ToString(i + 1) + ". " + Str + "\n";
                            }
                            StreamR.Close();
                            Reply = "В базе в данный момент файлов " + Convert.ToString(count) + ".\n" + Reply;
                            await botClient.SendTextMessageAsync(message.Chat.Id, Reply);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Для того чтобы скачать документ, введите '/docx_download 'номер файла' '\n" +
                                "Для того чтобы удалить документ, введите '/docx_delete 'номер файла' ' (функция проверяющего)");
                        }
                    }


                    if (message.Text.ToLower() == "/add_docx" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Для добавления файла введите: /add_docx [name_directory] [name_file(с расширением)] \n Для добавления вопросов - '/add_questions'");
                    }

                    if (message.Text.ToLower().Split(" ")[0] == "/add_docx" && (message.Text.ToLower().Split(" ").Length == 3))
                    {
                        string path = dir_path + message.Text.ToLower().Split(" ")[1];
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директория " + message.Text.ToLower().Split(" ")[1] + " создана");
                            FileInfo fileInfo = new FileInfo(path + "\\FileName.txt");
                            FileStream fs = fileInfo.Create();
                            fs.Write(Encoding.UTF8.GetBytes(message.Text.ToLower().Split(" ")[2] + "\n"));
                            fs.Close();
                        }
                        else
                        {
                            DirectoryInfo directory = new DirectoryInfo(path);
                            FileInfo[] files = directory.GetFiles();
                            foreach (FileInfo file in files)
                            {
                                Console.WriteLine(file.FullName);
                            }
                            System.IO.StreamWriter writer = new System.IO.StreamWriter(path + "\\FileName.txt", true);
                            writer.WriteLine(message.Text.ToLower().Split(" ")[2]);
                            writer.Close();
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Файл " + message.Text.ToLower().Split(" ")[2] + " добавлен в директорию " + message.Text.ToLower().Split(" ")[1]);
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Следующим сообщением прикрепите файл");

                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/docx_delete") && (message.Text.ToLower().Split(" ").Length == 2) && ChatID_checker.Contains(message.Chat.Id))
                    {
                        try
                        {
                            StreamReader StreamR = new StreamReader(libr_path);
                            int count = Convert.ToInt32(StreamR.ReadLine());
                            string Reply = StreamR.ReadToEnd();
                            StreamR.Close();
                            string file_name = Reply.Split("\n")[Convert.ToInt32(message.Text.ToLower().Split(" ")[1])];
                            Console.WriteLine($"{file_name}");
                            Console.WriteLine(Reply.Split("\n"));
                            Reply = Reply.Replace("\n" + file_name, "");
                            StreamWriter StreamW = new StreamWriter(libr_path, false);
                            StreamW.Write(Convert.ToString(count - 1) + "\n" + Reply);
                            StreamW.Close();
                            FileInfo fileInfo = new FileInfo($"{files_path}\\{file_name}");
                            fileInfo.Delete();
                            bool fileExist = System.IO.File.Exists($"{forms_path}\\{file_name}");
                            if (fileExist)
                            {
                                FileInfo fileInfoForms = new FileInfo($"{forms_path}\\{file_name}");
                                fileInfoForms.Delete();
                                FileFormDict.Remove(file_name);
                                using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                                {
                                    connection.Open();

                                    string sql = "DELETE FROM answers WHERE name = @name";
                                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                                    {
                                        command.Parameters.AddWithValue("name", file_name);
                                    }
                                }
                            }

                                /*
                                // удаление таблицы в google.drive
                                desired_url = file_name + "_answer";
                                Console.WriteLine(desired_url);
                                var request = driveService.Files.List();
                                var response = request.Execute();
                                foreach (var file in response.Files)
                                {
                                    if (file.Name.Equals(desired_url))
                                    {
                                        sheetId = file.Id;
                                        break;
                                    }
                                }
                                service.Spreadsheets.BatchUpdate(new BatchUpdateSpreadsheetRequest()
                                {
                                    Requests = new List<Request> { new Request { DeleteSheet = new DeleteSheetRequest { SheetId = 1 } } }
                                }, sheetId).Execute();
                                */

                                await botClient.SendTextMessageAsync(message.Chat.Id, "Файл удалён");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/docx_download") && (message.Text.ToLower().Split(" ").Length == 2) && ChatIDs.Contains(message.Chat.Id))
                    {
                        try
                        {
                            StreamReader StreamR = new StreamReader(libr_path);
                            string Reply = StreamR.ReadToEnd();
                            StreamR.Close();
                            string file_name = Reply.Split("\n")[Convert.ToInt32(message.Text.ToLower().Split(" ")[1]) + 1];
                            await using Stream stream = System.IO.File.OpenRead($"{files_path}\\{file_name}");
                            await botClient.SendDocumentAsync(message.Chat.Id, new InputOnlineFile(stream, file_name));


                            if (FileFormDict.ContainsKey(file_name))
                            {
                                // вызов answer_to_questions
                                Console.WriteLine(file_name);
                                desired_url = file_name + "_answer";
                                message.Text = "/answer_to_questions";
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Отправление ответов происходит в следующем формате: /answer_to_questions ответ на вопрос");
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Для того, чтобы не отвечать на вопрос введите: пропуск вопроса");
                                lineNumber = 0;
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "К файлу не прикреплены вопросы");
                            }

                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Файл для скачивания не найден");
                            Console.WriteLine(ex.ToString());
                        }

                    }

                    if (message.Text.ToLower() == "/add_questions" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Отправьте название документа и вопросы(Документ: name(с расширением) \n Вопросы: вопрос1\nвопрос2)");
                    }
                    if (message.Text.ToLower().Contains("документ:") && ChatID_checker.Contains(message.Chat.Id))
                    {
                        string text = message.Text;
                        string[] subs = text.Split('\n');
                        string filePath = forms_path;
                        int found = subs[0].IndexOf(":");
                        string name = subs[0].Substring(found + 2);
                        Console.WriteLine(name);
                        int found2 = name.IndexOf(".");
                        string nameF = name.Substring(0, found2);
                        nameF = nameF + ".txt";
                        filePath = filePath + nameF;
                        int found3 = subs[1].IndexOf(":");
                        subs[1] = subs[1].Substring(found3 + 2);
                        FileFormDict.Add(name, nameF);

                        string sql = "INSERT INTO answers (nameF) VALUES (@NameF) WHERE name = @name";
                        using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                        using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                        {
                            command.Parameters.AddWithValue("name", name);
                            command.Parameters.AddWithValue("@NameF", nameF);

                            connection.Open();
                            int rowsAffected = command.ExecuteNonQuery();
                            connection.Close();
                        }

                        // открываем файл если нет файла то создаем файл
                        using (StreamWriter fileStream = System.IO.File.Exists(filePath) ? System.IO.File.AppendText(filePath) : System.IO.File.CreateText(filePath))
                        {
                            for (int i = 1; i < subs.Length; i++)
                            {
                                message.Text = subs[i];
                                fileStream.WriteLine(subs[i]);
                                EnterData(message, i);
                            }
                        }
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Вопросы прикреплены к файлу");
                    }

                    if (message.Text.ToLower() == "/add_directory" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Для создания калатога введите:\n /add_directory [name_directory]");
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/add_directory") && (message.Text.ToLower().Split(" ").Length == 2) && ChatID_checker.Contains(message.Chat.Id))
                    {
                        string text = message.Text;
                        string[] subs = text.Split(' ');
                        if (!Directory.Exists(dir_path + subs[1]))
                        {
                            Directory.CreateDirectory(dir_path + subs[1]);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директория " + subs[1] + " создана");
                            FileInfo fileInfo = new FileInfo(dir_path + subs[1] + "\\FileName.txt");
                            FileStream fs = fileInfo.Create();
                            fs.Close();
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директория " + subs[1] + " уже существует");
                        }
                    }

                    if (message.Text.ToLower() == "/directory_watch" && ChatIDs.Contains(message.Chat.Id))
                    {
                        try
                        {
                            if (Directory.Exists(dir_path))
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Подкаталоги:");
                                string[] dirs = Directory.GetDirectories(dir_path);//?

                                foreach (string s in dirs)
                                {
                                    await botClient.SendTextMessageAsync(message.Chat.Id, s.Split("\\")[7]);
                                }
                            }
                            else
                            {
                                await botClient.SendTextMessageAsync(message.Chat.Id, "Директорий не существует");
                            }
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Директорий не существует");
                        }
                    }

                    if (message.Text.ToLower() == "/directory_delete" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id,
                            "Для удаления калатога введите: /directory_delete [name_directory]");
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/directory_delete") && (message.Text.ToLower().Split(" ").Length == 2) && ChatID_checker.Contains(message.Chat.Id))
                    {
                        string[] subs = message.Text.ToLower().Split(" ");
                        string dirName = dir_path + subs[1];
                        if (Directory.Exists(dirName))
                        {
                            Directory.Delete(dirName, true);
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Каталог " + subs[1] + " удален");
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Каталог " + subs[1] + " не существует");
                        }
                    }

                    if (message.Text.ToLower().Contains("/answer_to_questions") && (message.Text.Split(" ").Length > 1) && ChatIDs.Contains(message.Chat.Id))
                    {
                        try
                        {

                            lineNumber += 1;
                            EnterData(message, lineNumber);
                            // это нужно для показа следующего вопроса пользователю
                            message.Text = "/answer_to_questions";
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }

                    }

                    if (message.Text.ToLower() == "пропуск вопроса")
                    {
                        lineNumber += 1;
                        message.Text = "/answer_to_questions";
                    }

                    if (message.Text.ToLower() == "/answer_to_questions" && ChatIDs.Contains(message.Chat.Id))
                    {
                        try
                        {
                            string answer_path = $"{forms_path}\\{desired_url.Split(".")[0] + ".txt"}";
                            Console.WriteLine(answer_path);
                            // читаем все строки из файла
                            string[] lines = System.IO.File.ReadAllLines(answer_path);
                            text = lines[lineNumber];

                            // Выводим вопрос
                            await botClient.SendTextMessageAsync(message.Chat.Id, text);
                            //lineNumber += 1;
                        }
                        catch (System.NullReferenceException ex)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Выберете сначала документ для скачивания");
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Вопросы закончились");
                        }
                    }

                    if (message.Text.ToLower() == "/showing_answers" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        string str = "";
                        int url_count = url_answer.Count;
                        if (url_count > 0)
                        {
                            foreach (string line in url_answer.Keys)
                            {
                                str += $"{line}: {url_answer[line]}\n";
                            }

                            await botClient.SendTextMessageAsync(message.Chat.Id, "Список с ссылками на все таблицы с ответами:");
                            await botClient.SendTextMessageAsync(message.Chat.Id, str);
                        }
                        else
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "В системе нет файлов на проверку");
                        }
                    }

                    if (message.Text.ToLower() == "/adduser" && ChatID_checker.Contains(message.Chat.Id))
                    {
                        await botClient.SendTextMessageAsync(message.Chat.Id, "Длф добавление пользователя введите: /adduser доступ пользователя(moder, expert или worker) ...");
                    }

                    if ((message.Text.ToLower().Split(" ")[0] == "/adduser") && (message.Text.ToLower().Split(" ").Length == 3) && ChatID_checker.Contains(message.Chat.Id))
                    {
                        try
                        {
                            string[] subs = message.Text.ToLower().Split(" ");
                            string sql = "INSERT INTO users (" + subs[1] + ") VALUES (@name)";
                            using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                            {
                                command.Parameters.AddWithValue("@name", subs[2]);

                                connection.Open();
                                int rowsAffected = command.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            await botClient.SendTextMessageAsync(message.Chat.Id, "Пользователь не добавлен");
                            Console.WriteLine(ex.ToString());
                        }
                    }
                }
                if (message.Document != null && ChatID_checker.Contains(message.Chat.Id))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Файл принят");

                    var fileId = update.Message.Document.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    var filePath = fileInfo.FilePath;
                    var filename = message.Document.FileName;
                    var filename_answer = filename + "_answer";

                    //string destinationFilePath = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\{filename}";

                    await using Stream fileStream = System.IO.File.OpenWrite($"{files_path}\\{filename}");
                    await botClient.DownloadFileAsync(filePath, fileStream);
                    fileStream.Close();
                    StreamReader StreamR = new StreamReader(libr_path);
                    int count = Convert.ToInt32(StreamR.ReadLine());
                    string Reply = StreamR.ReadToEnd();
                    StreamR.Close();
                    StreamWriter StreamW = new StreamWriter(libr_path, false);
                    StreamW.Write(Convert.ToString(count + 1) + "\n" + Reply + "\n" + filename);
                    StreamW.Close();

                    // создание google sheets с сохранением url адреса таблицы
                    var spreadsheet = new Spreadsheet()
                    {
                        Properties = new SpreadsheetProperties()
                        {
                            Title = filename_answer
                        },
                        Sheets = new List<Google.Apis.Sheets.v4.Data.Sheet>()
                            {
                                new Google.Apis.Sheets.v4.Data.Sheet()
                                {
                                    Properties = new Google.Apis.Sheets.v4.Data.SheetProperties()
                                    {
                                        Title = sheetName,
                                        GridProperties = new GridProperties()
                                        {

                                        }
                                    }
                                }
                            }
                    };

                    var request = service.Spreadsheets.Create(spreadsheet);
                    var response = request.Execute();
                    string spreadsheetURL = response.SpreadsheetUrl;
                    url_answer.Add(filename_answer, spreadsheetURL);


                    string connectionString = System.Configuration.ConfigurationManager.ConnectionStrings["ExpertWork"].ConnectionString; // эта строка не работает (не найден метод ConnectionStrings)
                    string sql = "INSERT INTO answers (name, url) VALUES (@Name, @Url)";                               // работает. метод лежит в system.configuration
                    using (NpgsqlConnection connection = new NpgsqlConnection(connectionString))
                    using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                    {

                        command.Parameters.AddWithValue("@Name", filename_answer);
                        command.Parameters.AddWithValue("@Url", spreadsheetURL);

                        connection.Open();
                        int rowsAffected = command.ExecuteNonQuery();
                        connection.Close();
                    }
                    desired_url = filename_answer;
                }
            }
            else
            {
                switch (update.Type)
                {
                    case Telegram.Bot.Types.Enums.UpdateType.CallbackQuery:
                        Console.WriteLine(update.CallbackQuery.Data);
                        switch (update.CallbackQuery.Data)
                        {
                            case "/expert":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id,
                                    "Как эксперт вы можете:\n" +
                                    "Просмотреть каталоги: /directory_watch\n" +
                                    "Просмотреть документы в каталоге: /docs_watch\n" +
                                    "Просмотреть документы и скачать нужный: /docx_watch\n" +
                                    "Зайти как проверяющий: /checker", replyMarkup: CreateButtons(3, chec));
                                break;
                            case "/checker":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id, 
                                    "Как проверяющий вы можете:\n" +
                                    "Просмотреть каталоги: /directory_watch\n" +
                                    "Просмотреть документы в каталоге: /docs_watch\n" +
                                    "Просмотреть документы и скачать нужный: /docx_watch\n" +
                                    "Добавить каталог: /add_directory\n" +
                                    "Удалить каталог: /directory_delete\n" +
                                    "Добавить документ: /add_docx\n" +
                                    "Добавить нового пользователя: /adduser\n" +
                                    "Просмотреть ответы к документу: /showing_answers", replyMarkup: CreateButtons(8, chec));
                                break;
                            case "/directory_watch":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id, "Для просмотра файлов каталога введите:\n /docs_watch [name_directory]");
                                break;
                            case "/docs_watch":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id, "Для просмотра файлов каталога введите:\n /docs_watch [name_directory]");
                                break;
                            case "/add_directory":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id,
                            "Для создания калатога введите:\n /add_directory [name_directory]");
                                break;
                            case "/directory_delete":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id,
                            "Для удаления калатога введите: /directory_delete [name_directory]");
                                break;
                            case "/add_docx":
                                await botClient.SendTextMessageAsync(button.Message.Chat.Id, "Для добавления файла введите: /add_docx [name_directory] [name_file(с расширением)] \n Для добавления вопросов - '/add_questions'");
                                break;
                            case "/showing_answers":
                                string str = "";
                                int url_count = url_answer.Count;
                                if (url_count > 0)
                                {
                                    foreach (string line in url_answer.Keys)
                                    {
                                        str += $"{line}: {url_answer[line]}\n";
                                    }

                                    await botClient.SendTextMessageAsync(button.Message.Chat.Id, "Список с ссылками на все таблицы с ответами:");
                                    await botClient.SendTextMessageAsync(button.Message.Chat.Id, str);
                                }
                                else
                                {
                                    await botClient.SendTextMessageAsync(button.Message.Chat.Id, "В системе нет файлов на проверку");
                                }
                                break;
                        }
                        break;
                }

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    private static string GetColumnName(int columnNumber)
    {
        int dividend = columnNumber;
        string columnName = String.Empty;
        int modulo;

        while (dividend > 0)
        {
            modulo = (dividend - 1) % 26;
            columnName = Convert.ToChar(65 + modulo).ToString();
            dividend = (int)((dividend - modulo) / 26);
        }

        return columnName;
    }

    private static int GetColumnCount(string id, string Range)
    {

        var request = service.Spreadsheets.Values.Get(id, Range);

        var response = request.Execute();

        var values = response.Values;

        if (values != null && values.Count > 0)
        {
            return values.Count + 1;
        }
        else
        {
            return 1;
        }
    }

    private static void EnterData(Message message, int lineNumber)
    {
        // получаем название колонки в таблице
        string columnName = GetColumnName(lineNumber);
        var mess = message.Text.Replace("/answer_to_questions ", "");

        var valueRange = new ValueRange()
        {
            Values = new List<IList<object>>()
            {
                new List<object>() { mess }
            }
        };

        // Создание объекта запроса для добавления значения
        valueRange = new ValueRange();
        valueRange.MajorDimension = "ROWS";
        var oblist = new List<object>() { mess };
        valueRange.Values = new List<IList<object>> { oblist };

        var request = driveService.Files.List();
        var response = request.Execute();

        foreach (var file in response.Files)
        {
            if (file.Name.Equals(desired_url))
            {
                sheetId = file.Id;

                break;
            }
        }
        int column_count = GetColumnCount(sheetId, $"{sheetName}!{columnName}{1}:{columnName}");

        // заносим данные в таблицу
        var appendRequest = service.Spreadsheets.Values.Append(valueRange, sheetId,
                    $"{sheetName}!{columnName}{column_count}:{columnName}");
        appendRequest.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;
        var appendResponse = appendRequest.Execute();
    }

    private static IReplyMarkup CreateButtons(int count, string[] asd)
    {
        var buttons = new InlineKeyboardButton[count][];

        for (int i = 0; i < count; i++)
        {
            buttons[i] = new[]
            {
                            InlineKeyboardButton.WithCallbackData(text: asd[i], callbackData: $"/{asd[i]}")};

        }

        var keyboard = new InlineKeyboardMarkup(buttons);

        return keyboard;
    }
}
