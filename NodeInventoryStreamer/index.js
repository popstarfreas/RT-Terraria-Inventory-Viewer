var app = require('express')();
var serveStatic = require('serve-static');
var path = require('path');
var http = require('http').Server(app);
var io = require('socket.io')(http);
var fs = require('fs');

http.listen(3002);
app.use(require('less-middleware')(path.join(__dirname, 'public')));
app.use(serveStatic(path.resolve(__dirname, 'public')));

var isServer = {};
var server = null;
var sockets = [];
var socketsCount = 0;
var token = "";
var getPlayersQueue = [];

io.on('connection', function(socket) {
  sockets.push(socket);
  socketsCount++;
    console.log("Currently Connected: "+socketsCount);

  if (server !== null) {
    server.emit('getplayers', socket.id);
  } else {
    // No server; put them on the queue
    getPlayersQueue.push(socket);
  }

  socket.on('serverauth', function(data) {
    if (data == token) {
      isServer[socket.id] = true;
      server = socket;
      console.log(socket.id+": Successful attempt to auth as server.");
      if (getPlayersQueue.length > 0) {
        socket.emit('getplayers', getPlayersQueue[0].id);
      }
    } else {
      console.log(socket.id+": Failed attempt to auth as server using token: "+data);
    }
  });

  socket.on('slot', function(data) {
    if (isServer[socket.id])
      socket.broadcast.emit('slot', data);
  });

  socket.on('players', function(data) {
    if (isServer[socket.id]) {
      // Deal with the queue first
      var getPlayersQueueCount = getPlayersQueue.length;
      for (var i = 0; i < getPlayersQueueCount; i++)
          getPlayersQueue[i].emit('players', data.list);

      // Empty the queue
      getPlayersQueue = [];

      for (var i = 0; i < socketsCount; i++) {
        if (sockets[i].id == data.socketid) {
          sockets[i].emit('players', data.list);
          break;
        }
      }
    }
  });

  socket.on('playerjoin', function(data) {
    if (isServer[socket.id]) {
      socket.broadcast.emit('playerjoin', data);
    }
  });

  socket.on('playerleave', function(data) {
    if (isServer[socket.id]) {
      socket.broadcast.emit('playerleave', data);
    }
  });

  socket.on('getinventory', function(name) {
    if (server !== null) {
      server.emit('getinventory', {id: socket.id, name: name});
    }
  });

  socket.on('getinventory_response', function(data) {
    if (isServer[socket.id]) {
      for (var i = 0; i < socketsCount; i++) {
        if (sockets[i].id == data.socketid) {
          sockets[i].emit('inventory', data.state, data.inventory, data.index);
          break;
        }
      }
    }
  });

  socket.on('disconnect', function() {
    if (server !== null && server.id == socket.id) {
      server = null;
      console.log("Server Disconnected. "+socket.id);
    } else {
      console.log("Client Disconnected. "+socket.id);
    }

    var index = sockets.indexOf(socket);
    if (index > -1) {
      sockets.splice(index, 1);
      socketsCount--;
    }

    console.log("Currently Connected: "+socketsCount);
  });
});
