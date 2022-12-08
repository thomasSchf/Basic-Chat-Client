using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

class ClientSide {
    static void Main (string[] args) {
        // Instantiation
        string userName = "";
        string currentChatroom = "";
        Socket clientSocket;

        // Get user name
        do {
            Console.WriteLine("Please enter a username:");
            userName = Console.ReadLine();
        } while (userName.Equals("General"));

        // Connect to server
        IPAddress ip = IPAddress.Parse("127.0.0.1");
        IPEndPoint endpoint = new IPEndPoint(ip, 4242);
        clientSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        connectToServer(clientSocket, endpoint, userName);

        // Loops until program end
        while (!currentChatroom.Equals("exit()")) { ;
            // Instantiate inner values
            bool validChatroom = false;
            Queue<string> userInputs = new Queue<string>();
            bool exitChatroom = false;
            string userMessage = "";

            // Display possible chatrooms
            printChatrooms(clientSocket);

            // Loops until a valid chatroom is joined
            while (!validChatroom && !currentChatroom.Equals("exit()")) {
                // Get user request for chatroom
                Console.Write("Please enter the name of a chatroom to join, or enter exit(): ");
                Console.WriteLine("to exit the program");
                currentChatroom = Console.ReadLine();
                
                // Determine chatroom validity
                if (!currentChatroom.Equals("exit()")) {
                    validChatroom = isValidChatroom(clientSocket, currentChatroom);
                }
                else {
                    validChatroom = false;
                }

                // If user requests to end program, note that for later in the program
                if (!validChatroom && currentChatroom.Equals("exit()")) {
                    exitChatroom = true;
                }
                // Otherwise continue loop until valid chatroom obtained
                else if (!validChatroom) {
                    Console.WriteLine("\nInvalid name, please enter again");
                }
            }

            // Only create the input thread if the user intends to continue the program
            if (!currentChatroom.Equals("exit()")) {
                Thread userInputThread = new Thread(getInput);
                userInputThread.Start(userInputs);
            }

            // Loop until user wants to exit chatroom
            while (!exitChatroom) {
                // Print all queued messages from server
                printMessages(clientSocket);

                // Lock queue from input thread
                lock (userInputs) {
                    // Get all inputs in the queue
                    while (userInputs.TryDequeue(out userMessage)) {
                        // Prevents errors
                        if (userMessage == null) {userMessage = "";}

                        // Print the entered message in a formatted way
                        if (!userMessage.Equals("exit()")) {
                            clearLine();
                            Console.WriteLine(userName + ": \"" + userMessage + "\"    " + DateTime.Now);
                            sendMessage(clientSocket, userMessage);
                            userMessage = "";
                        }
                        // Exit if user requests it
                        else {
                            exitChatroom = true;
                            leaveChatroom(clientSocket);
                            currentChatroom = "";
                            break;
                        }
                    }
                }
            }
        }

    // Notify server of logout
    requestExit(clientSocket);
}

    // Obtains input infinitely until "exit()" is entered, saving to passed queue
    // Takes an Object that is CAST TO QUEUE<STRING> for threading purposes
    // Returns no value
    static void getInput(Object messageQueueObject) {
        // Cast object to queue<string> and declare buffer
        Queue<string> messageQueue;
        lock (messageQueueObject) {messageQueue = (Queue<string>)messageQueueObject;}
        string input = "";

        // Ask for inputs until the user exits
        while (!input.Equals("exit()")) {
            input = Console.ReadLine();

            // Prevents newline acceptance ?
            if (input != "\n" && input != "") {
                lock (messageQueue) {messageQueue.Enqueue(input);}
            }
        }
    }

    // Connects a socket to a server
    // Takes a socket object representing an unconnected user socket, an IPEndpoint to represent the
    //endpoint location, and a string representing the username to send the server
    // Returns no value
    static void connectToServer(Socket userSocket, IPEndPoint endPoint, string userName) {
        // Try connection
        try {
            // Connect to server with endpoint
            userSocket.Connect(endPoint);
            Console.WriteLine("\nConnected to Server\n");

            // Send User Name
            byte[] profileSend = Encoding.ASCII.GetBytes(userName);
            userSocket.Send(profileSend);

            // Recieve confirmation
            byte[] recievedData = new byte[16];
            userSocket.Receive(recievedData);
        }
        catch (Exception e) {
            Console.WriteLine(e.ToString());
        }
    }

