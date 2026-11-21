# Graph Report - CentralPress.API  (2026-11-21)

## Corpus Check
- 225 files · ~75,359 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 5 file(s) not represented in the graph (top: .doc 3, (none) 1, .config 1)

## Summary
- 2185 nodes · 5064 edges · 123 communities (102 shown, 21 thin omitted)
- Extraction: 94% EXTRACTED · 6% INFERRED · 0% AMBIGUOUS · INFERRED: 312 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `3df797e3`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- PermissionAuthorize
- WordExportService
- MoqaysaDetailViewModel
- MachineryViewModel
- Moqaysa
- AuditBacklogViewModel
- DateTime
- CancellationToken
- AccountViewModel
- MoqaysaCalculationResult
- List
- CentralPress.API.ViewModels
- MoqaysaDetailedItemViewModel
- MoqaysaSummaryViewModel
- CentralPress.API.DTOs
- MoqaysaCalculationResultViewModel
- Task
- PaperCuttingCalculationResultViewModel
- TasweyaUnsettledMoqaysaViewModel
- MaterialViewModel
- PaymentOrderDetailViewModel
- TasweyaDetailViewModel
- AuthController.cs
- PaymentOrder
- CentralPress.API.Data
- FormulaConfigViewModel
- Tasweya
- system_componentmodel_dataannotations
- Program.cs
- MoqaysaSaveDto
- UnpaidTasweyaViewModel
- AdminUserService
- ApplicationDbContext
- RolePermission
- TokenService
- ApplicationUser
- CustomerSummaryViewModel
- MoqaysaCalculationInput
- PaymentOrderSummaryViewModel
- TasweyaSummaryViewModel
- AuthController
- TasweyaCreateResultViewModel
- UserProfileViewModel
- MoqaysaCalculationRequestDto
- PaymentOrderService
- PermissionAuthorizeAttribute
- ApplicationDbContext.OnModelCreating
- TasweyaTokens.Build
- TasweyaService
- MoqaysaService
- PdfExportService.cs
- AccountSummaryViewModel
- AuditBacklog
- Customer
- RefreshToken
- EmailService
- PaymentOrderOperationResultViewModel
- CustomerViewModel
- LibreOfficeWarmService
- AuthService
- CentralPress.API.Documents
- MoqaysaMachineryItem
- TasweyaCalculatedTotals
- ModulePermissionViewModel
- CentralPress.API
- EfPrintDataSource.LoadTasweyaAsync
- LoginResponseViewModel
- MoqaysaAccountShareViewModel
- MoqaysaMachineryItemViewModel
- PaymentOrderRegisterItemViewModel
- DocumentsController.PaymentOrder
- DocumentService
- CreateMultiAccountTasweyaDto
- MoqaysaAccountShare
- TasweyaMoqaysaItemViewModel
- EmployeeDetailViewModel
- EmployeeSummaryViewModel
- PaymentOrderTasweyaLinkViewModel
- AccountEntity
- UserPermissionOverride
- http
- BudgetValidationResultViewModel
- PdfExportService
- CreatePaymentOrderDto
- CreateTasweyaDto
- UpdatePaymentOrderDto
- PaymentOrderService.UpdateStatusAsync
- MoqaysaMaterialItemViewModel
- UserPermissionOverrideViewModel
- AuthController.ResetPassword
- CreateEmployeeDto
- MachineryItemInputDto
- UpdateTasweyaDto
- PaymentOrderTasweyaLink
- SystemModule
- PaymentOrderStatus
- ITafqeetService.ToWords
- TafqeetService
- CustomerTasweyaSummaryViewModel
- ModuleEffectivePermissionViewModel
- AuditBacklogActionFilter.OnActionExecutionAsync
- AuthController.ForgotPassword
- AuthController.VerifyResetCode
- PdfConverter
- PermissionOverrideInputDto
- RolePermissionInputDto
- UpdateEmployeeDto
- JobTitleSummaryViewModel
- PaymentOrderService.CancelAsync
- MaterialItemInputDto
- UpdateFormulaConfigDto
- TasweyaMoqaysaLink
- ArabicTlvQrHelper.GenerateTlvBase64
- TasweyaAccountShareViewModel
- OperationStatusViewModel
- MoqaysatDetailedFilterDto
- PaginationRequestDto
- README.md

