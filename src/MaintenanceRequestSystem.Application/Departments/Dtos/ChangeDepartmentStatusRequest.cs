using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Departments.Dtos;

public sealed class ChangeDepartmentStatusRequest
{
    public bool IsActive { get; set; }
}