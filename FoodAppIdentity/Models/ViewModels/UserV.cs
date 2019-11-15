using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoodAppIdentity.Models.ViewModels
{
    public class UserV
    {
        
        public int Id { get; set; }

        
        public string Fullname { get; set; }

        
        public string Username { get; set; }

        

        
        public string Email { get; set; }


        public string Role { get; set; } 

        public string Status { get; set; } 

        public bool IsDeleted { get; set; }
    }
}
