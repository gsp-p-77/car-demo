using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using vxlapi_NET;

public class CanBusInterfaceClass : MonoBehaviour
{
    // -----------------------------------------------------------------------------------------------
    // Global variables
    // -----------------------------------------------------------------------------------------------
    // Driver access through XLDriver (wrapper)
    private static XLDriver CANDemo = new XLDriver();
    private static String appName = "xlCANUnityAppNET";

    // Driver configuration
    private static XLClass.xl_driver_config driverConfig = new XLClass.xl_driver_config();


    // Variables required by XLDriver
    private static XLDefine.XL_HardwareType hwType = XLDefine.XL_HardwareType.XL_HWTYPE_NONE;
    private static uint hwIndex = 0;
    private static uint hwChannel = 0;
    private static int portHandle = -1;
    private static UInt64 accessMask = 0;
    private static UInt64 permissionMask = 0;
    private static UInt64 txMask = 0;
    private static UInt64 rxMask = 0;
    private static int txCi = -1;
    private static int rxCi = -1;
    private static EventWaitHandle xlEvWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, null);

    // RX thread
    private static Thread rxThread;
    private static bool blockRxThread = false;
    // -----------------------------------------------------------------------------------------------

    private bool CanDllInitialized = false;
    // Start is called before the first frame update
    void Start()
    {
        CanDllInitialized = false;
        XLDefine.XL_Status status;
        // Open XL Driver
        status = CANDemo.XL_OpenDriver();
        if (status != XLDefine.XL_Status.XL_SUCCESS)
        {
            Debug.Log("CANDemo.XL_OpenDriver failed");
        }
        else
        {
            Debug.Log("CANDemo.XL_OpenDriver succeeded");
        }


        // Get XL Driver configuration
        status = CANDemo.XL_GetDriverConfig(ref driverConfig);
        if (status != XLDefine.XL_Status.XL_SUCCESS)
        {
            Debug.Log("CANDemo.XL_GetDriverConfig(ref driverConfig) failed");
        }
        else
        {
            Debug.Log("CANDemo.XL_GetDriverConfig(ref driverConfig) succeeded");
        }
        

        // If the application name cannot be found in VCANCONF...
        if ((CANDemo.XL_GetApplConfig(appName, 0, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS) ||
            (CANDemo.XL_GetApplConfig(appName, 1, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN) != XLDefine.XL_Status.XL_SUCCESS))
        {
        //...create the item with two CAN channels
        CANDemo.XL_SetApplConfig(appName, 0, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
        CANDemo.XL_SetApplConfig(appName, 1, XLDefine.XL_HardwareType.XL_HWTYPE_NONE, 0, 0, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
        PrintAssignErrorAndPopupHwConf();
        }


        // Run Rx Thread        
        //rxThread = new Thread(new ThreadStart(RXThread));
        //rxThread.Start();

    }

    // Update is called once per frame
    void Update()
    {
        XLDefine.XL_Status status;

        if (!GetAppChannelAndTestIsOk(0, ref txMask, ref txCi) || !GetAppChannelAndTestIsOk(1, ref rxMask, ref rxCi))
        {
            PrintAssignErrorAndPopupHwConf();
        }
        else if (!CanDllInitialized)
        {
            CanDllInitialized = true;
            if ( GetAppChannelAndTestIsOk(0, ref txMask, ref txCi) && GetAppChannelAndTestIsOk(1, ref rxMask, ref rxCi))
            {
                accessMask = txMask | rxMask;
                permissionMask = accessMask;

                // Open port
                status = CANDemo.XL_OpenPort(ref portHandle, appName, accessMask, ref permissionMask, 1024, XLDefine.XL_InterfaceVersion.XL_INTERFACE_VERSION, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
                if (status != XLDefine.XL_Status.XL_SUCCESS)
                {
                    Debug.Log("CANDemo.XL_OpenPort failed");
                    CanDllInitialized = false;
                }

                // Check port
                status = CANDemo.XL_CanRequestChipState(portHandle, accessMask);
                if (status != XLDefine.XL_Status.XL_SUCCESS)
                {
                    Debug.Log("CANDemo.XL_CanRequestChipState failed");
                    CanDllInitialized = false;
                }

                // Activate channel
                status = CANDemo.XL_ActivateChannel(portHandle, accessMask, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN, XLDefine.XL_AC_Flags.XL_ACTIVATE_NONE);
                if (status != XLDefine.XL_Status.XL_SUCCESS)
                {
                    Debug.Log("CANDemo.XL_ActivateChannel failed");
                    CanDllInitialized = false;
                }

                // Initialize EventWaitHandle object with RX event handle provided by DLL
                int tempInt = -1;
                status = CANDemo.XL_SetNotification(portHandle, ref tempInt, 1);
                xlEvWaitHandle.SafeWaitHandle = new SafeWaitHandle(new IntPtr(tempInt), true);

                if (status != XLDefine.XL_Status.XL_SUCCESS)
                {
                    Debug.Log("CANDemo.XL_SetNotification failed");
                    CanDllInitialized = false;
                }

                // Reset time stamp clock
                status = CANDemo.XL_ResetClock(portHandle);
                if (status != XLDefine.XL_Status.XL_SUCCESS)
                {
                    Debug.Log("CANDemo.XL_ResetClock failed");
                    CanDllInitialized = false;
                }
            }
        }
    }
    public bool SendCanMessage(int canid, byte[] data, int len)
    {
        String text = "Data := ";
        int idx = 0;
        XLDefine.XL_Status txStatus;

        // Create an event collection with 2 messages (events)
        XLClass.xl_event_collection xlEventCollection = new XLClass.xl_event_collection(1);
        
        xlEventCollection.xlEvent[0].tagData.can_Msg.id = (uint)canid;
        foreach (byte byte_value in data)
        {
            text += byte_value.ToString() +", ";
            xlEventCollection.xlEvent[0].tagData.can_Msg.data[idx] = byte_value;
            xlEventCollection.xlEvent[0].tag = XLDefine.XL_EventTags.XL_TRANSMIT_MSG;
            idx++;
        }
        xlEventCollection.xlEvent[0].tagData.can_Msg.dlc = (ushort)(idx);


        Debug.Log("CAN Send Message: CAN ID :=" + canid + ", data := " + text);
      

        // Transmit events
        txStatus = CANDemo.XL_CanTransmit(portHandle, txMask, xlEventCollection);
        if (txStatus != XLDefine.XL_Status.XL_SUCCESS)
        {
            Debug.Log("Send Message Failed");
        }
        return true;

    }

    // -----------------------------------------------------------------------------------------------
    /// <summary>
    /// EVENT THREAD (RX)
    /// 
    /// RX thread waits for Vector interface events and displays filtered CAN messages.
    /// </summary>
    // ----------------------------------------------------------------------------------------------- 
    public static void RXThread()
    {
        // Create new object containing received data 
        XLClass.xl_event receivedEvent = new XLClass.xl_event();

        // Result of XL Driver function calls
        XLDefine.XL_Status xlStatus = XLDefine.XL_Status.XL_SUCCESS;


        // Note: this thread will be destroyed by MAIN
        while (true)
        {
            // Wait for hardware events
            if (xlEvWaitHandle.WaitOne(1000))
            {
                // ...init xlStatus first
                xlStatus = XLDefine.XL_Status.XL_SUCCESS;

                // afterwards: while hw queue is not empty...
                while (xlStatus != XLDefine.XL_Status.XL_ERR_QUEUE_IS_EMPTY)
                {
                    // ...block RX thread to generate RX-Queue overflows
                    while (blockRxThread) { Thread.Sleep(1000); }

                    // ...receive data from hardware.
                    xlStatus = CANDemo.XL_Receive(portHandle, ref receivedEvent);

                    //  If receiving succeed....
                    if (xlStatus == XLDefine.XL_Status.XL_SUCCESS)
                    {
                        if ((receivedEvent.flags & XLDefine.XL_MessageFlags.XL_EVENT_FLAG_OVERRUN) != 0)
                        {
                            Debug.Log("-- XL_EVENT_FLAG_OVERRUN --");
                        }

                        // ...and data is a Rx msg...
                        if (receivedEvent.tag == XLDefine.XL_EventTags.XL_RECEIVE_MSG)
                        {
                            if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_OVERRUN) != 0)
                            {
                                Debug.Log("-- XL_CAN_MSG_FLAG_OVERRUN --");
                            }

                            // ...check various flags
                            if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                                == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_ERROR_FRAME)
                            {
                                Debug.Log("ERROR FRAME");
                            }

                            else if ((receivedEvent.tagData.can_Msg.flags & XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                                == XLDefine.XL_MessageFlags.XL_CAN_MSG_FLAG_REMOTE_FRAME)
                            {
                                Debug.Log("REMOTE FRAME");
                            }

                            else
                            {
                                Debug.Log("CANDemo.XL_GetEventString(" + receivedEvent);
                            }
                        }
                    }
                }
            }
            // No event occurred
        }
    }

    // -----------------------------------------------------------------------------------------------
    // -----------------------------------------------------------------------------------------------
    /// <summary>
    /// Error message if channel assignment is not valid and popup VHwConfig, so the user can correct the assignment
    /// </summary>
    // -----------------------------------------------------------------------------------------------
    private static void PrintAssignErrorAndPopupHwConf()
    {
        Debug.Log("vxlapi_NET.dll error: Please assign channel");        
        CANDemo.XL_PopupHwConfig();        
    }
    // -----------------------------------------------------------------------------------------------
    /// <summary>
    /// Retrieve the application channel assignment and test if this channel can be opened
    /// </summary>
    // -----------------------------------------------------------------------------------------------
    private static bool GetAppChannelAndTestIsOk(uint appChIdx, ref UInt64 chMask, ref int chIdx)
    {
        XLDefine.XL_Status status = CANDemo.XL_GetApplConfig(appName, appChIdx, ref hwType, ref hwIndex, ref hwChannel, XLDefine.XL_BusTypes.XL_BUS_TYPE_CAN);
        if (status != XLDefine.XL_Status.XL_SUCCESS)
        {            
           // Debug.Log("GetAppChannelAndTestIsOk failed");
        }

        chMask = CANDemo.XL_GetChannelMask(hwType, (int)hwIndex, (int)hwChannel);
        chIdx = CANDemo.XL_GetChannelIndex(hwType, (int)hwIndex, (int)hwChannel);
        if (chIdx < 0 || chIdx >= driverConfig.channelCount)
        {
            // the (hwType, hwIndex, hwChannel) triplet stored in the application configuration does not refer to any available channel.
            return false;
        }

        // test if CAN is available on this channel
        return (driverConfig.channel[chIdx].channelBusCapabilities & XLDefine.XL_BusCapabilities.XL_BUS_ACTIVE_CAP_CAN) != 0;
    }
    // -----------------------------------------------------------------------------------------------

    // -----------------------------------------------------------------------------------------------
}
