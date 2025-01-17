﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UsersManager.Models;

namespace UsersManager.Controllers
{
    public class AccountsController : Controller
    {
        UsersDBEntities DB = new UsersDBEntities();
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DB.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Account creation
        [HttpPost]
        public JsonResult EmailAvailable(string email, int Id = 0)
        {
            return Json(DB.EmailAvailable(email, Id));
        }
        [HttpPost]
        public JsonResult EmailExist(string email)
        {
            return Json(DB.EmailExist(email));
        }
        public ActionResult Subscribe()
        {
            ViewBag.Genders = SelectListItemConverter<Gender>.Convert(DB.Genders.ToList());
            return View(new User());
        }
        [HttpPost]
        public ActionResult Subscribe(User user)
        {
            if (ModelState.IsValid)
            {
                user = DB.Add_User(user);
                SendEmailVerification(user, user.Email);
                return RedirectToAction("SubscribeDone/" + user.Id.ToString());
            }
            ViewBag.Genders = SelectListItemConverter<Gender>.Convert(DB.Genders.ToList());
            return View(user);
        }
        public ActionResult SubscribeDone(int id)
        {
            User newlySubscribedUser = DB.Users.Find(id);
            if (newlySubscribedUser != null)
                return View(newlySubscribedUser);
            return RedirectToAction("Login");
        }
        #endregion

        #region Account Verification
        public void SendEmailVerification(User user, string newEmail)
        {
            if (user.Id != 0)
            {
                UnverifiedEmail unverifiedEmail = DB.Add_UnverifiedEmail(user.Id, newEmail);
                if (unverifiedEmail != null)
                {
                    string verificationUrl = Url.Action("VerifyUser", "Accounts", null, Request.Url.Scheme);
                    String Link = @"<br/><a href='" + verificationUrl + "?userid=" + user.Id + "&code=" + unverifiedEmail.VerificationCode + @"' > Confirmez votre inscription...</a>";

                    String suffixe = "";
                    if (user.GenderId == 2)
                    {
                        suffixe = "e";
                    }
                    string Subject = "UsersManager - Vérification d'inscription...";

                    string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                    Body += @"Merci de vous être inscrit" + suffixe + " au site [nom de l'application]. <br/>";
                    Body += @"Pour utiliser votre compte vous devez confirmer votre inscription en cliquant sur le lien suivant : <br/>";
                    Body += Link;
                    Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                    Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                         + Gmail.SMTP.OwnerEmail + "'>" + Gmail.SMTP.OwnerName + "</a> (Webmestre du site [nom de l'application])";

                    Gmail.SMTP.SendEmail(user.GetFullName(), unverifiedEmail.Email, Subject, Body);
                }
            }
        }
        public ActionResult VerifyDone(int id)
        {
            User newlySubscribedUser = DB.Users.Find(id);
            if (newlySubscribedUser != null)
                return View(newlySubscribedUser);
            return RedirectToAction("Login");
        }
        public ActionResult VerifyError()
        {
            return View();
        }
        public ActionResult AlreadyVerified()
        {
            return View();
        }
        public ActionResult VerifyUser(int userid, int code)
        {
            User newlySubscribedUser = DB.FindUser(userid);
            if (newlySubscribedUser != null)
            {
                if (!DB.HaveUnverifiedEmail(userid, code))
                {
                    if (DB.EmailVerified(newlySubscribedUser.Email))
                        return RedirectToAction("AlreadyVerified");
                }
                if (DB.Verify_User(userid, code))
                    return RedirectToAction("VerifyDone/" + userid);
            }
            return RedirectToAction("VerifyError");
        }
        #endregion

