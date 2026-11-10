using System.Collections.Generic;

namespace CentralPress.API.Common;

public static class PermissionModules
{
    public const string Employees = "employees";
    public const string JobTitles = "job_titles";
    public const string Machinery = "machinery";
    public const string Materials = "materials";
    public const string Customers = "customers";
    public const string Moqaysa = "moqaysa";
    public const string Formulas = "formulas";
    public const string Tasweya = "tasweya";
    public const string PaymentOrders = "payment_orders";
    public const string Reports = "reports";

    public static readonly IReadOnlyList<string> All =
    [
        Employees,
        JobTitles,
        Machinery,
        Materials,
        Customers,
        Moqaysa,
        Formulas,
        Tasweya,
        PaymentOrders,
        Reports
    ];
}
