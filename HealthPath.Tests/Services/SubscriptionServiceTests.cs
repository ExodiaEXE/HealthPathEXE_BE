using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using HealthPath.API.Common;
using HealthPath.API.Models;
using HealthPath.API.Models.DTOs;
using HealthPath.API.Services;
using HealthPath.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthPath.Tests.Services;

public class SubscriptionServiceTests
{
    private readonly Mock<IIapVerificationService> _iapMock;
    private readonly Mock<ILogger<SubscriptionService>> _loggerMock;

    public SubscriptionServiceTests()
    {
        _iapMock = new Mock<IIapVerificationService>();
        _loggerMock = new Mock<ILogger<SubscriptionService>>();
    }

    [Fact]
    public async Task GetPlansAsync_ReturnsActivePlans()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Vip Gói Tháng",
            Code = "vip_monthly",
            IsActive = true,
            PriceMonthly = 50000,
            PriceYearly = 500000,
            Currency = "VND",
            Features = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Vip Gói Năm",
            Code = "vip_yearly",
            IsActive = false, // Inactive plan
            PriceMonthly = 40000,
            PriceYearly = 400000,
            Currency = "VND",
            Features = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _iapMock.Object, _loggerMock.Object);

        // Act
        var result = await service.GetPlansAsync();

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().HaveCount(1);
        result.Data![0].Name.Should().Be("Vip Gói Tháng");
    }

    [Fact]
    public async Task VerifyAndFulfillPurchaseAsync_ValidAndroidReceipt_CreatesTransactionAndSubscription()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();

        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Vip Gói Tháng",
            Code = "vip_monthly",
            GoogleProductId = "com.healthpath.premium.monthly",
            IsActive = true,
            Currency = "VND",
            Features = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var requestDto = new VerifyReceiptRequestDto
        {
            Platform = "GooglePlay",
            ProductId = "com.healthpath.premium.monthly",
            PurchaseToken = "valid_google_token",
            BillingCycle = "monthly"
        };

        var verificationResult = new IapVerificationResult
        {
            IsValid = true,
            PlatformTransactionId = "gplay_tx_123456",
            OriginalTransactionId = "gplay_orig_123456",
            PurchasedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMonths(1),
            Amount = 50000,
            Currency = "VND"
        };

        _iapMock.Setup(x => x.VerifyAndroidPurchaseAsync(requestDto.ProductId, requestDto.PurchaseToken))
            .ReturnsAsync(verificationResult);

        var service = new SubscriptionService(context, _iapMock.Object, _loggerMock.Object);

        // Act
        var result = await service.VerifyAndFulfillPurchaseAsync(userId, requestDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Status.Should().Be("active");
        result.Data.PlanId.Should().Be(planId);
        result.Data.PaymentRef.Should().Be("valid_google_token");

        // Verify Database
        context.Transactions.Should().HaveCount(1);
        var dbTx = context.Transactions.First();
        dbTx.PlatformTransactionId.Should().Be("gplay_tx_123456");
        dbTx.UserId.Should().Be(userId);

        context.UserSubscriptions.Should().HaveCount(1);
        var dbSub = context.UserSubscriptions.First();
        dbSub.Status.Should().Be("active");
        dbSub.UserId.Should().Be(userId);
        dbSub.PlanId.Should().Be(planId);
    }

    [Fact]
    public async Task VerifyAndFulfillPurchaseAsync_PlanNotFound_ReturnsFail()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        var requestDto = new VerifyReceiptRequestDto
        {
            Platform = "GooglePlay",
            ProductId = "unknown_product_id",
            PurchaseToken = "some_token"
        };

        var service = new SubscriptionService(context, _iapMock.Object, _loggerMock.Object);

        // Act
        var result = await service.VerifyAndFulfillPurchaseAsync(Guid.NewGuid(), requestDto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.SUBSCRIPTION_PLAN_NOT_FOUND.ToString());
    }

    [Fact]
    public async Task VerifyAndFulfillPurchaseAsync_VerificationFailed_ReturnsFail()
    {
        // Arrange
        using var context = DbContextFactory.Create();
        context.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Vip Gói Tháng",
            Code = "vip_monthly",
            AppleProductId = "com.healthpath.premium.monthly",
            IsActive = true,
            Currency = "VND",
            Features = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var requestDto = new VerifyReceiptRequestDto
        {
            Platform = "AppStore",
            ProductId = "com.healthpath.premium.monthly",
            PurchaseToken = "fail_token"
        };

        var verificationResult = new IapVerificationResult
        {
            IsValid = false,
            ErrorMessage = "Invalid Receipt"
        };

        _iapMock.Setup(x => x.VerifyIosPurchaseAsync(requestDto.ProductId, requestDto.PurchaseToken))
            .ReturnsAsync(verificationResult);

        var service = new SubscriptionService(context, _iapMock.Object, _loggerMock.Object);

        // Act
        var result = await service.VerifyAndFulfillPurchaseAsync(Guid.NewGuid(), requestDto);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.IAP_VERIFICATION_FAILED.ToString());
    }
}
