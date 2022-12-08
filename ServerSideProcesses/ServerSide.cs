using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace ServerSide {

class ServerSide {
    static void Main(string[] args) {
        // Declare General variables
        List<Chatroom> chatrooms = new List<Chatroom>();
        chatrooms.Add(new Chatroom(true));
        List<ServerProfile> users = new List<ServerProfile>();

        // Start Separated Listening Thread
        Thread connectionThread = new Thread(listenForUser);
        connectionThread.Start(users);
        
        // Infinite loop--should never end until outside intervention
        while (true) {
            // Lock the thread-shared list of users every time it is accessed
            lock(users) {
                // Find the requests of every user
                for (int i = 0; i < users.Count(); ++i) {
                    // Isolate the current user
                    ServerProfile profile = users[i];
                    Queue<List<string>> profileRequests = profile.getRequests();

                    // Loop through all of the user's requests
                    List<string> request;
                    while (profileRequests.TryDequeue(out request)) {
                        // Find the actual request separated from its data
                        string baseRequest = request[0];

                        // Do appropriate response to request
                        if (baseRequest.Equals("printChatrooms()")) {sendChatrooms(profile, users);}
                        else if (baseRequest.Equals("isValidChatroom()")) {attemptChatroomJoin(profile, request[1], users, chatrooms);}
                        else if (baseRequest.Equals("requestExitChatroom()")) {leaveChatroom(profile, chatrooms);}
                        else if (baseRequest.Equals("requestMessages()")) {recieveMessages(profile);}
                        else if (baseRequest.Equals("sendMessage()")) {sendMessages(profile, request[1], chatrooms);}
                        else if (baseRequest.Equals("requestExit()")) {
                            requestExit(profile, users);
                            --i;
                        }
                        // Something has gone wrong if the request is not exactly equal to the above
                        else {throw new Exception();}
                    }
                }
            }
        }
    }

    // Infinitely Listens for any connections to the server, adding when detected
    // Takes an Object that is CAST TO LIST<SERVERPROFILE> representing the list of users connected
    // Returns no value
    static void listenForUser(Object usersObject) {
        // Declaring maximum amount of users
        int maxUsers = 20;

        // Creating the server socket
        List<ServerProfile> users;
        lock (usersObject) {users = (List<ServerProfile>)usersObject;}
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        IPEndPoint endpoint = new IPEndPoint(ip, 4242);
        Socket serverSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        serverSocket.Bind(endpoint);

        // Infinite loop that listens for connections
        while (true) {
            // Start listening for connections
            serverSocket.Listen(maxUsers);

            // Accept connections
            Socket userSocket = serverSocket.Accept();

            // Take profile name from client, saving
            byte[] userProfileData = new byte[1024];
            int bytesRecieved = userSocket.Receive(userProfileData);
            string profileName = Encoding.ASCII.GetString(userProfileData, 0, bytesRecieved);

            // Note request success, creating the profile and adding it to the list of users
            Console.WriteLine(profileName + " has requested log on");
            ServerProfile newProfile = new ServerProfile(profileName, userSocket);
            lock(users) {users.Add(newProfile);}

            // Begin general user thread to obtain input from client until they disconnect
            Thread newUserThread = new Thread(userThread);
            newUserThread.Start(newProfile);

            // Send confirmation to user, noting
            byte[] logonConfirmation = new byte[1];
            logonConfirmation[0] = Convert.ToByte(true);
            userSocket.Send(logonConfirmation);
            Console.WriteLine(profileName + " Confirmed Log on");
        }
    }

    // Accepts requests from client socket until a request to disconnect is made
    // Takes an Object that is CAST TO SERVERPROFILE representing the client profile
    // Returns no value
    static void userThread(Object userProfile) {
        // Casts object to ServerPrifle, initialize incomingRequests
        ServerProfile user = (ServerProfile)userProfile;
        string incomingRequest = "";

        // Loop until client requests a disconnect
        while (!incomingRequest.Equals("requestExit()")) {
            // Declare buffer
            byte[] incomingData = new byte[2048];

            // Recieve and translate request
            int bytesRecieved = user.getSocket().Receive(incomingData);
            incomingRequest = Encoding.ASCII.GetString(incomingData, 0, bytesRecieved);

            // Add the request to the user, locking for precaution
            lock (user) {
                user.addRequest(incomingRequest);
            }

            // End the thread if the user request is a disconnect
            if (incomingRequest.Equals("requestExit()")) {
                // i hate that c# doesnt let you set objects to null
                user = new ServerProfile();
            }
        }
    }

    // Sends all of the open chatrooms to the client
    // Takes a ServerProfile representing the cleint's profile and a List<ServerProfile> representing
    //the list of users connected to the server
    // Returns no value
    static void sendChatrooms(ServerProfile userProfile, List<ServerProfile> users) {
        // Note request
        Console.WriteLine(userProfile.getName() + " Requested printChatrooms()");

        // Declare buffer
        string buffer = "\n***Chatrooms***\nGeneral \n";
        
        // Precautionary lock of thread-shared users
        lock (users){
            // Ever other user is a possible chatroom
            foreach (ServerProfile user in users) {
                if (user != userProfile) {
                    buffer = buffer + user.getName() + "\n";
                }
            }
        }

        // End the buffer
        buffer = buffer + "***************\n";

        // Send the chatrooms
        byte[] messageRequest = Encoding.ASCII.GetBytes(buffer);
        userProfile.getSocket().Send(messageRequest);

        // Note Success
        Console.WriteLine(userProfile.getName() + " Succesfully did printChatrooms()");
    }

    // Attempts to allow a client to join a chatroom
    // Takes a ServerProfile representing the client, a string representing the room name,
    //a List<ServerProfile> representing the users connected to the server, and a List<Chatroom>
    //representing the list of all active chatrooms
    // Returns no value
    static void attemptChatroomJoin(ServerProfile userProfile, string chatName, List<ServerProfile> users, List<Chatroom> chatrooms) {
        // Note request
        Console.WriteLine(userProfile.getName() + " Requested isValidChatroom()");

        // Declare variables
        ServerProfile targetUser = userProfile;
        bool accomplishedJoin = false;
        bool choseDM = false;
        bool dmAlreadyOpen = false;
        byte[] accomplishedJoinData = new byte[16];

        // Autojoin if the name is general
        if (chatName.Equals("General")) {
            accomplishedJoin = true;

            // Notify users in general
            foreach (ServerProfile profile in chatrooms[0].getActiveUsers()) {
                profile.addMessage(userProfile.getName() + " has joined!");
            }

            // Add user to general
            chatrooms[0].attemptAddUser(userProfile);
            userProfile.addMessage("\nChatroom Join Successful, enter exit() to leave");
        }
        // Otherwise check if a user has the same name as the chat name
        else {
            // Precautionary lock, thread-shared
            lock(users) {
                // Check if the user exists
                foreach (ServerProfile user in users) {
                    if (chatName.Equals(user.getName()) && user != userProfile) {
                        choseDM = true;
                        accomplishedJoin = true;
                        targetUser = user;
                    }
                }
            }
        }

        // When the user chatroom chosen
        if (choseDM) {
            // Determine if the two users already have a chat open
            foreach (Chatroom room in chatrooms) {
                // If the user belongs in the current chat--chat already open
                if (!room.isGeneral() && room.checkBelong(userProfile)) {
                    dmAlreadyOpen = true;
                    room.attemptAddUser(userProfile);
                    userProfile.addMessage("\nChatroom Join Successful, enter exit() to leave");

                    // Notify other user of join
                    targetUser.addMessage(userProfile.getName() + " has joined!");
                }
            }

            // If the two users do not already have a chat open
            if (!dmAlreadyOpen) {
                chatrooms.Add(new Chatroom(userProfile, targetUser));
                userProfile.addMessage("\nChatroom Join Successful, enter exit() to leave");

                // Notify other user of the request
                targetUser.addMessage(userProfile.getName() + " is requesting to DM you!");
            }
        }

        // Notify client about succes of the join
        accomplishedJoinData[0] = Convert.ToByte(accomplishedJoin);
        userProfile.getSocket().Send(accomplishedJoinData);

        // Note request success
        Console.WriteLine(userProfile.getName() + " Succesfully did isValidChatroom()");
    }

    // Removes the client from their chatroom
    // Takes a ServerProfile representing the client profile and a List<Chatroom> representing the active
    //chatrooms
    // Returns no value
    static void leaveChatroom(ServerProfile userProfile, List<Chatroom> chatrooms) {
        // Note Request
        Console.WriteLine(userProfile.getName() + " Requested requestExitChatroom()");

        // Should at least be in general
        Chatroom userOccupiedChatroom = chatrooms[0];

        // Find the chatroom the client occupies
        foreach (Chatroom chatroom in chatrooms) {
            if (chatroom.checkPresence(userProfile)) {
                userOccupiedChatroom = chatroom;
            }
        }

        // Notify all active users in the chat room of the client departure
        foreach (ServerProfile profile in userOccupiedChatroom.getActiveUsers()) {
            if (profile != userProfile) {
                profile.addMessage(userProfile.getName() + " has left the chat");
            }
        }

        // Remove user from chatroom
        userOccupiedChatroom.attemptRemoveUser(userProfile);

        // If the chat is a Direct Message and no more users are in it, destroy it
        if (userOccupiedChatroom.getNumActiveUsers() == 0 && !userOccupiedChatroom.isGeneral()) {
            chatrooms.Remove(userOccupiedChatroom);
        }

        // Send confirmation to client
        byte[] confirmationBytes = Encoding.ASCII.GetBytes("Confirm");
        userProfile.getSocket().Send(confirmationBytes);

        // Note sucess
        Console.WriteLine(userProfile.getName() + " Succesfully did requestExitChatroom()");
    }

    // Returns the messages queued for the client to them in a formatted form
    // Takes a ServerProfile representing the client data
    // Returns no value
    static void recieveMessages(ServerProfile userProfile) {
        // Declarations
        Socket userSocket = userProfile.getSocket();
        Queue<string> queuedMessages = userProfile.returnMessages();
        string buffer = "";
        var tempString = "";

        // Add all queued messages to a buffer
        while (queuedMessages.TryDequeue(out tempString)) {
            buffer = buffer + tempString + "\n";
        }

        // Note lack of messages if queue is empty
        if (buffer == "") {
            buffer = "noMessage()";
        }

        // Send client the buffer
        byte[] messageRequest = Encoding.ASCII.GetBytes(buffer);
        userSocket.Send(messageRequest);
    }

    // Sends a message from the client to all those in the client's chatroom
    // Takes a ServerProfile representing client data, a string representing the sent message, and a
    //List<Chatroom> representing all active chatrooms in the server
    // Returns no value
    static void sendMessages(ServerProfile userProfile, string message, List<Chatroom> chatrooms) {
        // Note request
        Console.WriteLine(userProfile.getName() + " Requested sendMessage()");

        // Should at least be in general
        Chatroom userOccupiedChatroom = chatrooms[0];

        // Find the client occupied chatroom
        foreach (Chatroom chatroom in chatrooms) {
            if (chatroom.checkPresence(userProfile)) {
                userOccupiedChatroom = chatroom;
            }
        }

        // For each user in the chatroom, add the message to their queue
        foreach (ServerProfile reciever in userOccupiedChatroom.getActiveUsers()) {
            if (reciever != userProfile) {
                reciever.addMessage(userProfile.getName() + ": \"" + message + "\"");
            }
        }

        // Send Client Confirmation
        byte[] confirmationBytes = Encoding.ASCII.GetBytes("Confirm");
        userProfile.getSocket().Send(confirmationBytes);

        // Note sucecss
        Console.WriteLine(userProfile.getName() + " Succesfully did sendMessage()");
    }

    // Disconnects a client from the server
    // Takes a ServerProfile representing the client data and a List<ServerProfile> representing all
    //users in the server
    // Returns no value
    static void requestExit(ServerProfile userProfile, List<ServerProfile> users) {
        // Note request
        Console.WriteLine(userProfile.getName() + " Requested requestExit()");
        
        // Confirm request to client
        Socket userSocket = userProfile.getSocket();
        byte[] exitRequest = Encoding.ASCII.GetBytes("confirm");
        userSocket.Send(exitRequest);

        // Lock thread-shared list for modification
        lock(users) {
            // Remove client from list of users
            users.Remove(userProfile);
        }

        // Close the user socket
        userSocket.Shutdown(SocketShutdown.Both);
        userSocket.Close();

        // Note success
        Console.WriteLine(userProfile.getName() + " Succesfully did requestExit()");
    }
}
}