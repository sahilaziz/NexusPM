using Moq;
using Nexus.Application.CQRS.Commands.TaskDependencies;
using Nexus.Application.Interfaces;
using Nexus.Application.Interfaces.Repositories;
using Nexus.Domain.Entities;
using Shouldly;
using Xunit;

namespace Nexus.UnitTests.Commands;

/// <summary>
/// AddTaskDependencyCommand handler tests
/// </summary>
public class AddTaskDependencyCommandTests
{
    private readonly Mock<ITaskDependencyRepository> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddTaskDependencyHandler _handler;

    public AddTaskDependencyCommandTests()
    {
        _repositoryMock = new Mock<ITaskDependencyRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new AddTaskDependencyHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDependency_AddsAndReturnsSuccess()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            LagDays: 0,
            Description: "Test dependency",
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.WouldCreateCycleAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetTaskStatusAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TaskStatus.Todo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.IsValid.ShouldBeTrue();
        result.TaskId.ShouldBe(2);
        result.DependsOnTaskId.ShouldBe(1);
        result.Type.ShouldBe(DependencyType.FinishToStart);

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskDependency>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_SelfDependency_ThrowsException()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 1,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            CreatedBy: "testuser"
        );

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken.None);
        });

        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<TaskDependency>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_DifferentProjects_ThrowsException()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(2L); // Fərqli layihə

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken.None);
        });

        exception.Message.ShouldContain("Fərqli layihə");
    }

    [Fact]
    public async Task Handle_CircularDependency_ThrowsException()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.WouldCreateCycleAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Dairəvi asılılıq!

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken.None);
        });

        exception.Message.ShouldContain("Dairəvi asılılıq");
    }

    [Fact]
    public async Task Handle_DuplicateDependency_ThrowsException()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.WouldCreateCycleAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Artıq mövcuddur

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _handler.Handle(command, CancellationToken.None);
        });

        exception.Message.ShouldContain("artıq mövcuddur");
    }

    [Fact]
    public async Task Handle_IncompleteDependency_ReturnsWithWarning()
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: DependencyType.FinishToStart,
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.WouldCreateCycleAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsAsync(2, 1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetTaskStatusAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TaskStatus.InProgress); // Hələ tamamlanmayıb

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Warning.ShouldNotBeNull();
        result.Warning.ShouldContain("bloklayır");
    }

    [Theory]
    [InlineData(DependencyType.FinishToStart)]
    [InlineData(DependencyType.StartToStart)]
    [InlineData(DependencyType.FinishToFinish)]
    [InlineData(DependencyType.StartToFinish)]
    public async Task Handle_AllDependencyTypes_Works(DependencyType type)
    {
        // Arrange
        var command = new AddTaskDependencyCommand(
            TaskId: 2,
            DependsOnTaskId: 1,
            Type: type,
            LagDays: 5,
            CreatedBy: "testuser"
        );

        _repositoryMock.Setup(r => r.GetTaskProjectIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1L);
        _repositoryMock.Setup(r => r.WouldCreateCycleAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.ExistsAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _repositoryMock.Setup(r => r.GetTaskStatusAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TaskStatus.Done);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Type.ShouldBe(type);
    }
}