## God Nodes (most connected - your core abstractions)
1. `CentralPress.API.ViewModels` - 86 edges
2. `CentralPress.API.DTOs` - 79 edges
3. `MoqaysaDetailViewModel` - 66 edges
4. `Moqaysa` - 65 edges
5. `ApplicationDbContext` - 62 edges
6. `CentralPress.API.Services` - 58 edges
7. `Tasweya` - 47 edges
8. `MoqaysaSummaryViewModel` - 44 edges
9. `PaymentOrder` - 43 edges
10. `MoqaysaCalculationResult` - 43 edges

## Surprising Connections (you probably didn't know these)
- `AdminUsersController` --references--> `IAdminUserService`  [EXTRACTED]
  Controllers/AdminUsersController.cs → Services/IAdminUserService.cs
- `AdminUsersController` --references--> `IEmailService`  [EXTRACTED]
  Controllers/AdminUsersController.cs → Services/IEmailService.cs
- `AuthController` --references--> `IAuthService`  [EXTRACTED]
  Controllers/AuthController.cs → Services/IAuthService.cs
- `CustomersController` --references--> `ICustomerService`  [EXTRACTED]
  Controllers/CustomersController.cs → Services/ICustomerService.cs
- `DocumentsController` --references--> `DocumentService`  [EXTRACTED]
  Controllers/DocumentsController.cs → Documents/DocumentService.cs

## Import Cycles
- None detected.

## Communities (123 total, 21 thin omitted)

### Community 0 - "PermissionAuthorize"
Cohesion: 0.09
Nodes (8): AdminUsersController, CustomersController, JobTitlesController, MaterialsController, MoqaysaController, PaymentOrdersController, SuperAdminController, TasweyaController

### Community 1 - "WordExportService"
Cohesion: 0.09
Nodes (3): DocxTemplateFiller, TasweyaLineEntry, WordExportService

### Community 2 - "MoqaysaDetailViewModel"
Cohesion: 0.04
Nodes (52): MoqaysaDetailViewModel, AccountCode, AccountShares, AdminExpenses, AdminExpensesPercent, ApplyAdminExpenses, ApplyMarkup, ApplyOther (+44 more)

### Community 3 - "MachineryViewModel"
Cohesion: 0.05
Nodes (38): MachineryDto, DepreciationHourlyRate, DisplayOrder, IsActive, MachineOperatingHourlyFee, MachineryId, MachineryNameAr, MaintenanceHourlyRate (+30 more)

### Community 4 - "Moqaysa"
Cohesion: 0.04
Nodes (47): Moqaysa, AccountShares, AdminExpenses, ApplyAdminExpenses, ApplyMarkup, ApplyOther, ApplyVat, AreMachineryHoursLinked (+39 more)

### Community 5 - "AuditBacklogViewModel"
Cohesion: 0.05
Nodes (35): AuditBacklogQueryDto, ModuleName, Page, PageSize, Search, SuperAdminLoginDto, Code, SecretCode (+27 more)

### Community 6 - "DateTime"
Cohesion: 0.10
Nodes (10): InitialCreate, M1_DatabaseConstraintsAndRefreshTokens, M2_MachineryMaintenanceAndCostingOverhead, M3_PaymentOrdersBudgetAndTasweyaConstraints, M4_MoqaysatJobOrderNumberNonUnique, AddSessionAndMoqaysaUpdates, M5_TasweyaSettlementNumberNonUnique, M6_MoqaysaAccountShares (+2 more)

### Community 7 - "CancellationToken"
Cohesion: 0.11
Nodes (7): MachineriesController, JobTitleDto, Description, TitleNameAr, IJobTitleService, IMachineryService, JobTitleService

### Community 8 - "AccountViewModel"
Cohesion: 0.09
Nodes (16): AccountSaveDto, AccountCode, AccountId, AccountNameAr, BudgetCeiling, DebitHeaderTextAr, IsActive, CustomerService (+8 more)

### Community 9 - "MoqaysaCalculationResult"
Cohesion: 0.05
Nodes (41): MoqaysaCalculationResult, AdminExpenses, AdminExpensesPercent, ApplyAdminExpenses, ApplyMarkup, ApplyOther, ApplyVat, FilmsProcessingFee (+33 more)

