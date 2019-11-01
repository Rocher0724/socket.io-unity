using System.Collections;
using System.Collections.Generic;
using Socket.Quobject.SocketIoClientDotNet.Client;
using UnityEngine;

public class TestObject : MonoBehaviour {
  void Start () {

    var socket = IO.Socket ("http://localhost:3000");
    socket.On (QSocket.EVENT_CONNECT, () => { socket.Emit ("hi"); });
    socket.On ("hi", (data) => {
      Debug.Log (data);
      socket.Disconnect ();
    });
  }

  void Update () { }
}