using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    public int port = 12345;
    private UdpClient udpClient;

    [HideInInspector] public float pitch;
    [HideInInspector] public float yaw;
    [HideInInspector] public float roll;
    [HideInInspector] public bool clutch;

    public int averageCount = 30;
    public bool queueReset = false;
    public Queue<Vector3> rotationQueue = new Queue<Vector3>();

    void Start()
    {
        try
        {
            udpClient = new UdpClient(port);
            Debug.Log("UDP Receiver started. Listening on port: " + port);
            udpClient.BeginReceive(ReceiveData, null);
        }
        catch (Exception e)
        {
            Debug.LogError("Error starting UDP Receiver: " + e.Message);
        }
    }

    private void ReceiveData(IAsyncResult result)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        byte[] receivedBytes = null;
        try
        {
            receivedBytes = udpClient.EndReceive(result, ref remoteEndPoint);
            string receivedString = System.Text.Encoding.UTF8.GetString(receivedBytes);

            Debug.Log("Received data: " + receivedString);

            // Parsing
            string[] values = receivedString.Split(',');
            pitch = float.Parse(values[0]);
            yaw = -float.Parse(values[1]);
            roll = float.Parse(values[2]);
            clutch = bool.Parse(values[3]);

            rotationQueue.Enqueue(new Vector3(pitch, yaw, roll));
            if (queueReset)
            {
                rotationQueue.Clear();
                queueReset = false;
            }
            if (rotationQueue.Count >= averageCount)
            {
                CalcMovingAverage();
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error receiving data: " + e.Message);
        }
        finally
        {
            // 다시 데이터를 수신 대기
            if (udpClient != null)
                udpClient.BeginReceive(ReceiveData, null);
        }
    }

    private void CalcMovingAverage()
    {
        Vector3 averageRotation = Vector3.zero;
        foreach (Vector3 rotation in rotationQueue)
        {
            averageRotation += rotation;
        }
        averageRotation /= rotationQueue.Count;

        pitch = averageRotation.x;
        yaw = averageRotation.y;
        roll = averageRotation.z;

        rotationQueue.Dequeue();
    }
}