### Community 10 - "List"
Cohesion: 0.09
Nodes (10): FormulaConfigsController, ReportsController, CustomerSummaryFilterDto, AccountId, CustomerId, PaymentOrdersRegisterFilterDto, AccountId, IsConfirmed (+2 more)

### Community 12 - "MoqaysaDetailedItemViewModel"
Cohesion: 0.05
Nodes (38): MoqaysaDetailedItemViewModel, AccountNameAr, AdminExpenses, CreatedAtUtc, CustomerNameAr, FinalGrandTotal, IsIncludedInTasweya, IsPaid (+30 more)

### Community 13 - "MoqaysaSummaryViewModel"
Cohesion: 0.05
Nodes (38): MoqaysaSummaryViewModel, AccountCode, AccountShares, AdminExpenses, ApplyOther, ApplyVat, CreatedAtUtc, CustomerId (+30 more)

### Community 14 - "CentralPress.API.DTOs"
Cohesion: 0.18
Nodes (3): CentralPress.API.DTOs, CentralPress.API.Services, ModuleEffectivePermissionDto

### Community 15 - "MoqaysaCalculationResultViewModel"
Cohesion: 0.06
Nodes (35): MoqaysaCalculationResultViewModel, AdminExpenses, AdminExpensesPercent, ApplyAdminExpenses, ApplyMarkup, ApplyOther, ApplyVat, FilmsProcessingFee (+27 more)

### Community 16 - "Task"
Cohesion: 0.10
Nodes (3): IMaterialService, IMoqaysaService, ITasweyaService

### Community 17 - "PaperCuttingCalculationResultViewModel"
Cohesion: 0.06
Nodes (30): PaperCuttingCalculationRequestDto, CutItemHeightMm, CutItemWidthMm, ParentSheetHeightMm, ParentSheetWidthMm, RequiredItemCopies, RunSpoilagePercent, SetupSpoilageSheets (+22 more)

### Community 18 - "TasweyaUnsettledMoqaysaViewModel"
Cohesion: 0.06
Nodes (33): TasweyaUnsettledMoqaysaViewModel, AccountId, AccountNameAr, AccountShares, AdminExpenses, ApplyMarkup, ApplyOther, ApplyVat (+25 more)

### Community 19 - "MaterialViewModel"
Cohesion: 0.07
Nodes (25): CustomerDto, AccountId, Address, BudgetCeiling, CustomerId, CustomerNameAr, Email, IsActive (+17 more)

### Community 20 - "PaymentOrderDetailViewModel"
Cohesion: 0.06
Nodes (32): PaymentOrderDetailViewModel, Account, AccountId, CancellationReason, CancelledAtUtc, CancelledByUserId, ConfirmedAtUtc, ConfirmedByUserId (+24 more)

### Community 21 - "TasweyaDetailViewModel"
Cohesion: 0.06
Nodes (31): TasweyaDetailViewModel, Account, AccountId, AccountShares, ActivePaymentOrderId, ActivePaymentOrderNumber, AdminExpensesSum, ClaimNumber (+23 more)

### Community 22 - "AuthController.cs"
Cohesion: 0.18
Nodes (3): CentralPress.API.Filters, CentralPress.API.Controllers, CentralPress.API.Middleware

### Community 23 - "PaymentOrder"
Cohesion: 0.07
Nodes (30): PaymentOrder, Account, AccountId, CancellationReason, CancelledAtUtc, CancelledByUserId, ConfirmedAtUtc, ConfirmedByUserId (+22 more)

### Community 25 - "FormulaConfigViewModel"
Cohesion: 0.08
Nodes (24): CostingFormulaConfig, AllowedOptionsCsv, DefaultPercentage, FieldKey, FieldLabelAr, FormulaConfigId, FormulaExpressionDescriptionAr, IsActive (+16 more)

### Community 26 - "Tasweya"
Cohesion: 0.07
Nodes (29): Tasweya, Account, AccountId, AdminExpensesSum, ClaimNumber, CreatedAtUtc, CreatedByUserId, Customer (+21 more)

### Community 27 - "system_componentmodel_dataannotations"
Cohesion: 0.16
Nodes (3): CentralPress.API.Entities, TestSmtpRequestDto, RecipientEmail

