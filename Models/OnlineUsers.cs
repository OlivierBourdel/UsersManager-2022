using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace UsersManager.Models
{
    public static class OnlineUsers
    {
        private static List<int> UsersId
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsers"] == null)
                    HttpRuntime.Cache["OnLineUsers"] = new List<int>();
                return (List<int>)HttpRuntime.Cache["OnLineUsers"];
            }
        }
        private static int CurrentUserId
        {
            get
            {
                try
                {
                    if (HttpContext.Current.Session["UserId"] != null)
                        return (int)HttpContext.Current.Session["UserId"];
                    return 0;
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            set
            {
                if (value != 0)
                {
                    HttpContext.Current.Session.Timeout = 60;
                    HttpContext.Current.Session["UserId"] = value;
                }
                else
                {
                    if (HttpContext.Current != null)
                        HttpContext.Current.Session.Abandon();
                }
            }
        }
        private static string SerialNumber
        {
            get
            {
                if (HttpRuntime.Cache["OnLineUsersSerialNumber"] == null)
                    RenewSerialNumber();
                return (string)HttpRuntime.Cache["OnLineUsersSerialNumber"];
            }
            set
            {
                HttpRuntime.Cache["OnLineUsersSerialNumber"] = value;
            }
        }
        public static bool NeedUpdate()
        {
            if (HttpContext.Current.Session["SerialNumber"] == null)
            {
                HttpContext.Current.Session["SerialNumber"] = SerialNumber;
                return true;
            }
            string sessionSerialNumber = (string)HttpContext.Current.Session["SerialNumber"];
            HttpContext.Current.Session["SerialNumber"] = SerialNumber;
            return SerialNumber != sessionSerialNumber;
        }
        public static void RenewSerialNumber()
        {
            SerialNumber = Guid.NewGuid().ToString();
        }
        public static void AddSessionUser(int userId)
        {
            if (userId != 0)
            {
                if (!UsersId.Contains(userId))
                {
                    UsersId.Add(userId);
                    CurrentUserId = userId;
                    RenewSerialNumber();
                }
            }
        }
        public static void RemoveSessionUser()
        {
            if (CurrentUserId != 0)
            {
                UsersId.Remove(CurrentUserId);
                CurrentUserId = 0;
                RenewSerialNumber();
            }
        }
        public static User GetSessionUser()
        {
            if (CurrentUserId != 0)
            {
                UsersDBEntities DB = new UsersDBEntities();
                User currentUser = DB.FindUser(CurrentUserId);
                DB.Dispose();
                return currentUser;
            }
            return null;
        }
        public static bool IsOnLine(int userId)
        {
            return UsersId.Contains(userId);
        }
    }

    public class UserAccess : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            User user = OnlineUsers.GetSessionUser();
            if (OnlineUsers.GetSessionUser() != null && !user.Blocked)
                return true;
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login?message=Acces illegal!");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
    public class AdminAccess : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            User sessionUser = OnlineUsers.GetSessionUser();
            if (sessionUser != null && sessionUser.IsAdmin)
                return true;
            else
            {
                OnlineUsers.RemoveSessionUser();
                httpContext.Response.Redirect("~/Accounts/Login?message=Acces illegal!");
            }
            return base.AuthorizeCore(httpContext);
        }
    }
}