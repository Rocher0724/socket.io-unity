# socket.io-unity

on NOV. 1. 2019

Socket.IO Client Library for Unity (mono / .NET 4.x)

[socket.io-unity](https://github.com/floatinghotpot/socket.io-unity) by floatinghotpot is a very good project, but it had some problems with me. For example in the use of Action or Func. Exactly I could not use UniRx. 


## Installation

[Download](https://github.com/Rocher0724/socket.io-unity/releases) on release page socket.io-unity.unitypackage and import into Unity.

It's a C# file from a DLL inside a unity project.


## Usage

```cs
using Socket.Quobject.SocketIoClientDotNet.Client;

QSocket socket = IO.Socket ("http://localhost:3000");

socket.On (QSocket.EVENT_CONNECT, () => { socket.Emit ("hi"); });

socket.On ("hi", (data) => {
  Debug.Log (data);
  socket.Disconnect ();
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