using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SupremeShare___Equipment_Borrowing_Tracker_System.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // Check if user is authenticated
            var userRole = context.HttpContext.Session.GetString("UserRole");
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (userId == null || string.IsNullOrEmpty(userRole))
            {
                context.Result = new RedirectToActionResult("Login", "Auth", null);
                return;
            }

            // Check if user has required role
            if (_roles.Length > 0 && !Array.Exists(_roles, r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase)))
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminOnlyAttribute : AuthorizeRoleAttribute
    {
        public AdminOnlyAttribute() : base("admin") { }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SupportAdminAttribute : AuthorizeRoleAttribute
    {
        public SupportAdminAttribute() : base("admin", "support_admin") { }
    }
}
