﻿using Microsoft.AspNetCore.Mvc;
using MovieAPI.Models.DTOs;
using MovieAPI.Models.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MovieAPI.DatabaseAccessLayer.Interface
{
    public interface IUserDAL
    {
        Task<Status> RegistraterAdmin(RegistrationModel model);
        Task<Status> RegistraterUser(RegistrationModel model);
        Task<LoginResponce> Login(LoginModel model);
        Task<Status> ChangePassword(ChangePasswordModel model);
        
        
    }
}