        #region EmailChange
        public ActionResult EmailChangedAlert()
        {
            OnlineUsers.RemoveSessionUser();
            return View();
        }
        public void SendEmailChangedVerification(User user, string newEmail)
        {
            if (user.Id != 0)
            {
                UnverifiedEmail unverifiedEmail = DB.Add_UnverifiedEmail(user.Id, newEmail);
                if (unverifiedEmail != null)
                {
                    string verificationUrl = Url.Action("VerifyUser", "Accounts", null, Request.Url.Scheme);
                    String Link = @"<br/><a href='" + verificationUrl + "?userid=" + user.Id + "&code=" + unverifiedEmail.VerificationCode + @"' > Confirmez votre adresse...</a>";

                    string Subject = "UsersManager - Vérification de courriel...";

                    string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                    Body += @"Vous avez modifié votre adresse de courriel. <br/>";
                    Body += @"Pour que ce changement soit pris en compte, vous devez confirmer cette adresse en cliquant sur le lien suivant : <br/>";
                    Body += Link;
                    Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                    Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                         + Gmail.SMTP.OwnerEmail + "'>" + Gmail.SMTP.OwnerName + "</a> (Webmestre du site [nom de l'application])";

                    Gmail.SMTP.SendEmail(user.GetFullName(), unverifiedEmail.Email, Subject, Body);
                }
            }
        }
        #endregion

        #region ResetPassword
        public ActionResult ResetPasswordCommand()
        {
            return View();
        }
        [HttpPost]
        public ActionResult ResetPasswordCommand(string Email)
        {
            if (ModelState.IsValid)
            {
                SendResetPasswordCommandEmail(Email);
                return RedirectToAction("ResetPasswordCommandAlert");
            }
            return View(Email);
        }
        public void SendResetPasswordCommandEmail(string email)
        {
            ResetPasswordCommand resetPasswordCommand = DB.Add_ResetPasswordCommand(email);
            if (resetPasswordCommand != null)
            {
                User user = DB.Users.Find(resetPasswordCommand.UserId);
                string verificationUrl = Url.Action("ResetPassword", "Accounts", null, Request.Url.Scheme);
                String Link = @"<br/><a href='" + verificationUrl + "?userid=" + user.Id + "&code=" + resetPasswordCommand.VerificationCode + @"' > Reinitialisation de mot de passe...</a>";

                string Subject = "UsersManager - Reinitialisaton ...";

                string Body = "Bonjour " + user.GetFullName(true) + @",<br/><br/>";
                Body += @"Vous avez demandé de reinitialiser votre mot de passe. <br/>";
                Body += @"Procedez en cliquant sur le lien suivant : <br/>";
                Body += Link;
                Body += @"<br/><br/>Ce courriel a été généré automatiquement, veuillez ne pas y répondre.";
                Body += @"<br/><br/>Si vous éprouvez des difficultés ou s'il s'agit d'une erreur, veuillez le signaler à <a href='mailto:"
                     + Gmail.SMTP.OwnerEmail + "'>" + Gmail.SMTP.OwnerName + "</a> (Webmestre du site [nom de l'application])";

                Gmail.SMTP.SendEmail(user.GetFullName(), user.Email, Subject, Body);
            }
        }
        public ActionResult ResetPassword(int userid, int code)
        {
            ResetPasswordCommand resetPasswordCommand = DB.Find_ResetPasswordCommand(userid, code);
            if (resetPasswordCommand != null)
                return View(new PasswordView() { UserId = userid });
            return RedirectToAction("ResetPasswordError");
        }
        [HttpPost]
        public ActionResult ResetPassword(PasswordView passwordView)
        {
            if (ModelState.IsValid)
            {
                if (DB.ResetPassword(passwordView.UserId, passwordView.Password))
                    return RedirectToAction("ResetPasswordSuccess");
                else
                    return RedirectToAction("ResetPasswordError");
            }
            return View(passwordView);
        }
        public ActionResult ResetPasswordCommandAlert()
        {
            return View();
        }
        public ActionResult ResetPasswordSuccess()
        {
            return View();
        }
        public ActionResult ResetPasswordError()
        {
            return View();
        }
        #endregion

        #region Profil
        [UserAccess]
        public ActionResult Profil()
        {
            ViewBag.Genders = SelectListItemConverter<Gender>.Convert(DB.Genders.ToList());
            return View(OnlineUsers.GetSessionUser());
        }

