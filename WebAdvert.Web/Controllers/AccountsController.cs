using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.AspNetCore.Identity.Cognito;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebAdvert.Web.Models.Accounts;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebAdvert.Web.Controllers
{
    public class AccountsController : Controller
    {
        private readonly SignInManager<CognitoUser> _signInManager;
        private readonly UserManager<CognitoUser> _userManager;
        private readonly CognitoUserPool _pool;

        public AccountsController(SignInManager<CognitoUser> signInManager, UserManager<CognitoUser> userManager, CognitoUserPool pool)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _pool = pool;
        }


        // GET: /<controller>/
        public async Task<IActionResult> Register()
        {
            var model = new RegisterModel();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Confirm()
        {
            var model = new ConfirmModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Confirm(ConfirmModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                {
                    ModelState.AddModelError("Not Found", "User with this email not found");
                    return View(model);
                }

                var result = await ((CognitoUserManager<CognitoUser>) _userManager).ConfirmSignUpAsync(user, model.Code, true).ConfigureAwait(false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    foreach (var item in result.Errors)
                    {
                        ModelState.AddModelError(item.Code,item.Description);
                    }

                    return View(model);
                }
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if(ModelState.IsValid)
            {
                var user = _pool.GetUser(model.Email);
                if(user.Status != null)
                {
                    ModelState.AddModelError("User Exists", "User with this mail already exists");
                    return View(model);
                }

                user.Attributes.Add(CognitoAttribute.Name.AttributeName, model.Email);
                user.Attributes.Add(CognitoAttribute.PhoneNumber.AttributeName, model.PhoneNumber);
                var createdUser = await _userManager.CreateAsync(user, model.Password).ConfigureAwait(false);
                if (createdUser.Succeeded)
                {
                   return RedirectToAction("Confirm");
                }
                else
                {
                    foreach (var item in createdUser.Errors)
                    {
                        ModelState.AddModelError(item.Code,item.Description);
                    }

                    return View(model);
                }
            }

            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            var loginModel = new LoginModel();
            return View(loginModel);
        }

        [HttpPost]
        [ActionName("Login")]
        public async Task<IActionResult> LoginPost(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email,
                    model.Password, model.RememberMe, false).ConfigureAwait(false);
                if (result.Succeeded)
                    return RedirectToAction("Index", "Home");
                ModelState.AddModelError("LoginError", "Email and password do not match");
            }

            return View("Login", model);
        }

        public async Task<IActionResult> Signout()
        {
            if (User.Identity.IsAuthenticated) await _signInManager.SignOutAsync().ConfigureAwait(false);
            return RedirectToAction("Login");
        }
    }
}
