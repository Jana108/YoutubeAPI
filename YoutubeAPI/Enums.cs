using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YoutubeAPI
{
    public enum LogLevel
    {
        Trace = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        FatalError = 4
    }

    public enum StatusCode
    {
        Ok = 200,
        NotFound = 404,
        BadRequest = 400,
        InternalError = 500
    }
}
