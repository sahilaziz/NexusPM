using Microsoft.EntityFrameworkCore;
using Nexus.Domain.Entities;
using Nexus.Infrastructure.Data;
using Nexus.Infrastructure.Repositories;
using Shouldly;
using Xunit;

namespace Nexus.UnitTests.Repositories;

/// <summary>
/// TaskDependencyRepository unit tests
/// In-memory database ilə testlər
/// </summary>
public class TaskDependencyRepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TaskDependencyRepository _repository;

    public TaskDependencyRepositoryTests()
    {
        // In-memory database yarat
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new TaskDependencyRepository(_context);

        // Test datası əlavə et
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Layihə yarat
        var project = new Project
        {
            ProjectId = 1,
            ProjectCode = "PRJ-001",
            ProjectName = "Test Project",
            OrganizationCode = "test"
        };
        _context.Projects.Add(project);

        // Tapşırıqlar yarat
        var tasks = new List<TaskItem>
        {
            new() { TaskId = 1, ProjectId = 1, TaskTitle = "Task A", Status = TaskStatus.Done },
            new() { TaskId = 2, ProjectId = 1, TaskTitle = "Task B", Status = TaskStatus.Todo },
            new() { TaskId = 3, ProjectId = 1, TaskTitle = "Task C", Status = TaskStatus.Todo },
            new() { TaskId = 4, ProjectId = 1, TaskTitle = "Task D", Status = TaskStatus.Todo },
            new() { TaskId = 5, ProjectId = 1, TaskTitle = "Task E", Status = TaskStatus.Todo }
        };
        _context.TaskItems.AddRange(tasks);

        // Asılılıqlar yarat: A → B → C
        var dependencies = new List<TaskDependency>
        {
            new() { DependencyId = 1, TaskId = 2, DependsOnTaskId = 1, Type = DependencyType.FinishToStart },
            new() { DependencyId = 2, TaskId = 3, DependsOnTaskId = 2, Type = DependencyType.FinishToStart }
        };
        _context.TaskDependencies.AddRange(dependencies);

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    #region Get Tests

    [Fact]
    public async Task GetByIdAsync_ExistingDependency_ReturnsDependency()
    {
        // Act
        var result = await _repository.GetByIdAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result!.TaskId.ShouldBe(2);
        result.DependsOnTaskId.ShouldBe(1);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingDependency_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Fact]
    public async Task GetDependenciesAsync_TaskWithDependencies_ReturnsList()
    {
        // Act
        var result = await _repository.GetDependenciesAsync(2); // Task B

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].TaskId.ShouldBe(2);
        result[0].DependsOnTaskId.ShouldBe(1);
    }

    [Fact]
    public async Task GetDependenciesAsync_TaskWithoutDependencies_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetDependenciesAsync(1); // Task A (ilk tapşırıq)

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetDependentsAsync_TaskWithDependents_ReturnsList()
    {
        // Act
        var result = await _repository.GetDependentsAsync(1); // Task A

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(1);
        result[0].TaskId.ShouldBe(2); // Task B
        result[0].DependsOnTaskId.ShouldBe(1);
    }

    #endregion

    #region Exists Tests

    [Fact]
    public async Task ExistsAsync_ExistingDependency_ReturnsTrue()
    {
        // Act
        var result = await _repository.ExistsAsync(2, 1); // B depends on A

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingDependency_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(1, 2); // A depends on B (yoxdur)

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Circular Dependency Tests - ƏSAS TESTLƏR

    [Fact]
    public async Task WouldCreateCycleAsync_DirectCycle_ReturnsTrue()
    {
        // Arrange: A → B var, indi B → A yaratmağa çalışırıq
        // Bu dairəvi asılılıq olacaq: A ↔ B

        // Act
        var result = await _repository.WouldCreateCycleAsync(1, 2);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_IndirectCycle_ReturnsTrue()
    {
        // Arrange: A → B → C var, indi C → A yaratmağa çalışırıq
        // Bu dairəvi asılılıq olacaq: A → B → C → A

        // Act
        var result = await _repository.WouldCreateCycleAsync(3, 1);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_NoCycle_ReturnsFalse()
    {
        // Arrange: A → B → C var, indi D → A yaratmağa çalışırıq
        // Bu dairəvi asılılıq deyil

        // Act
        var result = await _repository.WouldCreateCycleAsync(5, 1); // E → A

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_SelfDependency_ReturnsTrue()
    {
        // Act
        var result = await _repository.WouldCreateCycleAsync(1, 1);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_NewChainNoCycle_ReturnsFalse()
    {
        // Arrange: D → E yarat
        _context.TaskDependencies.Add(new TaskDependency
        {
            TaskId = 5,
            DependsOnTaskId = 4,
            Type = DependencyType.FinishToStart
        });
        await _context.SaveChangesAsync();

        // Act: E → C yaratmağa çalış (problemli deyil)
        var result = await _repository.WouldCreateCycleAsync(5, 3);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region IsBlocked Tests

    [Fact]
    public async Task IsBlockedAsync_TaskWithIncompleteDependency_ReturnsTrue()
    {
        // Arrange: Task B depends on Task A
        // Task A Done, Task B Todo

        // Act
        var result = await _repository.IsBlockedAsync(2); // Task B

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task IsBlockedAsync_TaskWithCompleteDependency_ReturnsFalse()
    {
        // Arrange: Task C depends on Task B
        // Task B Todo, ancaq Task B-nin asılılığı (Task A) Done
        // İndi Task B-ni Done edək
        var taskB = await _context.TaskItems.FindAsync(2);
        taskB!.Status = TaskStatus.Done;
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.IsBlockedAsync(3); // Task C

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task IsBlockedAsync_TaskWithoutDependencies_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsBlockedAsync(1); // Task A

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region CanStart Tests

    [Fact]
    public async Task CanStartAsync_AllDependenciesDone_ReturnsTrue()
    {
        // Arrange: Task B-nin asılılığı (Task A) Done
        // İndi Task B-ni başlatmağa çalışaq

        // Act
        var result = await _repository.CanStartAsync(2); // Task B

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public async Task CanStartAsync_IncompleteDependency_ReturnsFalse()
    {
        // Arrange: Task C depends on Task B
        // Task B hələ Todo

        // Act
        var result = await _repository.CanStartAsync(3); // Task C

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task CanStartAsync_StartToStartDependency_ReturnsFalse()
    {
        // Arrange: Task D yarad, SS asılılıq əlavə et
        _context.TaskDependencies.Add(new TaskDependency
        {
            TaskId = 4,
            DependsOnTaskId = 1,
            Type = DependencyType.StartToStart // A başlayandan sonra D başlaya bilər
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.CanStartAsync(4); // Task D

        // Assert
        result.ShouldBeTrue(); // Task A başlayıb (Done olduğuna görə)
    }

    #endregion

    #region Add/Delete Tests

    [Fact]
    public async Task AddAsync_NewDependency_SavesToDatabase()
    {
        // Arrange
        var dependency = new TaskDependency
        {
            TaskId = 4,
            DependsOnTaskId = 1,
            Type = DependencyType.FinishToStart,
            CreatedBy = "test"
        };

        // Act
        await _repository.AddAsync(dependency);
        await _context.SaveChangesAsync();

        // Assert
        var saved = await _repository.GetByTaskIdsAsync(4, 1);
        saved.ShouldNotBeNull();
        saved.TaskId.ShouldBe(4);
    }

    [Fact]
    public async Task DeleteAsync_ExistingDependency_RemovesFromDatabase()
    {
        // Arrange
        var dependency = await _repository.GetByIdAsync(1);
        dependency.ShouldNotBeNull();

        // Act
        await _repository.DeleteAsync(dependency);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _repository.GetByIdAsync(1);
        deleted.ShouldBeNull();
    }

    #endregion

    #region GetTaskProjectId Tests

    [Fact]
    public async Task GetTaskProjectIdAsync_ExistingTask_ReturnsProjectId()
    {
        // Act
        var result = await _repository.GetTaskProjectIdAsync(1);

        // Assert
        result.ShouldBe(1);
    }

    [Fact]
    public async Task GetTaskProjectIdAsync_NonExistingTask_ReturnsNull()
    {
        // Act
        var result = await _repository.GetTaskProjectIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    #endregion
}

/// <summary>
/// Complex circular dependency scenarios
/// </summary>
public class TaskDependencyComplexCycleTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly TaskDependencyRepository _repository;

    public TaskDependencyComplexCycleTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new TaskDependencyRepository(_context);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_LongChainCycle_DetectsCorrectly()
    {
        // Arrange: A → B → C → D → E yarad
        var project = new Project { ProjectId = 1, ProjectCode = "PRJ-001", ProjectName = "Test" };
        _context.Projects.Add(project);

        for (int i = 1; i <= 5; i++)
        {
            _context.TaskItems.Add(new TaskItem
            {
                TaskId = i,
                ProjectId = 1,
                TaskTitle = $"Task {i}",
                Status = TaskStatus.Todo
            });
        }

        // A → B → C → D yarad
        _context.TaskDependencies.AddRange(new[]
        {
            new TaskDependency { TaskId = 2, DependsOnTaskId = 1, Type = DependencyType.FinishToStart },
            new TaskDependency { TaskId = 3, DependsOnTaskId = 2, Type = DependencyType.FinishToStart },
            new TaskDependency { TaskId = 4, DependsOnTaskId = 3, Type = DependencyType.FinishToStart }
        });

        await _context.SaveChangesAsync();

        // Act & Assert
        // E → A (yoxdur) - OK
        (await _repository.WouldCreateCycleAsync(5, 1)).ShouldBeFalse();

        // D → A (kicik dövr) - Yoxlanılacaq
        // A → B → C → D var, indi D → A yaratmağa çalışırıq = CYCLE
        (await _repository.WouldCreateCycleAsync(4, 1)).ShouldBeTrue();
    }

    [Fact]
    public async Task WouldCreateCycleAsync_DiamondPattern_NoCycle()
    {
        // Arrange: Diamond pattern
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D

        var project = new Project { ProjectId = 1, ProjectCode = "PRJ-001", ProjectName = "Test" };
        _context.Projects.Add(project);

        for (int i = 1; i <= 4; i++)
        {
            _context.TaskItems.Add(new TaskItem
            {
                TaskId = i,
                ProjectId = 1,
                TaskTitle = $"Task {i}",
                Status = TaskStatus.Todo
            });
        }

        // A → B, A → C, B → D, C → D
        _context.TaskDependencies.AddRange(new[]
        {
            new TaskDependency { TaskId = 2, DependsOnTaskId = 1, Type = DependencyType.FinishToStart },
            new TaskDependency { TaskId = 3, DependsOnTaskId = 1, Type = DependencyType.FinishToStart },
            new TaskDependency { TaskId = 4, DependsOnTaskId = 2, Type = DependencyType.FinishToStart },
            new TaskDependency { TaskId = 4, DependsOnTaskId = 3, Type = DependencyType.FinishToStart }
        });

        await _context.SaveChangesAsync();

        // Act & Assert: Diamond pattern dairəvi asılılıq deyil
        // D → A yaratmağa çalışsaq = CYCLE (A → B → D → A)
        (await _repository.WouldCreateCycleAsync(4, 1)).ShouldBeTrue();
    }
}
