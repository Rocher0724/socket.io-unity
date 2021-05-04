
# socket.io-unity

on May. 4. 2021

Socket.IO Client Library for Unity (mono / .NET 4.x, Unity 2019.4.2.f1 LTS)

[socket.io-unity](https://github.com/floatinghotpot/socket.io-unity) by floatinghotpot is a very good project, but it had some problems with me. For example in the use of Action or Func. Exactly I could not use UniRx.


## Installation

[Download](https://github.com/Rocher0724/socket.io-unity/releases) on release page socket.io-unity.unitypackage and import into Unity.

It's a C# file from a DLL inside a unity project.

## Usage

### unity player settings - other settings - configuration

* Api Compatibility Level : .NET 4.x




```cs
// unity c# code
using Socket.Quobject.SocketIoClientDotNet.Client;
using UnityEngine;

public class TestObject : MonoBehaviour {
  private QSocket socket;

  void Start () {
    Debug.Log ("start");
    socket = IO.Socket ("http://localhost:3000");

    socket.On (QSocket.EVENT_CONNECT, () => {
      Debug.Log ("Connected");
      socket.Emit ("chat", "test");
    });

    socket.On ("chat", data => {
      Debug.Log ("data : " + data);
    });
  }

  private void OnDestroy () {
    socket.Disconnect ();
  }
}
```



```javascript
// node js code
const app = require('express')();
const http = require('http').createServer(app);
const io = require('socket.io')(http);
app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});
io.on('connection', (socket) => {
  console.log('a user connected');
  socket.on('chat message', (msg) => {
    io.emit('chat message', msg);
  });
  socket.on('disconnect', () => {
    console.log('user disconnected');
  });
});
http.listen(3000, () => {
  console.log('Connected at 3000');
});

```



## Features

This library supports all of the features the JS client does, including events, options and upgrading transport.

## Framework Versions

- Mono

- .NET 4.x
    - Unity project setting - Scripting Runtime Version : .NET 4.x Equivalent
    - Unity project setting - Api Compatibility Level : .NET 4.x
    - Unity Editor restart

## Demo

See [floatinghotpot's demo](https://github.com/floatinghotpot/socket.io-unity#demo) document


## Credit

Thanks to the authors of following projects:

* [SocketIoClientDotNet](https://github.com/Quobject/SocketIoClientDotNet) by Quobject, a Socket.IO Client Library for C#
* [WebSocket4Net](https://github.com/kerryjiang/WebSocket4Net) by Kerry Jiang, a .NET websocket client implementation.
* [Newtonsoft.Json](https://github.com/JamesNK/Newtonsoft.Json) by JamesNK, a popular high-performance JSON framework for .NET
* [floatinghotpot](https://github.com/floatinghotpot/socket.io-unity) by Raymond Xie, a Socket.IO Client Library for Unity


## Known Bug

npm module socket 2.0.4 reports a connection failure.

I used to use 1.7.4.


# Another Choice 

> [NHN Unity socketio client](https://github.com/nhn/socket.io-client-unity3d)  

NHN is a South Korean IT conglomerate.

The development version of this project has been developed until relatively recently.

I needed more from NHN unity socket, so I used [floatinghotpot](https://github.com/floatinghotpot/socket.io-unity), and the NHN unity socket client might be a better choice for you.

Try to use once.


