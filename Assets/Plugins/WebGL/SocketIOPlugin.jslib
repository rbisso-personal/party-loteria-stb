var SocketIOPlugin = {
    $socketState: {
        socket: null,
        gameObjectName: null,
        isConnected: false,
        pendingUrl: null
    },

    SocketIO_Init: function(gameObjectNamePtr) {
        socketState.gameObjectName = UTF8ToString(gameObjectNamePtr);
        console.log('[SocketIO WebGL] Initialized with GameObject:', socketState.gameObjectName);
    },

    SocketIO_Connect: function(urlPtr) {
        var url = UTF8ToString(urlPtr);
        console.log('[SocketIO WebGL] Connecting to:', url);

        // Check if Socket.IO library is loaded
        if (typeof io === 'undefined') {
            console.error('[SocketIO WebGL] Socket.IO library not loaded! Add <script src="https://cdn.socket.io/4.7.2/socket.io.min.js"></script> to index.html');
            if (socketState.gameObjectName) {
                SendMessage(socketState.gameObjectName, 'OnWebGLConnectionError', 'Socket.IO library not loaded. Check index.html.');
            }
            return;
        }

        // Disconnect existing socket if any
        if (socketState.socket) {
            socketState.socket.disconnect();
        }

        socketState.socket = io(url, {
            transports: ['websocket'],
            reconnection: true,
            reconnectionAttempts: 10,
            reconnectionDelay: 1000,
            reconnectionDelayMax: 5000
        });

        socketState.socket.on('connect', function() {
            console.log('[SocketIO WebGL] Connected, socket.id:', socketState.socket.id);
            socketState.isConnected = true;
            SendMessage(socketState.gameObjectName, 'OnWebGLConnected', socketState.socket.id || '');
        });

        socketState.socket.on('disconnect', function(reason) {
            console.log('[SocketIO WebGL] Disconnected:', reason);
            socketState.isConnected = false;
            SendMessage(socketState.gameObjectName, 'OnWebGLDisconnected', reason);
        });

        socketState.socket.on('connect_error', function(error) {
            console.error('[SocketIO WebGL] Connection error:', error);
            SendMessage(socketState.gameObjectName, 'OnWebGLConnectionError', error.message || 'Connection error');
        });

        // Room events
        socketState.socket.on('room-created', function(data) {
            console.log('[SocketIO WebGL] room-created:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLRoomCreated', JSON.stringify(data));
        });

        socketState.socket.on('player-joined', function(data) {
            console.log('[SocketIO WebGL] player-joined:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLPlayerJoined', JSON.stringify(data));
        });

        socketState.socket.on('player-left', function(data) {
            console.log('[SocketIO WebGL] player-left:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLPlayerLeft', JSON.stringify(data));
        });

        socketState.socket.on('update-lobby', function(data) {
            console.log('[SocketIO WebGL] update-lobby:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLLobbyUpdate', JSON.stringify(data));
        });

        // Game events
        socketState.socket.on('game-started', function(data) {
            console.log('[SocketIO WebGL] game-started:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLGameStarted', JSON.stringify(data));
        });

        socketState.socket.on('card-drawn', function(data) {
            console.log('[SocketIO WebGL] card-drawn:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLCardDrawn', JSON.stringify(data));
        });

        socketState.socket.on('win-claimed', function(data) {
            console.log('[SocketIO WebGL] win-claimed:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLWinClaimed', JSON.stringify(data));
        });

        socketState.socket.on('win-verified', function(data) {
            console.log('[SocketIO WebGL] win-verified:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLWinVerified', JSON.stringify(data));
        });

        socketState.socket.on('win-rejected', function(data) {
            console.log('[SocketIO WebGL] win-rejected:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLWinRejected', JSON.stringify(data));
        });

        socketState.socket.on('game-paused', function(data) {
            console.log('[SocketIO WebGL] game-paused');
            SendMessage(socketState.gameObjectName, 'OnWebGLGamePaused', '');
        });

        socketState.socket.on('game-resumed', function(data) {
            console.log('[SocketIO WebGL] game-resumed');
            SendMessage(socketState.gameObjectName, 'OnWebGLGameResumed', '');
        });

        socketState.socket.on('game-over', function(data) {
            console.log('[SocketIO WebGL] game-over:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLGameOver', JSON.stringify(data));
        });

        socketState.socket.on('game-reset', function(data) {
            console.log('[SocketIO WebGL] game-reset');
            SendMessage(socketState.gameObjectName, 'OnWebGLGameReset', '');
        });

        socketState.socket.on('game-error', function(data) {
            console.log('[SocketIO WebGL] game-error:', data);
            SendMessage(socketState.gameObjectName, 'OnWebGLGameError', JSON.stringify(data));
        });
    },

    SocketIO_Disconnect: function() {
        if (socketState.socket) {
            socketState.socket.disconnect();
            socketState.socket = null;
            socketState.isConnected = false;
        }
    },

    SocketIO_Emit: function(eventNamePtr, dataPtr) {
        if (!socketState.socket || !socketState.isConnected) {
            console.warn('[SocketIO WebGL] Cannot emit - not connected');
            return;
        }
        var eventName = UTF8ToString(eventNamePtr);
        var dataStr = UTF8ToString(dataPtr);

        console.log('[SocketIO WebGL] Emitting:', eventName, dataStr);

        if (dataStr && dataStr.length > 0) {
            try {
                var data = JSON.parse(dataStr);
                socketState.socket.emit(eventName, data);
            } catch (e) {
                console.error('[SocketIO WebGL] Failed to parse emit data:', e);
            }
        } else {
            socketState.socket.emit(eventName);
        }
    },

    SocketIO_IsConnected: function() {
        return socketState.isConnected ? 1 : 0;
    }
};

autoAddDeps(SocketIOPlugin, '$socketState');
mergeInto(LibraryManager.library, SocketIOPlugin);
