﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Traffic_Control_System.Models
{
    public class  TrafficViolationsModel
    {
        [Required]
        public string DeviceID { get; set; }

        [Required(ErrorMessage = "License Plate is required.")]
        [StringLength(10, ErrorMessage = "License Plate cannot exceed 10 characters.")]
        public string? LicensePlate { get; set; }

        [Required(ErrorMessage = "Video URL is required.")]
        public string? Filename { get; set; }
    }
    
    public class UserList
    {
        //[Key]
        public string? Id { get; set; }

        [DataType(DataType.EmailAddress)]
        [Required(ErrorMessage = "Email / Username is required.")]
        public string EmailId { get; set; }

        //[Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class ApproveDenyModel
    {
        public string ID { get; set; }
        public bool textArea { get; set; }
        public string confirmBtnMessage { get; set; }
        public string hintMessage { get; set; }
        public string cancelBtnMessage { get; set; }
        public string reminderText { get; set; }
    }

    public class ICRUDModel<T> where T : class
    {
        public string action { get; set; }

        public string table { get; set; }

        public string keyColumn { get; set; }

        public object key { get; set; }

        public T value { get; set; }

        public List<T> added { get; set; }

        public List<T> changed { get; set; }

        public List<T> deleted { get; set; }

        public IDictionary<string, object> @params { get; set; }
    }

    public class VideStreamViewModel
    {
        public string VideoURL { get; set; } = "~/VideoServiceProxy/";
        public string? Intersection { get; set; }
        public string? Direction1 { get; set; }
        public string? Direction2 { get; set; }
        public int? Direction1GreenTime { get; set; }
        public int? Direction2GreenTime { get; set; }
        public bool IsClientConnected { get; set; }
    }

    public class ReportViewModel
    {
        public DateTime DateCreated { get; set; }
        public string? LicensePlate { get; set; }
        public string VideoURL { get; set; } = "~/VideoServiceProxy/";
    }

    public class PagingQueryModel
    {
        public bool requiresCounts { get; set; } = false;
        public int Take { get; set; } = 10;
        public int Skip { get; set; } = 0;
        public List<Filter>? Where { get; set; }
        public List<SortDescriptor>? Sorted { get; set; }
    }

    public class Filter
    {
        public bool IsComplex { get; set; } = false;
        public string Condition { get; set; } = "and";
        public string? Field { get; set; }
        public string? Operator { get; set; }
        public string? Value { get; set; }
        public bool IgnoreCase { get; set; } = false;
        public List<Filter>? Predicates { get; set; }
    }

    public class SortDescriptor
    {
        public string Name { get; set; }
        public string Direction { get; set; }
    }

    public class PopUpModel
    {
        public string ID { get; set; }
        public bool textArea { get; set; }
        public string confirmBtnMessage { get; set; }
        public string hintMessage { get; set; }
        public string cancelBtnMessage { get; set; }
        public string reminderText { get; set; }
    }

}
