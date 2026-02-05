using System;

public partial class UserAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int? Id { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string Username { get; set; }

    [Column(TypeName = "varchar(100)")]
    public string Password { get; set; }

    [Column(TypeName = "varchar(150)")]
    public string FullName { get; set; }
    [Column(TypeName = "varchar(150)")]
    public string? EmailAddress { get; set; }

    public int? Role { get; set; }
    public int? UserType { get; set; }

    public int? IsActive { get; set; }

    public int? IsDeleted { get; set; }

    public int? CreatedBy { get; set; }

    public int? DeletedBy { get; set; }

    public int? UpdatedBy { get; set; }

    public int? Status { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime? DateUpdated { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime? DateDeleted { get; set; }
    [Column(TypeName = "datetime")]
    public DateTime? DateCreated { get; set; }


    public int? isLoggedIn { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string? RememberToken { get; set; }

    [Column(TypeName = "varchar(255)")]
    public string? JWToken { get; set; }
}
