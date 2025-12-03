using System;
using System.Collections.Generic;

namespace PartyLoteria.Data
{
    public enum GamePhase
    {
        Waiting,
        Playing,
        Paused,
        Finished
    }

    [Serializable]
    public class Player
    {
        public string id;
        public string name;
        public bool isReady;
    }

    [Serializable]
    public class Winner
    {
        public string id;
        public string name;
        public int[] pattern;
    }

    [Serializable]
    public class RoomCreatedData
    {
        public string roomCode;
    }

    [Serializable]
    public class PlayerJoinedData
    {
        public Player player;
        public int playerCount;
    }

    [Serializable]
    public class PlayerLeftData
    {
        public string playerId;
        public string playerName;
        public int playerCount;
    }

    [Serializable]
    public class GameStartedData
    {
        public string winPattern;
        public int drawSpeed;
        public int totalCards;
        public int playerCount;
    }

    [Serializable]
    public class CardDrawnData
    {
        public Card card;
        public int cardNumber;
        public int totalCards;
        public int[] drawnCardIds;
    }

    [Serializable]
    public class GameOverData
    {
        public string reason;
        public Winner winner;
    }

    [Serializable]
    public class LobbyUpdateData
    {
        public Player[] players;
        public string hostId;
    }

    [Serializable]
    public class GameErrorData
    {
        public string message;
    }

    [Serializable]
    public class WinClaimedData
    {
        public string playerId;
        public string playerName;
    }

    [Serializable]
    public class WinVerifiedData
    {
        public string playerId;
        public string playerName;
        public int[] winningPattern;
    }

    [Serializable]
    public class WinRejectedData
    {
        public string playerId;
        public string playerName;
        public string reason;
    }
}
