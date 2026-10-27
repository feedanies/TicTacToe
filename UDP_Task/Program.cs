using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace UDP_Task
{
    internal class Program
    {

        public static int RemotePort { get; set; }
        public static int LocalPort { get; set; }
        public static IPAddress IP { get; set; }
        static int[,] board = new int[3, 3];

        static void Main(string[] args)
        {
            IP = IPAddress.Parse("127.0.0.1");

            Console.Write("Enter name: ");
            var name = Console.ReadLine();

            Console.Write("Ozunuzde olan portu daxil edin: ");
            LocalPort = int.Parse(Console.ReadLine());
            Console.Write("Qosulacaginiz portu daxil edin: ");
            RemotePort = int.Parse(Console.ReadLine());

            var client = new UdpClient(LocalPort);
            var remoteEp = new IPEndPoint(IP, RemotePort);

            var OBytes = Encoding.UTF8.GetBytes("JOIN|" + name);
            client.Send(OBytes, OBytes.Length, remoteEp);


            var text = ReceiveText(client);
            var parts = text.Split('|');

            int symbol;
            string opponentName = parts[1];

            if (parts[0] == "JOIN")
            {
                symbol = 1;
                var XBytes = Encoding.UTF8.GetBytes("WELCOME|" + name);
                client.Send(XBytes, XBytes.Length, remoteEp);
            }
            else
            {
                symbol = -1;
            }

            Console.WriteLine($"Sen: {(symbol == 1 ? "X" : "O")}, reqib: {opponentName}");

            string gamer1 = symbol == 1 ? name : opponentName;
            string gamer2 = symbol == 1 ? opponentName : name;

            int turn = 1;

            while (true)
            {
                PrintBoard();
                if (turn == symbol)
                {
                    Console.Write("Setir daxil edin (1-3 araliginda): ");
                    if (!int.TryParse(Console.ReadLine(), out int r))
                    {
                        Console.WriteLine("Reqem daxil edin!");
                        continue;
                    }

                    Console.Write("Sutun daxil edin (1-3 araliginda): ");
                    if (!int.TryParse(Console.ReadLine(), out int c))
                    {
                        Console.WriteLine("Reqem daxil edin!");
                        continue;
                    }

                    if ((r < 1 || r > 3) || (c < 1 || c > 3))
                    {
                        Console.WriteLine("Wrong input...");
                        continue;
                    }
                    if (board[r - 1, c - 1] != 0)
                    {
                        Console.WriteLine("Bu xana doludur!");
                        continue;
                    }

                    board[r - 1, c - 1] = symbol;
                    var moveBytes = Encoding.UTF8.GetBytes($"MOVE|{r - 1}|{c - 1}");
                    client.Send(moveBytes, moveBytes.Length, remoteEp);
                }
                else
                {
                    Console.WriteLine($"{opponentName} gedis edir...");
                    var msg = ReceiveText(client);
                    var move = msg.Split('|');
                    int mr = int.Parse(move[1]);
                    int mc = int.Parse(move[2]);
                    board[mr, mc] = -symbol;

                }

                if (symbol == 1)
                {
                    SaveGame(gamer1, gamer2);
                }

                int winner = CheckWinner();
                if (winner != 0)
                {
                    PrintBoard();
                    if (winner == symbol)
                    {
                        Console.WriteLine("Tebrikler, siz qazandiniz!");
                    }
                    else
                    {
                        Console.WriteLine($"{opponentName} qazandi!");
                    }
                    break;
                }

                if (IsDraw())
                {
                    PrintBoard();
                    Console.WriteLine("Hec-hece!");
                    break;
                }

                turn = -turn;
            }

        }

        static void PrintBoard()
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    string point;
                    if (board[r, c] == 1)
                    {
                        point = "X";
                    }
                    else if (board[r, c] == -1)
                    {
                        point = "O";
                    }
                    else
                    {
                        point = "-";
                    }
                    Console.Write($" {point} ");
                    if (c < 2)
                    {
                        Console.Write("|");
                    }
                }
                Console.WriteLine();
                if (r < 2)
                {
                    Console.WriteLine("---+---+---");
                }
            }
        }

        static string ReceiveText(UdpClient client)
        {
            IPEndPoint ep = null;
            while (true)
            {
                try
                {
                    var data = client.Receive(ref ep);
                    return Encoding.UTF8.GetString(data);
                }
                catch (Exception ex)
                {

                }
            }
        }

        static int CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                int rowSum = board[i, 0] + board[i, 1] + board[i, 2];
                int colSum = board[0, i] + board[1, i] + board[2, i];

                if (rowSum == 3 || colSum == 3) return 1;
                if (rowSum == -3 || colSum == -3) return -1;
            }

            int diag1 = board[0, 0] + board[1, 1] + board[2, 2];
            int diag2 = board[0, 2] + board[1, 1] + board[2, 0];

            if (diag1 == 3 || diag2 == 3) return 1;
            if (diag1 == -3 || diag2 == -3) return -1;

            return 0;
        }

        static bool IsDraw()
        {
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    if (board[r, c] == 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        static void SaveGame(string gamer1, string gamer2)
        {
            int[][] game = new int[3][];
            for (int r = 0; r < 3; r++)
            {
                game[r] = new int[] { board[r, 0], board[r, 1], board[r, 2] };
            }
            var data = new GameData { Gamer1 = gamer1, Gamer2 = gamer2, Game = game };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText($"{gamer1}-{gamer2}.json", json);
        }

    }
}
