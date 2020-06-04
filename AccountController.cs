using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using TopLearn.Core.Convertors;
using TopLearn.Core.DTOs;
using TopLearn.Core.Generator;
using TopLearn.Core.Security;
using TopLearn.Core.Senders;
using TopLearn.Core.Services.Interfaces;
using TopLearn.DataLayer.Entities.User;

namespace TopLearn.Web.Controllers
{
    public class AccountController : Controller
    {
        private IUserService _userService;
        private IViewRenderService _ViewRender;
        public AccountController(IUserService userService , IViewRenderService viewRender)
        {
            _userService = userService;
            _ViewRender = viewRender;
        }

        #region Register

        
        [Route("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("Register")]
        public IActionResult Register(RegisterViewModel register)
        {

            #region چک کردن اعتبار سنجی ها و عدم وجود نام کاربری و ایمیل
            if (!ModelState.IsValid)
            {

                return View(register);
            }

            if (_userService.isExistEmail(FixedText.FixEmail(register.Email)))
            {
                ModelState.AddModelError("Email", "این ایمیل موجود می باشد");
                return View(register);
            }

            if (_userService.isExistUserName(FixedText.FixEmail(register.UserName)))
            {
                ModelState.AddModelError("UserName", "این نام کاربری معتبر نمی باشد");
                return View(register);
            }

            #endregion
            #region ثبت کاربر
            DataLayer.Entities.User.User user = new DataLayer.Entities.User.User()
            {
                ActiveCode = NameGenerator.GenerateUniqCode(),
                Email = FixedText.FixEmail(register.Email),
                IsActive = false,
                Password = PasswordHelper.EncodePasswordMd5(register.Password),
                RegisterDate = DateTime.Now,
                UserAvatar = "Defult.jpg",
                UserName = register.UserName,




            };

            _userService.AddUser(user);


            #endregion

            #region ارسال ایمیل فعال سازی
            string body = _ViewRender.RenderToStringAsync("_ActiveEmail", user);
            SendEmail.Send(user.Email, "فعالسازی", body);

            #endregion

            return View("SuccessRegister", user);
        }
        #endregion

        #region Login
        [Route("Login")]
        public ActionResult Login()
        {


            return View();
        }

        [HttpPost]
        [Route("Login")]
        public ActionResult Login(LoginViewModel login , string ReturnUrl="/")
        {

            if (!ModelState.IsValid)
            {


                return View(login);
            }
            var user = _userService.LoginUser(login);
            if (user !=null)
            {
                var claims = new List<Claim>
                {new Claim(ClaimTypes.NameIdentifier,user.UserId.ToString()),
                new Claim(ClaimTypes.Name,user.UserName)
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var properties = new AuthenticationProperties
                {
                    IsPersistent=login.RememberMe,
                    

                };
                HttpContext.SignInAsync(principal, properties);

                if (user.IsActive)
                {
                    if (ReturnUrl != "/")
                    {
                        return Redirect(ReturnUrl);
                    }

                    ViewBag.IsSuccess = true;
                    return View();
                }
                else
                {
                    ModelState.AddModelError("Email", "حساب کاربری شما فعال نمی باشد");

                }
            }


            ModelState.AddModelError("Email", "کاربری با مشخصات وارد شده یافت نگردید");

            return View(login);

           
        }
        #endregion

        #region Active Account

        public IActionResult ActiveAccount(string id)
        {
            ViewBag.IsActive = _userService.ActiveAccount(id);
            return View();
        }
        #endregion


        #region Logout
   
        [Route("Logout")]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/Login");
        }
        #endregion
        #region Forgot Password
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword()
        {



            return View();
        }
        [HttpPost]
        [Route("ForgotPassword")]
        public IActionResult ForgotPassword(ForgotPasswordViewModel forgot)
        {
            if (!ModelState.IsValid)
                return View(forgot);
            string fixedEmail = FixedText.FixEmail(forgot.Email);
            User user = _userService.GetUserByEmail(fixedEmail);
            if (user == null) { 
            ModelState.AddModelError("Email", "کاربری یافت نشد");
                return View(forgot);
         }

            string bodyEmail = _ViewRender.RenderToStringAsync("_ForgotPassword", user);
            SendEmail.Send(user.Email, "بازیابی حساب کاربری", bodyEmail);
            ViewBag.IsSuccess = true;
            return View();
        }
        #endregion


        #region Reset Password

        public IActionResult ResetPassword(string id)
        {

            return View(new ResetPasswordViewModel()
            {
                ActiveCode = id
        }) ;
        }
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel reste)
        {
            if (!ModelState.IsValid)
            
                return View(reste);

            DataLayer.Entities.User.User user = _userService.GetUserByActiveCode(reste.ActiveCode);
            
                if(user==null)
            

                return NotFound();
            

            string hashNewPassword = PasswordHelper.EncodePasswordMd5(reste.Password);
            user.Password = hashNewPassword;
            _userService.UpdateUser(user);
            return Redirect("/Login");
        }


        #endregion

    }







}