### Community 29 - "MoqaysaSaveDto"
Cohesion: 0.08
Nodes (24): MoqaysaAccountShareDto, AccountId, SharePercentage, MoqaysaSaveDto, AccountShares, ApplyAdminExpenses, ApplyMarkup, ApplyOther (+16 more)

### Community 30 - "UnpaidTasweyaViewModel"
Cohesion: 0.08
Nodes (25): UnpaidTasweyaViewModel, AccountId, AccountNameAr, AdminExpensesSum, ClaimNumber, CreatedAtUtc, CustomerId, CustomerNameAr (+17 more)

### Community 31 - "AdminUserService"
Cohesion: 0.12
Nodes (4): SaveOverridesDto, Overrides, AdminUserService, IAdminUserService

### Community 32 - "ApplicationDbContext"
Cohesion: 0.09
Nodes (20): ApplicationDbContext, Accounts, AuditBacklogs, CostingFormulaConfigs, Customers, JobTitles, Machineries, Materials (+12 more)

### Community 33 - "RolePermission"
Cohesion: 0.10
Nodes (18): DbInitializer, JobTitle, Description, IsActive, JobTitleId, RolePermissions, TitleNameAr, Users (+10 more)

### Community 34 - "TokenService"
Cohesion: 0.15
Nodes (6): ITokenService, TokenService, AuthTokenViewModel, AccessToken, ExpiresAtUtc, RefreshToken

### Community 35 - "ApplicationUser"
Cohesion: 0.10
Nodes (18): ApplicationUser, CreatedAtUtc, CurrentSessionId, EmployeeCode, FullName, IsActive, JobTitle, JobTitleId (+10 more)

### Community 36 - "CustomerSummaryViewModel"
Cohesion: 0.10
Nodes (20): CustomerMoqaysaSummaryViewModel, CreatedAtUtc, FinalGrandTotal, IsIncludedInTasweya, IsPaid, JobOrderNumber, MoqaysaId, CustomerSummaryViewModel (+12 more)

### Community 37 - "MoqaysaCalculationInput"
Cohesion: 0.11
Nodes (18): MoqaysaCalculationInput, AdminExpensesPercent, ApplyAdminExpenses, ApplyMarkup, ApplyOther, ApplyVat, FilmsProcessingFee, MaterialProcessingPercent (+10 more)

### Community 38 - "PaymentOrderSummaryViewModel"
Cohesion: 0.11
Nodes (19): PaymentOrderSummaryViewModel, AccountId, AccountNameAr, ConfirmedAtUtc, ConfirmedByUserId, CreatedAtUtc, CustomerId, CustomerNameAr (+11 more)

### Community 39 - "TasweyaSummaryViewModel"
Cohesion: 0.11
Nodes (19): TasweyaSummaryViewModel, AccountId, AccountNameAr, ActivePaymentOrderId, ActivePaymentOrderNumber, ClaimNumber, CreatedAtUtc, CustomerId (+11 more)

### Community 40 - "AuthController"
Cohesion: 0.18
Nodes (5): AuthController, RefreshTokenRequestDto, RefreshToken, RevokeTokenRequestDto, RefreshToken

### Community 41 - "TasweyaCreateResultViewModel"
Cohesion: 0.11
Nodes (17): BatchTasweyaCreateResultViewModel, CreatedCount, Items, Message, TasweyaCreateResultViewModel, AccountId, AccountNameAr, Message (+9 more)

### Community 42 - "UserProfileViewModel"
Cohesion: 0.14
Nodes (12): UpdateProfileDto, FullName, UserProfileViewModel, Email, EmployeeCode, FullName, Id, IsSuperAdmin (+4 more)

### Community 43 - "MoqaysaCalculationRequestDto"
Cohesion: 0.12
Nodes (17): MoqaysaCalculationRequestDto, ApplyAdminExpenses, ApplyMarkup, ApplyOther, ApplyVat, FilmsProcessingFee, MachineryItems, MaterialItems (+9 more)

### Community 44 - "PaymentOrderService"
Cohesion: 0.28
Nodes (3): AuditLogService, IAuditLogService, PaymentOrderService

### Community 45 - "PermissionAuthorizeAttribute"
Cohesion: 0.13
Nodes (7): PermissionActions, PermissionConstants, PermissionModules, CentralPress.API.Common, PermissionAuthorizeAttribute, ActionType, ModuleKey

