using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

public class TCPClient : MonoBehaviour
{
    // ���� �ּ� �� ��Ʈ ��ȣ
    public string serverAddress = "127.0.0.1";
    public int port = 65432;

    TcpClient client;
    NetworkStream stream;

    void Start()
    {
        // TCP Ŭ���̾�Ʈ �ʱ�ȭ
        client = new TcpClient(serverAddress, port);
        stream = client.GetStream();

        // ������ ������ ���� ������ ����
        StartListening();
    }

    void StartListening()
    {
        byte[] buffer = new byte[1024];
        while (true)
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("������ ������: " + receivedData);
            }
        }
    }

    void OnDestroy()
    {
        // ���� ����
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}
