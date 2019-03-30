using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace CraftWar
{
    public class NetworkManager
    {
        public const int defaultPort = 7832;
        public const int defaultMaxPlayers = 101;

        //Object
        public GameClient localGameClient;
        public string ipAddress;
        public string username;
        public GameServer gameServer = null;
        public int port;
        private int maxPlayers;
        public bool host = false;
        public string messagesToSendToServer = "";

        public NetworkManager(string ipAddress, string username, int port = defaultPort, int maxPlayers = defaultMaxPlayers)
        {
            this.ipAddress = ipAddress;
            this.username = username;
            this.port = port;
            this.maxPlayers = maxPlayers;
        }

        public void updateGameServer()
        {
            if (gameServer != null)
            {
                gameServer.relayInformation();
            }
        }

        public void sendInformationToGameServer()
        {
            if (!string.IsNullOrEmpty(messagesToSendToServer))
            {
                //TEMP OPTIMIZE PLAYER
                localGameClient.sendString(messagesToSendToServer + Game1.mainPlayer.serverInformationString);
                messagesToSendToServer = "";
            }
            else
            {
                localGameClient.sendString(Game1.mainPlayer.serverInformationString);
            }
        }

        public void receiveInformationFromGameServer()
        {
            string[] messages = localGameClient.readIncomingAsString().Split(GameServer.messageSeparator);
            foreach (string b in messages)
            {
                string[] data = b.Split(GameServer.dataSeparator);
                if (data[0] == ((int)GameServer.NetworkKeyword.mapInfo).ToString())
                {
                    //Map related message
                    Game1.currentMap.interpretServerMessage(data);
                }
                if (data[0] == ((int)GameServer.NetworkKeyword.playerInfo).ToString())
                {
                    OtherPlayer.interpretPlayerServerMessage(data);
                }
            }
        }

        public bool gameServerAvailable
        {
            get
            {
                try
                {
                    TcpClient testClient = new TcpClient(ipAddress, port);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        public void connect()
        {
            if (gameServerAvailable)
            {
                //Connect as client
                localGameClient = new GameClient(new TcpClient(ipAddress, port));
            }
            else
            {
                //Create new server
                gameServer = new GameServer(ipAddress, port, maxPlayers);
                localGameClient = new GameClient(new TcpClient(ipAddress, port), 0);
                host = true;
            }
        }
    }

    public class GameServer
    {
        public enum NetworkKeyword
        {
            hostInfo,
            gameClientDisconnect,
            mapInfo,
            tileInfo,
            tileChange,
            tileNull,
            playerInfo,
            tileBreakOverlay,
            mapLogRequest,
            hostRequest,
            trueIdentifier,
            falseIdentifier,
            entityAdd,
            dropAdd
        }
        public const char dataSeparator = '|';
        public const char messageSeparator = ';';

        //Object
        public List<GameClient> gameClients = new List<GameClient>();
        public TcpListener tcpListener;
        public string ipAddress;
        public int port;
        public int maxClients;
        public int mapSeed;
        public string messagesToSendToGameClients;
        private string mapUpdateMessageLog = "";

        public GameServer(string ipAddress, int port, int maxClients)
        {
            tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
            tcpListener.Start();
            this.maxClients = maxClients;
            Random random = new Random();
            mapSeed = random.Next();
            clientAcceptor();
        }

        public async void clientAcceptor()
        {
            while (true)
            {
                TcpClient clientReceived = await tcpListener.AcceptTcpClientAsync();
                if (gameClients.Count < maxClients)
                {
                    //Can add client
                    gameClients.Add(new GameClient(clientReceived, gameClients.Count));

                    //Send initial host information
                    gameClients[gameClients.Count - 1].sendString(((int)NetworkKeyword.hostInfo).ToString() + dataSeparator + mapSeed + dataSeparator + gameClients[gameClients.Count - 1].clientID + messageSeparator);

                    //Sends map change log
                    gameClients[gameClients.Count - 1].sendString(mapUpdateMessageLog);
                }
                else
                {
                    clientReceived.Close();
                }
            }
        }

        public async void relayInformation()
        {
            await Task.Run(() =>
            {
                foreach (GameClient b in gameClients)
                {
                    if (b.enabled)
                    {
                        //Attempt to read information
                        try
                        {
                            if (b.tcpClient.Available > 0)
                            {
                                //Read information
                                byte[] buffer = new byte[b.tcpClient.ReceiveBufferSize];
                                int bytesRead = b.readIncomingAsBytes(ref buffer);

                                //Interpret information being sent
                                interpretInformation(Encoding.ASCII.GetString(buffer, 0, bytesRead), b);

                                //Send information to clients
                                relayMessageToClients(buffer, bytesRead, b);
                            }
                        }
                        catch
                        {
                            b.disable(ref messagesToSendToGameClients);
                        }
                    }
                }

                relayServerInformation();
            });
        }

        private void interpretInformation(string message, GameClient sender)
        {
            string[] messages = message.Split(messageSeparator);

            foreach (string b in messages)
            {
                string[] data = b.Split(dataSeparator);

                //Check for tile check map data to log
                if (data[0] == ((int)NetworkKeyword.mapInfo).ToString() && data[1] == ((int)NetworkKeyword.tileInfo).ToString() && data[2] == ((int)GameServer.NetworkKeyword.tileChange).ToString())
                {
                    //Log information with host tag
                    mapUpdateMessageLog += b.Replace(((int)NetworkKeyword.falseIdentifier).ToString(), ((int)NetworkKeyword.trueIdentifier).ToString()) + messageSeparator;
                }
                
                //Check if the message is a host message
                if (data[0] == ((int)NetworkKeyword.hostRequest).ToString())
                {
                    interpretServerRequest(data, sender);
                }
            }
        }

        private void interpretServerRequest(string[] data, GameClient sender)
        {
            //Check for request for map log
            if (data[1] == ((int)NetworkKeyword.mapLogRequest).ToString())
            {
                sender.sendString(mapUpdateMessageLog);
            }
        }

        private void relayMessageToClients(byte[] buffer, int bufferLength, GameClient sender)
        {
            foreach (GameClient b in gameClients)
            {
                if (b.enabled && b != sender)
                {
                    try
                    {
                        b.sendBytes(buffer, bufferLength);
                    }
                    catch
                    {
                        b.disable(ref messagesToSendToGameClients);
                    }
                }
            }
        }

        private void relayServerInformation()
        {
            if (!string.IsNullOrEmpty(messagesToSendToGameClients))
            {
                foreach (GameClient b in gameClients)
                {
                    if (b.enabled)
                    {
                        try
                        {
                            b.sendString(messagesToSendToGameClients);
                        }
                        catch
                        {
                            b.disable(ref messagesToSendToGameClients);
                        }
                    }
                }

                messagesToSendToGameClients = "";
            }
        }
    }

    public class GameClient
    {
        public TcpClient tcpClient;
        public NetworkStream networkStream;
        public bool enabled = true;
        public int clientID;

        public GameClient(TcpClient tcpClient, int clientID = -1)
        {
            this.tcpClient = tcpClient;
            networkStream = tcpClient.GetStream();
            this.clientID = clientID;
        }

        public async void sendString(string message)
        {
            try
            {
                byte[] buffer = Encoding.ASCII.GetBytes(message);
                await networkStream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch
            {
            }
        }

        public async void sendBytes(byte[] buffer, int bufferLength)
        {
            try
            {
                await networkStream.WriteAsync(buffer, 0, bufferLength);
            }
            catch
            {
                string disableString = "";
                disable(ref disableString);
            }
        }

        public string readIncomingAsString()
        {
            if (tcpClient.Available > 0)
            {
                byte[] buffer = new byte[tcpClient.ReceiveBufferSize];
                int bytesRead = networkStream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
                return Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }
            else
            {
                //Avoid null pointer exception
                return "";
            }
        }

        public int readIncomingAsBytes(ref byte[] buffer)
        {
            if (tcpClient.Available > 0)
            {
                return networkStream.Read(buffer, 0, tcpClient.ReceiveBufferSize);
            }
            else
            {
                return 0;
            }
        }

        public void disable(ref string messageQueue)
        {
            enabled = false;
            networkStream.Close();
            tcpClient.Close();
            messageQueue += ((int)GameServer.NetworkKeyword.gameClientDisconnect).ToString() + GameServer.dataSeparator + clientID + GameServer.messageSeparator;
        }
    }
}
