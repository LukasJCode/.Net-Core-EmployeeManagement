﻿using EmployeeManagement.Models;
using EmployeeManagement.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace EmployeeManagement.Controllers
{
	[Authorize]
	public class AccountController : Controller
	{
		private readonly UserManager<ApplicationUser> userManager;
		private readonly SignInManager<ApplicationUser> signInManager;
		private readonly ILogger<AccountController> logger;

		public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager,
			ILogger<AccountController> logger)
		{
			this.userManager = userManager;
			this.signInManager = signInManager;
			this.logger = logger;
		}

		[HttpPost]
		public async Task<IActionResult> Logout()
		{
			await signInManager.SignOutAsync();
			return RedirectToAction("index", "home");
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult Register()
		{
			return View();
		}

		[AcceptVerbs("Get", "Post")]
		[AllowAnonymous]
		public async Task<IActionResult> IsEmailInUse(string email)
		{
			var user = await userManager.FindByEmailAsync(email);
			if (user == null)
			{
				return Json(true);
			}
			else
				return Json($"Email {email} is already in use");
		}


		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Register(RegisterViewModel model)
			{
				if(ModelState.IsValid)
				{
					var user = new ApplicationUser { UserName = model.Email, Email = model.Email, City = model.City };

					var result = await userManager.CreateAsync(user, model.Password);

					if(result.Succeeded)
					{
						var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

						var confirmationLink = Url.Action("ConfirmEmail", "Account",
							new { userId = user.Id, token = token }, Request.Scheme);

						logger.Log(LogLevel.Warning, confirmationLink);

						if(signInManager.IsSignedIn(User) && User.IsInRole("Admin"))
						{
							return RedirectToAction("ListUsers", "Administration");
						}

						ViewBag.ErrorTitle = "Registration successful";
						ViewBag.ErrorMessage = "Before you can Login, please confirm your " +
								"email, by clicking on the confirmation link we have emailed you";

						return View("Error");
				}

					foreach(var error in result.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
				}

				return View(model);
		}

		[AllowAnonymous]
		public async Task<IActionResult> ConfirmEmail(string userId, string token)
		{
			if (userId == null || token == null)
			{
				return RedirectToAction("index", "home");
			}

			var user = await userManager.FindByIdAsync(userId);
			if (user == null)
			{
				ViewBag.ErrorMessage = $"The User ID {userId} is invalid";
				return View("NotFound");
			}

			var result = await userManager.ConfirmEmailAsync(user, token);
			if (result.Succeeded)
			{
				return View();
			}

			ViewBag.ErrorTitle = "Email cannot be confirmed";
			return View("Error");
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ForgotPassword()
		{
			return View();
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
		{
			if (ModelState.IsValid)
			{
				// Find the user by email
				var user = await userManager.FindByEmailAsync(model.Email);
				// If the user is found AND Email is confirmed
				if (user != null && await userManager.IsEmailConfirmedAsync(user))
				{
					// Generate the reset password token
					var token = await userManager.GeneratePasswordResetTokenAsync(user);

					// Build the password reset link
					var passwordResetLink = Url.Action("ResetPassword", "Account",
							new { email = model.Email, token = token }, Request.Scheme);

					// Log the password reset link
					logger.Log(LogLevel.Warning, passwordResetLink);

					// Send the user to Forgot Password Confirmation view
					return View("ForgotPasswordConfirmation");
				}

				// To avoid account enumeration and brute force attacks, don't
				// reveal that the user does not exist or is not confirmed
				return View("ForgotPasswordConfirmation");
			}

			return View(model);
		}

		[HttpGet]
		[AllowAnonymous]
		public IActionResult ResetPassword(string token, string email)
		{
			// If password reset token or email is null, most likely the
			// user tried to tamper the password reset link
			if (token == null || email == null)
			{
				ModelState.AddModelError("", "Invalid password reset token");
			}
			return View();
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
		{
			if (ModelState.IsValid)
			{
				// Find the user by email
				var user = await userManager.FindByEmailAsync(model.Email);

				if (user != null)
				{
					// reset the user password
					var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
					if (result.Succeeded)
					{
						return View("ResetPasswordConfirmation");
					}
					// Display validation errors. For example, password reset token already
					// used to change the password or password complexity rules not met
					foreach (var error in result.Errors)
					{
						ModelState.AddModelError("", error.Description);
					}
					return View(model);
				}

				// To avoid account enumeration and brute force attacks, don't
				// reveal that the user does not exist
				return View("ResetPasswordConfirmation");
			}
			// Display validation errors if model state is not valid
			return View(model);
		}

		[HttpGet]
		[AllowAnonymous]
		public async Task<IActionResult> Login(string returnUrl)
		{
			LoginViewModel model = new LoginViewModel
			{
				ReturnUrl = returnUrl,
				ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()

			};
			return View(model);
		}

		[HttpPost]
		[AllowAnonymous]
		public async Task<IActionResult> Login(LoginViewModel model, string returnUrl)
		{

			model.ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

			if (ModelState.IsValid)
			{
				var user = await userManager.FindByEmailAsync(model.Email);

				if (user != null && !user.EmailConfirmed &&
							(await userManager.CheckPasswordAsync(user, model.Password)))
				{
					ModelState.AddModelError(string.Empty, "Email not confirmed yet");
					return View(model);
				}


				var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, false);

				if (result.Succeeded)
				{
					if(!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
					{
						return LocalRedirect(returnUrl);
					}
					return RedirectToAction("index", "home");
				}
				else
					ModelState.AddModelError("", "Invalid Login Attempt");
			}

			return View(model);
		}

		[AllowAnonymous]
		[HttpPost]
		public IActionResult ExternalLogin(string provider, string returnUrl)
		{
			var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { ReturnUrl = returnUrl });

			var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
			return new ChallengeResult(provider, properties);
		}

		[AllowAnonymous]
		public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
		{
			returnUrl = returnUrl ?? Url.Content("~/");

			LoginViewModel loginViewModel = new LoginViewModel
			{
				ReturnUrl = returnUrl,
				ExternalLogins =
						(await signInManager.GetExternalAuthenticationSchemesAsync()).ToList()
			};

			if (remoteError != null)
			{
				ModelState
					.AddModelError(string.Empty, $"Error from external provider: {remoteError}");

				return View("Login", loginViewModel);
			}

			// Get the login information about the user from the external login provider
			var info = await signInManager.GetExternalLoginInfoAsync();
			if (info == null)
			{
				ModelState
					.AddModelError(string.Empty, "Error loading external login information.");

				return View("Login", loginViewModel);
			}

			// Get the email claim value
			var email = info.Principal.FindFirstValue(ClaimTypes.Email);
			ApplicationUser user = null;

			if (email != null)
			{
				// Find the user
				user = await userManager.FindByEmailAsync(email);

				// If email is not confirmed, display login view with validation error
				if (user != null && !user.EmailConfirmed)
				{
					ModelState.AddModelError(string.Empty, "Email not confirmed yet");
					return View("Login", loginViewModel);
				}
			}

			// If the user already has a login (i.e if there is a record in AspNetUserLogins
			// table) then sign-in the user with this external login provider
			var signInResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider,
				info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

			if (signInResult.Succeeded)
			{
				return LocalRedirect(returnUrl);
			}
			// If there is no record in AspNetUserLogins table, the user may not have
			// a local account
			else
			{
				if (email != null)
				{
					if (user == null)
					{
						user = new ApplicationUser
						{
							UserName = info.Principal.FindFirstValue(ClaimTypes.Email),
							Email = info.Principal.FindFirstValue(ClaimTypes.Email)
						};

						await userManager.CreateAsync(user);
						// After a local user account is created, generate and log the
						// email confirmation link
						var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

						var confirmationLink = Url.Action("ConfirmEmail", "Account",
										new { userId = user.Id, token = token }, Request.Scheme);

						logger.Log(LogLevel.Warning, confirmationLink);

						ViewBag.ErrorTitle = "Registration successful";
						ViewBag.ErrorMessage = "Before you can Login, please confirm your " +
							"email, by clicking on the confirmation link we have emailed you";

						return View("Error");
					}

					// Add a login (i.e insert a row for the user in AspNetUserLogins table)
					await userManager.AddLoginAsync(user, info);
					await signInManager.SignInAsync(user, isPersistent: false);

					return LocalRedirect(returnUrl);
				}

				// If we cannot find the user email we cannot continue
				ViewBag.ErrorTitle = $"Email claim not received from: {info.LoginProvider}";
				ViewBag.ErrorMessage = "Please contact support on Pragim@PragimTech.com";

				return View("Error");
			}
		}
	}
}
