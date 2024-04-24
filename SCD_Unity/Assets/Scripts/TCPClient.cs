using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TCPClient : MonoBehaviour
{
    // 서버 주소 및 포트 번호
    public string serverAddress = "127.0.0.1";
    public int port = 65432;

    public float pitch;
    public float yaw;
    public float roll;
    
    TcpClient client;
    NetworkStream stream;
    bool isConnected = false;


    async void Start()
    {
        await ConnectToServerAsync();
    }

    async Task ConnectToServerAsync()
    {
        try
        {
            client = new TcpClient();
            await client.ConnectAsync(serverAddress, port);
            stream = client.GetStream();
            isConnected = true;
            Debug.Log("서버에 연결되었습니다.");
            await StartListeningAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("서버에 연결할 수 없습니다: " + e.Message);
            Reconnect();
        }
    }

    async Task StartListeningAsync()
    {
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            try
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead > 0)
                {
                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log("수신한 데이터: " + receivedData);

                    // 쉼표로 구분된 문자열을 분리하여 각 요소를 추출
                    string[] dataParts = receivedData.Split(',');
                    if (dataParts.Length == 3)
                    {
                        // 각 요소를 필요한 자료형으로 변환하여 저장
                        pitch = float.Parse(dataParts[0]);
                        yaw = float.Parse(dataParts[1]);
                        roll = float.Parse(dataParts[2]);
                    }
                    else
                    {
                        Debug.LogWarning("수신한 데이터 형식이 올바르지 않습니다.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("데이터 수신 중 오류 발생: " + e.Message);
                Reconnect();
            }
        }
    }

    void Reconnect()
    {
        isConnected = false;
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
        Debug.Log("서버와 연결이 끊어졌습니다. 재연결을 시도합니다...");
        Invoke("RetryConnection", 3f); // 3초 후에 재연결 시도
    }

    void RetryConnection()
    {
        if (!isConnected)
            _ = ConnectToServerAsync();
    }

    void OnDestroy()
    {
        Disconnect();
    }

    void Disconnect()
    {
        isConnected = false;
        if (stream != null)
            stream.Close();
        if (client != null)
            client.Close();
        Debug.Log("연결이 종료되었습니다.");
    }
}
