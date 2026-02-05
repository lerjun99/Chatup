using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Domain.Enums
{
    public enum ApplicationStatus
    {
        InitialAssessment = 1,
        ForInterview = 2,
        ForAssessment = 3,
        Hired = 4,
        Rejected = 5
    }
}
