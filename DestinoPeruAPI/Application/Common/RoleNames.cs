namespace DestinoPeruAPI.Application.Common;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Vendedor = "Vendedor";
    public const string Agencia = "Agencia";
    public const string Cliente = "Cliente";

    public const string AgencyPanel = "Admin,Vendedor,SuperAdmin";
    public const string AgencyStaff = "Admin,Vendedor,SuperAdmin";
    public const string TourCreator = "Admin,SuperAdmin";
    public const string VendorManager = "Admin,SuperAdmin";
}
