using System;
using System.Collections.Generic;

namespace ServerSide {
class Chatroom {
    private List<ServerProfile> allBelonging;
    private bool is_general = false;
    private List<ServerProfile> active_users;
    private int maxSize = 20;

    // Regular constructor, saves passed 'engager' and 'target,' and makes the engager the only active
    //user
    public Chatroom(ServerProfile engager, ServerProfile target) {
        // Engager by circumstance is the only active user at declaration
        this.active_users = new List<ServerProfile>();
        this.active_users.Add(engager); 

        // Makes both Profiles belong
        this.allBelonging = new List<ServerProfile>();
        this.allBelonging.Add(engager);
        this.allBelonging.Add(target);

        this.maxSize = 2;
    }

    // General Chat constructor, when passed a boolean true the chat is set to general 
    public Chatroom(bool generalPass) {
        // If the passed value is false something has gone wrong
        if (!generalPass) {throw new Exception();}

        this.is_general = generalPass;
        this.active_users = new List<ServerProfile>();
        this.allBelonging = new List<ServerProfile>();
    }

    // Checks if this specific chatroom happens to be general
    // Takes no value
    // Returns the boolean representing whether or not the chat is General
    public bool isGeneral() {
        return this.is_general;
    }

    // Attempts to add a user to chatroom
    // Takes a ServerProfile representing the user to try and add to the chatroom
    // Returns a boolean which is False if user already is in the chat or is not allowed to join it
    public bool attemptAddUser(ServerProfile addedUser) {
        // If the user does not belong or is already in the chat or if adding the user would
        //exceed the chat's max amount of active users
        if (!checkBelong(addedUser) || checkPresence(addedUser) 
            || ((getNumActiveUsers() + 1) > getMaxSize())) {return false;}
        
        active_users.Add(addedUser);
        return true;
    }

    // Attempts to remove a user from a chatroom
    // Takes a ServerProfile representing the user to try and remove from the chatroom
    // Returns a boolean which is False if the profile cannot be removed
    public bool attemptRemoveUser(ServerProfile removedUser) {
        // If the user either doesnt belong or isnt in the chat currently
        if (!checkBelong(removedUser) || !checkPresence(removedUser)) {return false;}

        active_users.Remove(removedUser);
        return true;
    }

    // Getter for the max size of the chat
    // Takes no value
    // Returns an integer value representing the max size of the chat
    public int getMaxSize() {
        return this.maxSize;
    }

    // Getter for the number of active users in the chat
    // Takes no value
    // Returns an integer value representing the number of active users in the chat
    public int getNumActiveUsers() {
        return active_users.Count();
    }

    // Getter for the active users in the chat
    // Takes no value
    // Returns a list of ServerProfiles representing each of the active users
    public List<ServerProfile> getActiveUsers() {
        return this.active_users;
    }

    // Checks whether a profile belongs in the chatroom
    // Takes a ServerProfile representing the profile to check
    // Returns a boolean value in which True indicates the user does belong
    public bool checkBelong(ServerProfile checkedProfile) { 
        // If it doesnt belong, check if general
        if (!allBelonging.Exists(x => x == checkedProfile)) {
            return is_general;
        }
        else {
            return true;
        }
    }

    // Checks whether a profile is active in the chatroom
    // Takes a ServerProfile representing the profile to check
    // Returns a boolean value in which True indicates the user is active
    public bool checkPresence(ServerProfile checkedProfile) { 
        return active_users.Exists(x => x == checkedProfile);
    }
}
}