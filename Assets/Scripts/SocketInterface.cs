using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Net;
using System.IO;
using System;
using System.ComponentModel;

namespace SocketInterfaceNameSpace
{
    public enum ReceiveStateEnum { IDLE, RECEIVING, RECEIVED };

    public class SocketInterface : MonoBehaviour
    {
        BackgroundWorker _WorkerReceive;
                
        ReceiveStateEnum _receiveState = ReceiveStateEnum.IDLE;
        public byte[] ReceiveMessageBuffer;
        
        [SerializeField]
        private int PortToSend = 12500;

        [SerializeField]
        private int PortToReceive = 12500;

        // Start is called before the first frame update
        void Start()
        {
            _WorkerReceive = new BackgroundWorker();
            ReceiveMessageBuffer = new byte[255];
            _WorkerReceive.WorkerReportsProgress = true;
            _WorkerReceive.DoWork += worker_ReceiveMessage;
            _WorkerReceive.ProgressChanged += worker_ProgressChanged;
            _WorkerReceive.RunWorkerCompleted += worker_RunWorkerCompleted;
            
        }

        // Update is called once per frame
        void Update()
        {
           
        }


        public void SendMessage(byte[] bytes)
        {
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint IPEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), PortToSend);

            if (s == null)
            {

                Debug.Log("No socket to send: Object is Null");
            }
            else
            {                
                if (_receiveState == ReceiveStateEnum.IDLE)
                {
                    _receiveState = ReceiveStateEnum.RECEIVING;
                    _WorkerReceive.RunWorkerAsync(10000);
                }
                else if (_receiveState == ReceiveStateEnum.RECEIVED)
                {
                    String stringBufferReceived = System.Text.Encoding.Default.GetString(ReceiveMessageBuffer);
                    Debug.Log("VirtualComInterface: ReceiveMessage: " + stringBufferReceived + ", Len :=" + ReceiveMessageBuffer.Length);
                    _receiveState = ReceiveStateEnum.RECEIVING;
                    _WorkerReceive.RunWorkerAsync(10000);
                }
                String stringBuffer = System.Text.Encoding.Default.GetString(bytes);
                Debug.Log("VirtualComInterface: SendMessage(" + stringBuffer + "), Time: " + Time.time);
                s.SendTo(bytes, IPEP);
                s.Close();
            }
        }


        private void worker_ReceiveMessage(object sender, DoWorkEventArgs e)
        {
            int len;
            Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint IPEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), PortToReceive);
            EndPoint receiveEP = (EndPoint)IPEP;
            s.Bind(receiveEP);

            (sender as BackgroundWorker).ReportProgress(0);
 
            len = s.ReceiveFrom(ReceiveMessageBuffer, SocketFlags.None, ref receiveEP);
            s.Close();            
            _receiveState = ReceiveStateEnum.RECEIVED;
            //e.Cancel = true;
            
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {




        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {


        }

    }
}
