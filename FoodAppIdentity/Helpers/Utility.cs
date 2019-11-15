using FoodAppIdentity.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodAppIdentity.Helpers
{
    public static class Utility
    {
        public static void UserDetailModify(this User user)
        {
            user.Username = user.Username.ToLower();
            if (string.IsNullOrEmpty(user.Role))
            {
                if (user.Role == AppConstants.User)
                {
                    user.Status = AppConstants.NotApplicable;
                }
                else
                {
                    user.Status = AppConstants.NotVerified;
                }
            }
            else
            {
                user.Role = user.Role.ToLower();
            }
            

            
        }
    }
}