### Community 46 - "ApplicationDbContext.OnModelCreating"
Cohesion: 0.13
Nodes (15): Material, IsActive, MaterialId, MaterialNameAr, UnitNameAr, MoqaysaMaterialItem, ItemId, Material (+7 more)

### Community 47 - "TasweyaTokens.Build"
Cohesion: 0.15
Nodes (5): IAmountInWords, MoqaysaTokens, PrintDataException, TafqeetAmountInWords, TasweyaTokens

### Community 48 - "TasweyaService"
Cohesion: 0.24
Nodes (3): FinancialYearHelper, FinancialYearInfo, TasweyaService

### Community 51 - "AccountSummaryViewModel"
Cohesion: 0.13
Nodes (14): AccountSummaryViewModel, AccountCode, AccountId, AccountNameAr, DebitHeaderTextAr, MoqaysatCount, PaidTasweyatCount, TasweyatCount (+6 more)

### Community 52 - "AuditBacklog"
Cohesion: 0.14
Nodes (14): AuditBacklog, ActionType, AuditId, EmployeeName, EntityName, IpAddress, JobTitleName, ModuleName (+6 more)

### Community 53 - "Customer"
Cohesion: 0.14
Nodes (14): Customer, Account, AccountId, Address, BudgetCeiling, CustomerId, CustomerNameAr, Email (+6 more)

### Community 54 - "RefreshToken"
Cohesion: 0.14
Nodes (14): RefreshToken, CreatedAtUtc, CreatedByIp, ExpiresAtUtc, Id, IsActive, IsExpired, IsRevoked (+6 more)

### Community 55 - "EmailService"
Cohesion: 0.21
Nodes (3): UserSessionValidationMiddleware, EmailService, IEmailService

### Community 56 - "PaymentOrderOperationResultViewModel"
Cohesion: 0.18
Nodes (8): IPaymentOrderService, PaymentOrderOperationResultViewModel, Message, PaymentOrderId, RemainingBudget, Status, Success, TotalPaidAmount

### Community 57 - "CustomerViewModel"
Cohesion: 0.14
Nodes (13): CustomerViewModel, AccountCode, AccountId, AccountNameAr, Address, BudgetCeiling, CustomerId, CustomerNameAr (+5 more)

### Community 58 - "LibreOfficeWarmService"
Cohesion: 0.23
Nodes (6): DocumentOptions, PdfTimeoutSeconds, ProfileFolder, SofficePath, TemplatesFolder, LibreOfficeWarmService

### Community 59 - "AuthService"
Cohesion: 0.22
Nodes (5): ChangePasswordDto, CurrentPassword, NewPassword, AuthService, IAuthService

### Community 61 - "MoqaysaMachineryItem"
Cohesion: 0.15
Nodes (13): MoqaysaMachineryItem, Hours, MachineHourlyRateSnapshot, MachineHours, Machinery, MachineryId, MachineryNameSnapshot, Moqaysa (+5 more)

### Community 62 - "TasweyaCalculatedTotals"
Cohesion: 0.15
Nodes (12): TasweyaCalculatedTotals, AdminExpensesSum, MarkupAmountSum, MinistryOfFinanceSum, OtherAmountSum, RevenuesTotal, SettlementGrandTotal, TotalMachineCostsSum (+4 more)

### Community 63 - "ModulePermissionViewModel"
Cohesion: 0.15
Nodes (12): ModulePermissionViewModel, CanAccess, CanCreate, CanToggleActive, CanUpdate, ModuleId, ModuleKey, ModuleNameAr (+4 more)

### Community 64 - "CentralPress.API"
Cohesion: 0.17
Nodes (11): CentralPress.API, net9.0, DocumentFormat.OpenXml (3.1.1), Microsoft.AspNetCore.Authentication.JwtBearer (9.0.2), Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.2), Microsoft.EntityFrameworkCore.Design (9.0.2), Microsoft.EntityFrameworkCore.SqlServer (9.0.2), Microsoft.EntityFrameworkCore.Tools (9.0.2) (+3 more)

### Community 65 - "EfPrintDataSource.LoadTasweyaAsync"
Cohesion: 0.36
Nodes (7): AccountLine, EfPrintDataSource, IPrintDataSource, MachineryLine, MaterialLine, MoqaysaPrintData, TasweyaPrintData

