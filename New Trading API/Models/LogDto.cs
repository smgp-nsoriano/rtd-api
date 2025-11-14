using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace New_Trading_API.Models
{
    public class LogDto
    {
        public int? Id { get; set; }
        public string Message { get; set; }
        public DateTime LogDate { get; set; }
        public string Downloader {  get; set; }
        public string Url { get; set; }
        public string ErrorMessage {  get; set; }
    }
}