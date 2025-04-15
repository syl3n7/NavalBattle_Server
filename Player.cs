using System;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace NavalBattle_Server;

public class Player
{
    public TcpClient TcpClient { get; private set; }
    public string Username { get; private set; }
    public string Board { get; private set; }
    public NetworkStream Stream => TcpClient.GetStream();
    public bool IsConnected => TcpClient.Connected;

    public Player(TcpClient client, int playerNumber)
    {
        TcpClient = client;
        Username = $"Player{playerNumber}";
        Board = $"Player{playerNumber}.txt";
    }

    public void Send(string msg)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            Stream.Write(data, 0, data.Length);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Erro ao enviar para {Username}: {ex.Message}");
        }
    }

    public string Receive()
    {
        try
        {
            byte[] buffer = new byte[1024];
            int bytes = Stream.Read(buffer, 0, buffer.Length);
            return Encoding.UTF8.GetString(buffer, 0, bytes);
        }
        catch
        {
            return null;
        }
    }

    public void Close()
    {
        try
        {
            Stream.Close();
            TcpClient.Close();
        }
        catch { }
    }

    public override string ToString()
    {
        return Username;
    }
    
}