### Community 66 - "LoginResponseViewModel"
Cohesion: 0.20
Nodes (8): LoginDto, Identifier, Password, LoginResponseViewModel, Permissions, RefreshToken, Token, User

### Community 67 - "MoqaysaAccountShareViewModel"
Cohesion: 0.17
Nodes (12): MoqaysaAccountShareViewModel, AccountId, AccountNameAr, AllocatedAmount, DebitHeaderTextAr, IsIncludedInTasweya, IsPaid, PaidAtUtc (+4 more)

### Community 68 - "MoqaysaMachineryItemViewModel"
Cohesion: 0.17
Nodes (11): MoqaysaMachineryItemViewModel, Hours, ItemId, MachineHourlyRateSnapshot, MachineHours, MachineryId, MachineryNameSnapshot, MoqaysaId (+3 more)

### Community 69 - "PaymentOrderRegisterItemViewModel"
Cohesion: 0.17
Nodes (12): PaymentOrderRegisterItemViewModel, AccountNameAr, ConfirmedAtUtc, CustomerNameAr, IsConfirmed, Notes, PaymentDate, PaymentOrderId (+4 more)

### Community 71 - "DocumentService"
Cohesion: 0.33
Nodes (5): DocFormat, Docx, Pdf, DocumentService, RenderedDocument

### Community 72 - "CreateMultiAccountTasweyaDto"
Cohesion: 0.18
Nodes (11): AccountSettlementEntryDto, AccountId, ClaimNumber, MoqaysaIds, SettlementNumber, CreateMultiAccountTasweyaDto, Accounts, CustomerId (+3 more)

### Community 73 - "MoqaysaAccountShare"
Cohesion: 0.18
Nodes (11): MoqaysaAccountShare, Account, AccountId, AllocatedAmount, IsIncludedInTasweya, IsPaid, Moqaysa, MoqaysaId (+3 more)

### Community 74 - "TasweyaMoqaysaItemViewModel"
Cohesion: 0.18
Nodes (9): TasweyaMoqaysaItemViewModel, AllocatedAmount, ApplyVat, FinalGrandTotal, JobOrderNumber, LinkId, MoqaysaId, SharePercentage (+1 more)

### Community 75 - "EmployeeDetailViewModel"
Cohesion: 0.18
Nodes (11): EmployeeDetailViewModel, CreatedAtUtc, Email, EmployeeCode, FullName, Id, IsActive, JobTitleId (+3 more)

### Community 76 - "EmployeeSummaryViewModel"
Cohesion: 0.18
Nodes (11): EmployeeSummaryViewModel, CreatedAtUtc, Email, EmployeeCode, FullName, Id, IsActive, JobTitleId (+3 more)

### Community 77 - "PaymentOrderTasweyaLinkViewModel"
Cohesion: 0.18
Nodes (11): PaymentOrderTasweyaLinkViewModel, ClaimNumber, CustomerNameAr, IsActive, JobOrderNumbersText, PaymentLinkId, PaymentOrderId, SettlementDate (+3 more)

### Community 78 - "AccountEntity"
Cohesion: 0.20
Nodes (10): AccountEntity, AccountCode, AccountId, AccountNameAr, BudgetCeiling, Customers, DebitHeaderTextAr, IsActive (+2 more)

### Community 79 - "UserPermissionOverride"
Cohesion: 0.20
Nodes (10): UserPermissionOverride, CanAccess, CanCreate, CanToggleActive, CanUpdate, Module, ModuleId, OverrideId (+2 more)

### Community 80 - "http"
Cohesion: 0.20
Nodes (9): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, profiles, http (+1 more)

### Community 81 - "BudgetValidationResultViewModel"
Cohesion: 0.22
Nodes (7): BudgetValidationResultViewModel, BudgetCeiling, IsValid, Message, RemainingBudget, RequestedAmount, TotalAllocated

### Community 83 - "CreatePaymentOrderDto"
Cohesion: 0.22
Nodes (8): CreatePaymentOrderDto, AccountId, ConfirmImmediately, CustomerId, Notes, PaymentDate, PaymentOrderNumber, TasweyaIds

### Community 84 - "CreateTasweyaDto"
Cohesion: 0.22
Nodes (8): CreateTasweyaDto, AccountId, ClaimNumber, CustomerId, MoqaysaIds, OverrideMarkupPercent, SettlementDate, SettlementNumber

