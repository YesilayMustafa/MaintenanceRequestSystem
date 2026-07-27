using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Common.Exceptions;

public sealed class RequestValidationException : Exception
{
    public RequestValidationException(string message)
        : base(message)
    {
    }
}