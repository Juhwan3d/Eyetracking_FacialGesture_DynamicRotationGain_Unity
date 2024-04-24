using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;

public class TCPClient : MonoBehaviour
{
    // 서버 주소 및 포트 번호
    public string serverAddress = "127.0.0.1";
    public int port = 65432;

    TcpClient client;
    NetworkStream stream;

    void Start()
    {
        // TCP 클라이언트 초기화
        client = new TcpClient(serverAddress, port);
        stream = client.GetStream();

        // 데이터 수신을 위한 쓰레드 시작
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
                Debug.Log("수신한 데이터: " + receivedData);
            }
        }
    }

    void OnDestroy()
    {
        // 연결 종료
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
    }
}
