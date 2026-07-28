using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Common.Exceptions;

public sealed class InvalidCredentialsException : Exception
{
    public InvalidCredentialsException(string message)
        : base(message)
    {
    }
}