### Community 85 - "UpdatePaymentOrderDto"
Cohesion: 0.25
Nodes (7): UpdatePaymentOrderDto, AccountId, CustomerId, Notes, PaymentDate, PaymentOrderNumber, TasweyaIds

### Community 86 - "PaymentOrderService.UpdateStatusAsync"
Cohesion: 0.31
Nodes (5): UpdatePaymentOrderStatusDto, NewStatus, Notes, Reason, Status

### Community 87 - "MoqaysaMaterialItemViewModel"
Cohesion: 0.22
Nodes (8): MoqaysaMaterialItemViewModel, ItemId, MaterialId, MaterialNameSnapshot, MoqaysaId, Quantity, TotalPrice, UnitPrice

### Community 88 - "UserPermissionOverrideViewModel"
Cohesion: 0.22
Nodes (8): UserPermissionOverrideViewModel, CanAccess, CanCreate, CanToggleActive, CanUpdate, ModuleId, OverrideId, UserId

### Community 89 - "AuthController.ResetPassword"
Cohesion: 0.32
Nodes (4): ResetPasswordDto, Code, Email, NewPassword

### Community 90 - "CreateEmployeeDto"
Cohesion: 0.25
Nodes (7): CreateEmployeeDto, DummyPassword, Email, EmployeeCode, FullName, JobTitleId, PhotoUrl

### Community 91 - "MachineryItemInputDto"
Cohesion: 0.25
Nodes (7): MachineryItemInputDto, Hours, MachineHourlyRateSnapshot, MachineHours, MachineryId, MachineryNameSnapshot, WorkerHourlyRateSnapshot

### Community 92 - "UpdateTasweyaDto"
Cohesion: 0.25
Nodes (8): UpdateTasweyaDto, AccountId, ClaimNumber, CustomerId, MoqaysaIds, OverrideMarkupPercent, SettlementDate, SettlementNumber

### Community 93 - "PaymentOrderTasweyaLink"
Cohesion: 0.25
Nodes (8): PaymentOrderTasweyaLink, IsActive, PaymentLinkId, PaymentOrder, PaymentOrderId, SnapshotTasweyaAmount, Tasweya, TasweyaId

### Community 94 - "SystemModule"
Cohesion: 0.25
Nodes (7): SystemModule, IsActive, ModuleId, ModuleKey, ModuleNameAr, RolePermissions, UserOverrides

### Community 95 - "PaymentOrderStatus"
Cohesion: 0.25
Nodes (7): PaymentOrderStatus, Approved, Cancelled, Draft, Paid, Rejected, UnderReview

### Community 98 - "CustomerTasweyaSummaryViewModel"
Cohesion: 0.25
Nodes (8): CustomerTasweyaSummaryViewModel, ClaimNumber, IsPaid, PaidAtUtc, SettlementDate, SettlementGrandTotal, SettlementNumber, TasweyaId

### Community 99 - "ModuleEffectivePermissionViewModel"
Cohesion: 0.25
Nodes (7): ModuleEffectivePermissionViewModel, CanAccess, CanCreate, CanToggleActive, CanUpdate, ModuleKey, ModuleNameAr

### Community 102 - "AuthController.VerifyResetCode"
Cohesion: 0.38
Nodes (3): VerifyResetCodeDto, Code, Email

### Community 104 - "PermissionOverrideInputDto"
Cohesion: 0.29
Nodes (6): PermissionOverrideInputDto, CanAccess, CanCreate, CanToggleActive, CanUpdate, ModuleId

### Community 105 - "RolePermissionInputDto"
Cohesion: 0.29
Nodes (6): RolePermissionInputDto, CanAccess, CanCreate, CanToggleActive, CanUpdate, ModuleId

### Community 106 - "UpdateEmployeeDto"
Cohesion: 0.29
Nodes (6): UpdateEmployeeDto, Email, EmployeeCode, FullName, JobTitleId, PhotoUrl

### Community 107 - "JobTitleSummaryViewModel"
Cohesion: 0.29
Nodes (6): JobTitleSummaryViewModel, Description, EmployeesCount, IsActive, JobTitleId, TitleNameAr