    // Prints all currently avaliable chatrooms from the server
    // Takes a Socket object representing the client socket connected to the server
    // Returns no value
    static void printChatrooms(Socket userSocket) {
        // Send Request to obtain chatrooms in string form
        byte[] chatroomRequest = Encoding.ASCII.GetBytes("printChatrooms()");
        userSocket.Send(chatroomRequest);

        // Get chatrooms in string form
        byte[] chatroomRecieve = new byte[1024];
        int bytesRecieved = userSocket.Receive(chatroomRecieve);

        // Print Chatroom results
        Console.WriteLine(Encoding.ASCII.GetString(chatroomRecieve, 0, bytesRecieved));
    }

    // Determined if a chatroom is valid to join and, if so, joins it
    // Takes a Socket representing the server-connected socket and a string representing room name
    // Returns a boolean indicating that the chatroom is valid if true, invalid if false
    static bool isValidChatroom(Socket userSocket, string chatroomName) {
        // Send request to server to join chatroom
        byte[] joinRequest = Encoding.ASCII.GetBytes("isValidChatroom()," + chatroomName);
        userSocket.Send(joinRequest);

        // Recieve whether join was successful
        byte[] validChatroomRecieve = new byte[16];
        userSocket.Receive(validChatroomRecieve);

        // Return whether successful
        return Convert.ToBoolean(validChatroomRecieve[0]);
    }

    // Sends a request to the server to leave the current chatroom
    // Takes a Socket representing the server-connected socket
    // Returns no value
    static void leaveChatroom(Socket userSocket) {
        // Send request to server to leave chatroom
        byte[] joinRequest = Encoding.ASCII.GetBytes("requestExitChatroom()");
        userSocket.Send(joinRequest);

        // Recieve confirmation of leave
        byte[] confirmRecieve = new byte[1024];
        userSocket.Receive(confirmRecieve);
    }

    // Prints all messages queued for the user from the server
    // Takes a Socket representing the server-connected socket
    // Returns no value
    static void printMessages(Socket userSocket) {
        // Request Messages from server
        byte[] messageRequest = Encoding.ASCII.GetBytes("requestMessages()");
        userSocket.Send(messageRequest);

        // Recieve all queued messages
        byte[] messageRecieve = new byte[2048];
        int bytesRecieved = userSocket.Receive(messageRecieve);

        // Translate queued message to string, determine if there were any queed messages
        string message = Encoding.ASCII.GetString(messageRecieve, 0, bytesRecieved);
        bool messagesExist = !message.Equals("noMessage()");

        // Print queued messages if they exist
        if (messagesExist) {
            Console.Write(message);
        }
    }

    // Sends a typed message to a server for distribution to others in the chatroom
    // Takes a Socket representing the server-connected socket and a string representing the message
    // Returns no value
    static void sendMessage(Socket userSocket, string message) {
        // Notify server about message send, alongside sending it
        byte[] messageSend = Encoding.ASCII.GetBytes("sendMessage()," + message);
        userSocket.Send(messageSend);

        // Recieve confirmation before continuing
        byte[] confirmRecieve = new byte[1024];
        userSocket.Receive(confirmRecieve);
    }

    // Sends a request to the server to disconnect
    // Takes a Socket representing the server-connected socket
    // Returns no value
    static void requestExit(Socket userSocket) {
        // Send exit request to server
        byte[] exitFlag = Encoding.ASCII.GetBytes("requestExit()");
        userSocket.Send(exitFlag);

        // Recieve confirmation before continuing
        byte[] exitAccept = new byte[1024];
        userSocket.Receive(exitAccept);

        // Close the socket
        userSocket.Shutdown(SocketShutdown.Both);
        userSocket.Close();
    }

    // Clears the previous line in the console, override what was entered
    // Takes no value
    // Returns no value
    static void clearLine() {
        // Navigate up a row
        int cursorPoint = Console.CursorTop - 1;

        // Erase row
        Console.SetCursorPosition(0, cursorPoint);
        Console.Write(new string(' ', Console.WindowWidth)); 

        // Reset cursor position
        Console.SetCursorPosition(0, cursorPoint);
    }
}