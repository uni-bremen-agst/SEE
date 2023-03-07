using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

[System.Serializable]
public struct DeviceData{
    public string[] deviceNames;
    public Vector3 position;
    public Vector3 rotation;

    public DeviceData(string name, Vector3 pos, Vector3 rot)
    {
        deviceNames = new string[] { name };
        position = pos;
        rotation = rot;
    }
    public DeviceData(string[] names, Vector3 pos, Vector3 rot)
    {
        deviceNames = names;
        position = pos;
        rotation = rot;
    }
};


public class XRHandOffset : MonoBehaviour {
    [Tooltip("DO NOT CHANGE THIS UNLESS YOU ARE REDOING THE RELATIVE POSITIONS. This is the device that you are using to setup the innital proper orientation of the hand, all offsets are relative to this device")]
    public string defaultDevice = "Oculus";


    [SerializeField]
    public Transform[] rightOffsets, leftOffsets;

    [SerializeField]
    public DeviceData[] devices = new DeviceData[] {
        new DeviceData("Oculus", new Vector3(0.005f, -0.016f, 0.014f), new Vector3(48, 0, 15)),
        new DeviceData("Windows MR", new Vector3(0.003f, -0.005f, -0.078f), new Vector3(36, -12, 2)),
        new DeviceData(new string[]{"Vive", "HTC", "Index", "Cosmos", "Elite" }, new Vector3(0.015f, 0, 0.0412f), new Vector3(30, -17, 0))
    };

    bool offsetDone = false;
    bool hasProvider = false;

    void OnEnable(){
        InputDevices.deviceConnected += DeviceConnected;
        List<InputDevice> devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);

        foreach(var device in devices)
            DeviceConnected(device);
    }

    void OnDisable() {
        if(hasProvider) {
            InputDevices.deviceConnected -= DeviceConnected;
        }
    }


    internal void AdjustPositions(XRHandOffset otherOffset) {
        var defaultPos = GetDefaultPositionOffset() - otherOffset.GetDefaultPositionOffset();
        var defaultRot = GetDefaultRotationOffset() - otherOffset.GetDefaultRotationOffset();

        foreach(var leftOffset in leftOffsets) {
            leftOffset.localPosition += new Vector3(-defaultPos.x, defaultPos.y, defaultPos.z);
            leftOffset.localEulerAngles += new Vector3(defaultRot.x, -defaultRot.y, -defaultRot.z);
        }

        foreach(var rightOffset in rightOffsets) {
            rightOffset.localPosition += defaultPos;
            rightOffset.localEulerAngles += defaultRot;
        }
    }

    void DeviceConnected(InputDevice inputDevice) {
        if (inputDevice.characteristics != 0){
            Debug.Log("Devices Count: " + devices.Length, this);
            foreach (var device in devices){
                if (offsetDone)
                    break;

                Debug.Log("Device Names Count: " + device.deviceNames.Length, this);
                for (int i = 0; i < device.deviceNames.Length; i++){
                    Debug.Log(inputDevice.name, this);
                    if (inputDevice.name.Contains(device.deviceNames[i])){
                        var offsetPos = GetPositionOffset(defaultDevice, device.deviceNames[i]);
                        var offsetRot = GetRotationOffset(defaultDevice, device.deviceNames[i]);

                        foreach (var leftOffset in leftOffsets){
                            leftOffset.localPosition += new Vector3(-offsetPos.x, offsetPos.y, offsetPos.z);
                            leftOffset.localEulerAngles += new Vector3(offsetRot.x, -offsetRot.y, -offsetRot.z);
                        }

                        foreach (var rightOffset in rightOffsets){
                            rightOffset.localPosition += offsetPos;
                            rightOffset.localEulerAngles += offsetRot;
                        }

                        OnDisable();

                        offsetDone = true;
                        break;
                    }
                }
            }
        }
    }


    Vector3 GetPositionOffset(string from, string to) {
        if(from == to)
            return Vector3.zero;

        Vector3 fromPos, toPos = fromPos = Vector3.zero;
        foreach(var device in devices) {
            foreach(var deviceName in device.deviceNames) {
                if(deviceName == from)
                    fromPos = device.position;
                if(deviceName == to)
                    toPos = device.position;
            }
        }

        return (toPos - fromPos);
    }


    Vector3 GetRotationOffset(string from, string to) {
        if(from == to)
            return Vector3.zero;

        Vector3 fromPos, toPos = fromPos = Vector3.zero;
        foreach(var device in devices) {
            foreach(var deviceName in device.deviceNames) {
                if(deviceName == from)
                    fromPos = device.rotation;
                if(deviceName == to)
                    toPos = device.rotation;
            }
        }

        return (toPos - fromPos);
    }

    protected Vector3 GetDefaultPositionOffset() {

        Vector3 fromPos = Vector3.zero;
        foreach(var device in devices) {
            foreach(var deviceName in device.deviceNames) {
                if(deviceName == defaultDevice)
                    fromPos = device.position;
            }
        }

        return fromPos;
    }


    protected Vector3 GetDefaultRotationOffset() {
        Vector3 fromPos = Vector3.zero;
        foreach(var device in devices) {
            foreach(var deviceName in device.deviceNames) {
                if(deviceName == defaultDevice)
                    fromPos = device.rotation;
            }
        }

        return fromPos;
    }
}
