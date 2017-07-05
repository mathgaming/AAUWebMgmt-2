﻿using System;
using System.Collections.Specialized;
using System.DirectoryServices;


namespace ITSWebMgmt.Connectors.Active_Directory
{
    public class ADHelpers
    {

       //XXX these functions can add/remove any user from any group. Dont let i be a user parameter. 
       //Wrap these to know groups. 

        private static void addADuserToGroupUNSAFE(string userADpath, string groupADPath) 
        {
            
                DirectoryEntry dirEntry = new DirectoryEntry("LDAP://" + groupADPath);
                dirEntry.Properties["member"].Add(userADpath);
                dirEntry.CommitChanges();
                dirEntry.Close();
            
        }

        private static void removeADuserFromGroupUNSAFE(string userADpath, string groupADPath)
        {
            
                DirectoryEntry dirEntry = new DirectoryEntry("LDAP://" + groupADPath);
                dirEntry.Properties["member"].Remove(userADpath);
                dirEntry.CommitChanges();
                dirEntry.Close();
                
        }

        static string[] safegroups = {
             
            "CN=kyrketestgroup,OU=Manual,OU=Groups,DC=its,DC=aau,DC=dk",
        };
        

        private static bool isGroupSafe(String groupADPath)
        {

            foreach (string s in safegroups)
            {
                if (s.Equals(groupADPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }
            return false;

        }

        public static void addADUserToGroup(string userADpath, string groupADPath)
        {
            if (!isGroupSafe(groupADPath))
            {
                throw new Exception("Not a safe group");
            }

            addADuserToGroupUNSAFE(userADpath, groupADPath);

        }

        public static void remoteADUserFromGroup(string userADpath, string groupADPath)
        {
            if (!isGroupSafe(groupADPath))
            {
                throw new Exception("Not a safe group");
            }
            removeADuserFromGroupUNSAFE(userADpath, groupADPath);
        }

    }
}