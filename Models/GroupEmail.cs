using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace UsersManager.Models
{
    public class GroupEmail
    {
        public List<int> SelectedUsers { get; set; }
        [Display(Name = "Sujet"), Required(ErrorMessage = "Obligatoire")]
        public string Subject { get; set; }
        [Display(Name = "Message"), Required(ErrorMessage = "Obligatoire")]
        public string Message { get; set; }
        public void Send(UsersDBEntities DB)
        {
            foreach(int selectedUser in SelectedUsers)
            {
                User user = DB.Users.Find(selectedUser);
                string message = Message.Replace("[Nom]", user.GetFullName(true)).Replace("\r\n", @"<br>");
                Gmail.SMTP.SendEmail(user.GetFullName(), user.Email, Subject, message);
            }
        }
    }

}