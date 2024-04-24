using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

public class TCPClient : MonoBehaviour
{
    // ���� �ּ� �� ��Ʈ ��ȣ
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
            Debug.Log("������ ����Ǿ����ϴ�.");
            await StartListeningAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("������ ������ �� �����ϴ�: " + e.Message);
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
                    Debug.Log("������ ������: " + receivedData);

                    // ��ǥ�� ���е� ���ڿ��� �и��Ͽ� �� ��Ҹ� ����
                    string[] dataParts = receivedData.Split(',');
                    if (dataParts.Length == 3)
                    {
                        // �� ��Ҹ� �ʿ��� �ڷ������� ��ȯ�Ͽ� ����
                        pitch = float.Parse(dataParts[0]);
                        yaw = float.Parse(dataParts[1]);
                        roll = float.Parse(dataParts[2]);
                    }
                    else
                    {
                        Debug.LogWarning("������ ������ ������ �ùٸ��� �ʽ��ϴ�.");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("������ ���� �� ���� �߻�: " + e.Message);
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
        Debug.Log("������ ������ ���������ϴ�. �翬���� �õ��մϴ�...");
        Invoke("RetryConnection", 3f); // 3�� �Ŀ� �翬�� �õ�
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
        Debug.Log("������ ����Ǿ����ϴ�.");
    }
}