        [HttpPost]
        public ActionResult Profil(User user)
        {
            User currentUser = OnlineUsers.GetSessionUser();
            string newEmail = "";
            if (ModelState.IsValid)
            {
                if (user.Password == "Not Changed")
                {
                    user.Password = currentUser.Password;
                    user.ConfirmPassword = currentUser.Password;
                }

                if (user.Email != currentUser.Email)
                {
                    newEmail = user.Email;
                    user.Email = user.ConfirmEmail = currentUser.Email;
                }

                user = DB.Update_User(user);

                if (newEmail != "")
                {
                    SendEmailChangedVerification(user, newEmail);
                    return RedirectToAction("EmailChangedAlert");
                }
                else
                    return RedirectToAction("Index", "Application");
            }
            ViewBag.Genders = SelectListItemConverter<Gender>.Convert(DB.Genders.ToList());
            return View(currentUser);
        }
        #endregion

        #region Login and Logout
        public ActionResult Login(string message)
        {
            ViewBag.Message = message;
            return View(new LoginCredential());
        }

        [HttpPost]
        public ActionResult Login(LoginCredential loginCredential)
        {
            if (ModelState.IsValid)
            {
                if (DB.EmailBlocked(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce courriel est bloqué.");
                    return View(loginCredential);
                }
                if (!DB.EmailVerified(loginCredential.Email))
                {
                    ModelState.AddModelError("Email", "Ce courriel n'est pas vérifié.");
                    return View(loginCredential);
                }
                User user = DB.GetUser(loginCredential);
                if (user == null)
                {
                    ModelState.AddModelError("Password", "Mot de passe incorrecte.");
                    return View(loginCredential);
                }
                if (OnlineUsers.IsOnLine(user.Id))
                {
                    ModelState.AddModelError("Email", "Cet usager est déjà connecté.");
                    return View(loginCredential);
                }
                OnlineUsers.AddSessionUser(user.Id);
                DB.Add_Login(user.Id);
                return RedirectToAction("Index", "Application");
            }
            return View(loginCredential);
        }

        public ActionResult Logout()
        {
            int? id = OnlineUsers.GetSessionUser().Id;
            if (id != null)
            {
                DB.Update_Login((int)id);
            }
            OnlineUsers.RemoveSessionUser();
            return RedirectToAction("Login");
        }
        #endregion

        #region LoginsHistory
        [AdminAccess]
        public ActionResult Logins(string date)
        {
            if (date != null)
            {
                DateTime dateToDelete = DateTime.Parse(date);
                if(DB.Logins.Select(l => l.LoginDate == dateToDelete) != null)
                {
                    UsersDBDAL.DeleteLoginsJournalDay(DB, dateToDelete);
                }
            }
            return View(DB.Logins);
        }
        #endregion

        #region GroupEmail

        [AdminAccess]
        public ActionResult GroupEmail()
        {
            ViewBag.SelectedUsers = new List<int>();
            ViewBag.Users = DB.SortedUsers();
            return View(new GroupEmail());
        }
        [HttpPost]
        public ActionResult GroupEmail(GroupEmail groupEmail, List<int> SelectedUsers)
        {
            if(ModelState.IsValid)
            {
                groupEmail.SelectedUsers = SelectedUsers;
                groupEmail.Send(DB);
                return RedirectToAction("UserList");
            }
            ViewBag.SelectedUsers = SelectedUsers;
            ViewBag.Users = DB.SortedUsers();
            return View(groupEmail);
        }

        #endregion

        #region Administrator actions
        public JsonResult NeedUpdate()
        {
            return Json(OnlineUsers.NeedUpdate(), JsonRequestBehavior.AllowGet);
        }
        [AdminAccess]
        public ActionResult UserList()
        {
            return View(DB.Users);
        }

        [AdminAccess]
        public ActionResult ChangeUserBlockedStatus(int userid, bool blocked)
        {
            User user = DB.FindUser(userid);
            user.Blocked = blocked;
            DB.Update_User(user);
            return RedirectToAction("UserList");
        }

        [AdminAccess]
        public ActionResult Delete(int id)
        {
            DB.RemoveUser(id);
            return RedirectToAction("UserList");
        }
        #endregion

    }
}