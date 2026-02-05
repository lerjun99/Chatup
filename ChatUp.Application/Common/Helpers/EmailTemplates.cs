using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatUp.Application.Common.Helpers
{
    public static class EmailTemplates
    {
        public static string OtpEmail(string email, string otp)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{
    font-family: Arial, sans-serif;
    background-color: #f4f6f8;
    padding: 20px;
}}
.container {{
    max-width: 480px;
    margin: auto;
    background: #ffffff;
    border-radius: 8px;
    padding: 30px;
}}
.code {{
    font-size: 32px;
    letter-spacing: 6px;
    font-weight: bold;
    color: #4f46e5;
    text-align: center;
    margin: 20px 0;
}}
.footer {{
    font-size: 12px;
    color: #6b7280;
    text-align: center;
}}
</style>
</head>
<body>
<div class='container'>
    <h2>Password Reset Verification</h2>
    <p>Hello,</p>
    <p>Use the code below to reset your password:</p>

    <div class='code'>{otp}</div>

    <p>This code will expire in <b>5 minutes</b>.</p>
    <p>If you did not request this, please ignore this email.</p>

    <div class='footer'>
        © {DateTime.UtcNow.Year} Your Application
    </div>
</div>
</body>
</html>";
        }
    }

}
