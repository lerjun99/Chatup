using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tools
{
    public class ChatMessageDto
    {
        public int SenderId { get; set; }
        public int ReceiverId { get; set; }
        public string Text { get; set; } = "";
    }
    public partial class loginCredentials
    {
        public string? username { get; set; }
        public string? password { get; set; }
        public string? ipaddress { get; set; }
        public string? location { get; set; }
        public bool? rememberToken { get; set; }
    }
    public class StatusReturns
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public string? JwtToken { get; set; }
        public string? UserType { get; set; }
    }
    public class BaseResult
    {
        public string? Code { get; set; }
        public string? Status { get; set; }
        public string? MsgCode { get; set; }
        public string? Msg { get; set; }
        public string? Ref { get; set; }
        public int? Count { get; set; }
    }
    public class ApiListResponse<T>
    {
        public BaseResult? BaseResult { get; set; }
        public T? Data { get; set; }
    }
    public class Param
    {
        public string? Id { get; set; }
    }
    public class ModelId
    {
        public int? Id { get; set; }
        public int page { get; set; }
        public int pageSize { get; set; }
        public string? searchParam { get; set; }
        public bool isArchive { get; set; }
    }
    public class ApiResponse
    {
        public BaseResult? BaseResult { get; set; }
        public string Data { get; set; }
    }

    public class AppSettings
    {
        public string ApiBaseUrl { get; set; }
    }
    public class RegionModel
    {
        public string region_code { get; set; }
        public string region_name { get; set; }
    }
 
    public class ProvinceModel
    {
        public string province_code { get; set; }
        public string province_name { get; set; }
        public string region_code { get; set; }
    }
   
    public class CityModel
    {
        public string city_code { get; set; }
        public string city_name { get; set; }
        public string province_code { get; set; }
    }

    public class BarangayModel
    {
        public string brgy_code { get; set; }
        public string brgy_name { get; set; }
        public string city_code { get; set; }
    }
    public class PaginationModel
    {
        public int? CurrentPage { get; set; }
        public int? NextPage { get; set; }
        public int? PrevPage { get; set; }
        public double? TotalPage { get; set; }
        public int? PageSize { get; set; }
        public int? TotalRecord { get; set; }
    }
}