### Community 109 - "MaterialItemInputDto"
Cohesion: 0.33
Nodes (5): MaterialItemInputDto, MaterialId, MaterialNameSnapshot, Quantity, UnitPrice

### Community 110 - "UpdateFormulaConfigDto"
Cohesion: 0.33
Nodes (5): UpdateFormulaConfigDto, AllowedOptionsCsv, DefaultPercentage, FormulaConfigId, WorkersFundBaseMode

### Community 111 - "TasweyaMoqaysaLink"
Cohesion: 0.33
Nodes (6): TasweyaMoqaysaLink, LinkId, Moqaysa, MoqaysaId, Tasweya, TasweyaId

### Community 113 - "TasweyaAccountShareViewModel"
Cohesion: 0.33
Nodes (5): TasweyaAccountShareViewModel, AccountId, AccountNameAr, AllocatedAmount, SharePercentage

### Community 114 - "OperationStatusViewModel"
Cohesion: 0.40
Nodes (4): OperationStatusViewModel, Data, Message, Success

### Community 115 - "MoqaysatDetailedFilterDto"
Cohesion: 0.50
Nodes (4): MoqaysatDetailedFilterDto, CustomerId, FromDate, ToDate

### Community 116 - "PaginationRequestDto"
Cohesion: 0.50
Nodes (3): PaginationRequestDto, Page, PageSize

## Knowledge Gaps
- **1149 isolated node(s):** `net9.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (9.0.2)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.2)`, `Microsoft.EntityFrameworkCore.Design (9.0.2)`, `Microsoft.EntityFrameworkCore.SqlServer (9.0.2)` (+1144 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 1219 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **21 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Moqaysa` connect `Moqaysa` to `ApplicationDbContext`, `WordExportService`, `DateTime`, `MoqaysaAccountShare`, `PaymentOrderService`, `ApplicationDbContext.OnModelCreating`, `TasweyaMoqaysaLink`, `TasweyaService`, `MoqaysaService`, `PdfExportService`, `Customer`, `system_componentmodel_dataannotations`, `MoqaysaMachineryItem`, `SystemModule`?**
  _High betweenness centrality (0.087) - this node is a cross-community bridge._
- **What connects `net9.0`, `Microsoft.AspNetCore.Authentication.JwtBearer (9.0.2)`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore (9.0.2)` to the rest of the system?**
  _1149 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `PermissionAuthorize` be split into smaller, more focused modules?**
  _Cohesion score 0.08714969241285031 - nodes in this community are weakly interconnected._
- **Why does `MoqaysaDetailViewModel` connect `MoqaysaDetailViewModel` to `PermissionAuthorize`, `MoqaysaAccountShareViewModel`, `MoqaysaMachineryItemViewModel`, `DateTime`, `List`, `CentralPress.API.ViewModels`, `Task`, `MoqaysaService`, `MoqaysaMaterialItemViewModel`, `CustomerViewModel`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **Should `WordExportService` be split into smaller, more focused modules?**
  _Cohesion score 0.09288824383164006 - nodes in this community are weakly interconnected._
- **Why does `CentralPress.API.ViewModels` connect `CentralPress.API.ViewModels` to `MachineryViewModel`, `AuditBacklogViewModel`, `AccountViewModel`, `MoqaysaDetailedItemViewModel`, `CentralPress.API.DTOs`, `MoqaysaCalculationResultViewModel`, `PaperCuttingCalculationResultViewModel`, `MaterialViewModel`, `AuthController.cs`, `FormulaConfigViewModel`, `Program.cs`, `UserProfileViewModel`, `AccountSummaryViewModel`, `CustomerViewModel`, `ModulePermissionViewModel`, `MoqaysaMachineryItemViewModel`, `TasweyaMoqaysaItemViewModel`, `BudgetValidationResultViewModel`, `MoqaysaMaterialItemViewModel`, `UserPermissionOverrideViewModel`, `ModuleEffectivePermissionViewModel`, `JobTitleSummaryViewModel`, `TasweyaAccountShareViewModel`, `OperationStatusViewModel`?**
  _High betweenness centrality (0.052) - this node is a cross-community bridge._
- **Should `MoqaysaDetailViewModel` be split into smaller, more focused modules?**
  _Cohesion score 0.038461538461538464 - nodes in this community are weakly interconnected._