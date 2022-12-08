using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ServerSide {
class ServerProfile {
    private string name = "";
    private Queue<string> incomingMessages = new Queue<string>();
    private Socket socket;
    private Queue<List<string>> requests = new Queue<List<string>>();

    //Default constructor, simply sets the name of the profile
    public ServerProfile(string name, Socket socket) {
        this.name = name;
        this.socket = socket;
    }

    // Allows for creation of a useless profile to allow for deletion
    public ServerProfile() {
        name = "";
    }

    // Getter for the desired profile's name
    // Takes no value
    // Returns a string representing the name of the profile
    public string getName() {
        return name;
    }

    // Setter for desired profile's name, given a later change
    // Takes a string representing the new name
    // Returns no value
    public void rename(string name) {
        this.name = name;
    }

    // Getter for the queue of messages
    // Takes no value
    // Returns a Queue<string> representing the queued messages
    public Queue<string> returnMessages() {
        return incomingMessages;
    }

    // Formats a message, sends it to the incomingMessages queue
    // Takes a string representing the unformatted message to add
    // Returns no value
    public void addMessage(string message) {
        incomingMessages.Enqueue(message + "    -" + DateTime.Now);
    }

    // Getter for the profile's socket
    // Takes no value
    // Returns a Socket representing the profile's socket
    public Socket getSocket() {
        return socket;
    }

    // Setter for the profile's socket
    // Takes a Socket representing the new socket for the profile
    // Returns no value
    public void setSocket(Socket socket) {
        this.socket = socket;
    }

    // Getter for the profile's queue of Requests
    // Takes no value
    // Returns the Queue<List<string>> representing the queue of requests
    public Queue<List<string>> getRequests() {
        return requests;
    }

    // Adds a request to the profile's queue of requests using an unformatted request
    // Takes a string representing the unformatted request
    // Returns no value
    public void addRequest(string request) {
        List<string> formattedRequest = new List<string>();
        int currIndex = 0;
        int indexOfComma = 0;
        
        // Navigate the string until the end of it
        while (indexOfComma != -1 && currIndex < request.Length) {
            // Identify any commas to remove
            indexOfComma = request.IndexOf(",", currIndex);

            // Given no commas, just add the section to the list of parts
            if (indexOfComma == -1) {
                formattedRequest.Add(request.Substring(currIndex));
            }
            // If there is a comma, cut the string to before it, add it to the list, keep going
            else {
                formattedRequest.Add(request.Substring(currIndex, indexOfComma));
                currIndex = indexOfComma + 1;
            }
        }

        // Add the formatted request to the queue of requests
        this.requests.Enqueue(formattedRequest);
    }
}
}