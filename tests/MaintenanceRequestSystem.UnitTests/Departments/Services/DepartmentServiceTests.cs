using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Departments.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.UnitTests.Departments.Fakes;

namespace MaintenanceRequestSystem.UnitTests.Departments.Services;

public sealed class DepartmentServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAndSavesDepartment()
    {
        // Arrange
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        var request = new CreateDepartmentRequest
        {
            Name = " Bilgi İşlem ",
            Description = " Teknik destek "
        };

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        Assert.Single(repository.Items);
        Assert.Equal(1, repository.SaveChangesCallCount);

        Assert.Equal("Bilgi İşlem", result.Name);
        Assert.Equal("Teknik destek", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WhenNameAlreadyExists_ThrowsException()
    {
        // Arrange
        var repository = new FakeDepartmentRepository();

        repository.Seed(
            new Department("Bilgi İşlem"));

        var service = new DepartmentService(repository);

        var request = new CreateDepartmentRequest
        {
            Name = "bilgi işlem"
        };

        // Act
        var action = async () =>
            await service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<ConflictException>(action);

        Assert.Single(repository.Items);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WhenDepartmentDoesNotExist_ReturnsNull()
    {
        // Arrange
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        // Act
        var result = await service.GetByIdAsync(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsValidationException()
    {
        // Arrange
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        var request = new CreateDepartmentRequest
        {
            Name = "   "
        };

        // Act
        var action = async () =>
            await service.CreateAsync(request);

        // Assert
        await Assert.ThrowsAsync<RequestValidationException>(action);

        Assert.Empty(repository.Items);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WithExistingDepartment_UpdatesAndSaves()
    {
        // Arrange
        var department = new Department(
            "Bilgi İşlem",
            "Eski açıklama");

        var repository = new FakeDepartmentRepository();
        repository.Seed(department);

        var service = new DepartmentService(repository);

        var request = new UpdateDepartmentRequest
        {
            Name = " Bilgi Teknolojileri ",
            Description = " Yeni açıklama "
        };

        // Act
        var result = await service.UpdateAsync(
            department.Id,
            request);

        // Assert
        Assert.NotNull(result);

        Assert.Equal("Bilgi Teknolojileri", result.Name);
        Assert.Equal("Yeni açıklama", result.Description);
        Assert.NotNull(result.UpdatedAt);

        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithExistingDepartment_DeactivatesDepartment()
    {
        // Arrange
        var department = new Department("Bilgi İşlem");

        var repository = new FakeDepartmentRepository();
        repository.Seed(department);

        var service = new DepartmentService(repository);

        // Act
        var result = await service.ChangeStatusAsync(
            department.Id,
            isActive: false);

        // Assert
        Assert.True(result);
        Assert.False(department.IsActive);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ChangeStatusAsync_WhenDepartmentDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        // Act
        var result = await service.ChangeStatusAsync(
            Guid.NewGuid(),
            isActive: false);

        // Assert
        Assert.False(result);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }
}