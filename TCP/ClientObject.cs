using System.Net.Sockets;
using System.Text;

namespace ChatServer
{
    public class ClientObject
    {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        string userName;
        TcpClient client;
        ServerObject server; // объект сервера

        public ClientObject(TcpClient tcpClient, ServerObject serverObject)
        {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Process()
        {
            try
            {
                Stream = client.GetStream();
                // получаем имя пользователя
                string message = GetMessage();
                userName = message;

                message = userName + " вошел в чат";
                WriteToFile(message);
                // посылаем сообщение о входе в чат всем подключенным пользователям
                server.BroadcastMessage(message, this.Id);
                Console.WriteLine(message);
                // в бесконечном цикле получаем сообщения от клиента
                while (true)
                {
                    try
                    {
                        message = GetMessage();
                        message = String.Format("{0}: {1}", userName, message);

                        WriteToFile(message);

                        Console.WriteLine(message);
                        // server.BroadcastMessage(message, this.Id);
                    }
                    catch
                    {
                        message = String.Format("{0}: покинул чат", userName);

                        WriteToFile(message);

                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        private void WriteToFile(string message)
        {

            FileStream fileStream = new FileStream($"..\\..\\..\\..\\Messages\\{userName}Messages.txt", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            using (BinaryWriter writer = new BinaryWriter(fileStream))
            {
                //fileStream.Lock(0, fileStream.Length);
                writer.Write(message + " ");
                writer.Write(DateTime.Now.ToString()+ "\n");
                Task.Delay(6000).Wait();
            }
        }

        // чтение входящего сообщения и преобразование в строку
        private string GetMessage()
        {
            string message;
            BinaryReader reader = new BinaryReader(Stream);

            message = reader.ReadString();

            if (message[0] != '@')
            {
                return message;
            }
            else
            {
                foreach (var client in server.clients)
                {
                    if (client.userName == message.Substring(1, message.Length - 1))
                    {
                        while (message != "@end")
                        {
                            message = reader.ReadString();
                            client.GetPersonalMessage(message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Такого пользователя нет!");
                        return message;
                    }
                }
                return $"{userName} Вернулся в общий чат";
            }
        }

        private void GetPersonalMessage(string message)
        {
            byte[] data = Encoding.Unicode.GetBytes(message);

            Stream.Write(data, 0, data.Length); //передача данных


        }

        // закрытие подключения
        protected internal void Close()
        {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
        }
    }
}