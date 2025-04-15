using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace NavalBattle_Server;

class GameServer
{
    static List<Player> players = new List<Player>();
    static TcpListener server;
    static Dictionary<Player, bool> boardsReceived = new Dictionary<Player, bool>();

    static void Main()
    {
        server = new TcpListener(IPAddress.Any, 8888);
        server.Start();
        Console.WriteLine("Game Server is running...");
        boardsReceived.Clear();

        while (players.Count < 2)
        {
            TcpClient client = server.AcceptTcpClient();
            Player p = new Player(client, players.Count + 1);
            players.Add(p);
            boardsReceived[p] = false;
            
            Console.WriteLine("New player connected!");

            Thread t = new Thread(() => ManagePlayer(p));
            t.Start();
        }
        // SendAll("START") and players[0].Send("YOUR_TURN") are now moved to after boards are received
    }

    static void ManagePlayer(Player p)
    {
        while (p.IsConnected)
        {
            string msg = p.Receive();
            if(string.IsNullOrEmpty(msg)) break;
            
            Console.WriteLine($"{p.Username}: {msg}");

            if (msg.StartsWith("FIRE"))
            {
                string[] parts = msg.Split(';');
                string position = parts[1];

                Player adversary = players.Find(player => player != p);
                
                string result = VerifyShot(adversary.Board, position);
                
                p.Send(result);
                
                adversary.Send("INCOMING;" + position);

                if (VerifyVictory(adversary.Board))
                {
                    p.Send("WIN");
                    adversary.Send("LOSE");
                }
                else
                {
                    adversary.Send("YOUR_TURN");
                }
            }
            else if (msg.StartsWith("BOARD;"))
            {
                string boardData = msg.Substring(6); // Remove "BOARD;"
                File.WriteAllText(p.Board, boardData);
                boardsReceived[p] = true;
                Console.WriteLine($"{p.Username} submitted their board.");
                p.Send("BOARD_RECEIVED");
                
                if (boardsReceived.Count == 2 && boardsReceived.Values.All(received => received))
                {
                    SendAll("START");
                    players[0].Send("YOUR_TURN");
                }
            }
        }

        p.Close();
        Console.WriteLine("Player disconnected!");
    }

    static string VerifyShot(string file, string position)
    {
        char column = position[0]; // A to J
        int row = int.Parse(position.Substring(1)); // 0 to 9

        int col = column - 'A';
        string[] lines = File.ReadAllLines(file);
        string[] cells = lines[row].Split(' ');
        string cell = cells[col];

        if (cell == "N")
        {
            cells[col] = "X";
            lines[row] = string.Join(" ", cells);
            File.WriteAllLines(file, lines);
            return cell;
        }
        else if (cell == "˜")
        {
            cells[col] = "O";
            lines[row] = string.Join(" ", cells);
            File.WriteAllLines(file, lines);
            return "MISS;" + position;
        }
        else
        {
            return "REPEAT;" + position;
        }
    }

    static bool VerifyVictory(string file)
    {
        string[] lines = File.ReadAllLines(file);
        foreach (string row in lines)
        {
            if (row.Contains("N")) return false;
        }
        return true;
    }

    static void CreateTable(string file)
    {
        string[,] grid = new string [10, 10];
        
        //initialize w/ water
        for (int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                grid[i, j] = "˜";
            }
        }
        
        //insert a ship vertically with 3 cells
        Random rnd = new Random();
        int x = rnd.Next(0, grid.GetLength(0));
        int y = rnd.Next(0, grid.GetLength(1));
        for (int i = 0; i < 3; i++)
        {
            grid[x + 1, y] = "N";
        }

        using (StreamWriter sw = new StreamWriter(file))
        {
            for (int i = 0; i < 10; i++)
            {
                List<string> row = new List<string>();
                for (int j = 0; j < 10; j++)
                {
                    row.Add(grid[i, j]);
                }
                sw.WriteLine(string.Join(" ", row));
            }
        }
    }

    static void SendAll(string msg)
    {
        foreach (var p in players)
        {
            p.Send(msg);
        }
    }
    
}

