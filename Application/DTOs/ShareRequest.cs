using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOs;
public class ShareRequest
{
    public string Email { get; set; } = string.Empty;
    public AccessLevel AccessLevel { get; set; }
}