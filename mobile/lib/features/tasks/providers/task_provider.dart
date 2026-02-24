import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/api_client.dart';
import '../../../models/task.dart';

// Task Service
class TaskService {
  final ApiClient _apiClient;

  TaskService(this._apiClient);

  Future<List<Task>> getTasks({
    int? projectId,
    TaskStatus? status,
    String? assignedTo,
  }) async {
    final queryParams = <String, dynamic>{
      if (projectId != null) 'projectId': projectId,
      if (status != null) 'status': status.name,
      if (assignedTo != null) 'assignedTo': assignedTo,
    };

    final response = await _apiClient.get('/tasks', queryParameters: queryParams);
    final List<dynamic> data = response.data;
    return data.map((json) => Task.fromJson(json)).toList();
  }

  Future<Task> getTask(int id) async {
    final response = await _apiClient.get('/tasks/$id');
    return Task.fromJson(response.data);
  }

  Future<Task> createTask({
    required int projectId,
    required String title,
    String? description,
    TaskStatus status = TaskStatus.todo,
    TaskPriority priority = TaskPriority.medium,
    DateTime? dueDate,
    String? assignedTo,
  }) async {
    final response = await _apiClient.post('/tasks', data: {
      'projectId': projectId,
      'taskTitle': title,
      'taskDescription': description,
      'status': status.name,
      'priority': priority.name,
      'dueDate': dueDate?.toIso8601String(),
      'assignedTo': assignedTo,
    });
    return Task.fromJson(response.data);
  }

  Future<Task> updateTaskStatus(int taskId, TaskStatus status) async {
    final response = await _apiClient.patch('/tasks/$taskId/status', data: {
      'status': status.name,
    });
    return Task.fromJson(response.data);
  }

  Future<void> deleteTask(int taskId) async {
    await _apiClient.delete('/tasks/$taskId');
  }

  // Task Dependencies
  Future<List<TaskDependency>> getTaskDependencies(int taskId) async {
    final response = await _apiClient.get('/tasks/$taskId/dependencies');
    final List<dynamic> data = response.data;
    return data.map((json) => TaskDependency.fromJson(json)).toList();
  }

  Future<void> addDependency({
    required int taskId,
    required int dependsOnTaskId,
    DependencyType type = DependencyType.finishToStart,
  }) async {
    await _apiClient.post('/tasks/$taskId/dependencies', data: {
      'dependsOnTaskId': dependsOnTaskId,
      'type': type.name,
    });
  }

  // Task Labels
  Future<void> assignLabel(int taskId, int labelId) async {
    await _apiClient.post('/tasklabels/tasks/$taskId', data: {
      'labelId': labelId,
    });
  }

  Future<void> removeLabel(int taskId, int labelId) async {
    await _apiClient.delete('/tasklabels/tasks/$taskId/$labelId');
  }
}

// Task Service Provider
final taskServiceProvider = Provider<TaskService>((ref) {
  final apiClient = ref.watch(apiClientProvider);
  return TaskService(apiClient);
});

// Tasks State
class TasksState {
  final bool isLoading;
  final List<Task> tasks;
  final String? error;
  final Task? selectedTask;

  TasksState({
    this.isLoading = false,
    this.tasks = const [],
    this.error,
    this.selectedTask,
  });

  TasksState copyWith({
    bool? isLoading,
    List<Task>? tasks,
    String? error,
    Task? selectedTask,
  }) {
    return TasksState(
      isLoading: isLoading ?? this.isLoading,
      tasks: tasks ?? this.tasks,
      error: error ?? this.error,
      selectedTask: selectedTask ?? this.selectedTask,
    );
  }
}

// Tasks Notifier
class TasksNotifier extends StateNotifier<TasksState> {
  final TaskService _taskService;

  TasksNotifier(this._taskService) : super(TasksState());

  Future<void> loadTasks({
    int? projectId,
    TaskStatus? status,
    String? assignedTo,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    
    try {
      final tasks = await _taskService.getTasks(
        projectId: projectId,
        status: status,
        assignedTo: assignedTo,
      );
      state = state.copyWith(isLoading: false, tasks: tasks);
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Tapşırıqları yükləmək mümkün olmadı: $e',
      );
    }
  }

  Future<void> refresh() async {
    await loadTasks();
  }

  Future<void> updateTaskStatus(int taskId, TaskStatus status) async {
    try {
      await _taskService.updateTaskStatus(taskId, status);
      
      // Update local state
      final updatedTasks = state.tasks.map((task) {
        if (task.taskId == taskId) {
          return task.copyWith(status: status);
        }
        return task;
      }).toList();
      
      state = state.copyWith(tasks: updatedTasks);
    } catch (e) {
      state = state.copyWith(error: 'Status yenilənmədi: $e');
    }
  }

  void selectTask(Task task) {
    state = state.copyWith(selectedTask: task);
  }
}

// Tasks Provider
final tasksProvider = StateNotifierProvider<TasksNotifier, TasksState>((ref) {
  final taskService = ref.watch(taskServiceProvider);
  return TasksNotifier(taskService);
});

// Filtered Tasks Provider
final filteredTasksProvider = Provider.family<List<Task>, TaskFilter>((ref, filter) {
  final tasks = ref.watch(tasksProvider).tasks;
  
  return tasks.where((task) {
    if (filter.projectId != null && task.projectId != filter.projectId) {
      return false;
    }
    if (filter.status != null && task.status != filter.status) {
      return false;
    }
    if (filter.assignedTo != null && task.assignedTo != filter.assignedTo) {
      return false;
    }
    return true;
  }).toList();
});

class TaskFilter {
  final int? projectId;
  final TaskStatus? status;
  final String? assignedTo;

  TaskFilter({this.projectId, this.status, this.assignedTo});
}

// Today's Tasks Provider
final todaysTasksProvider = Provider<List<Task>>((ref) {
  final tasks = ref.watch(tasksProvider).tasks;
  final today = DateTime.now();
  
  return tasks.where((task) {
    if (task.dueDate == null) return false;
    return task.dueDate!.year == today.year &&
           task.dueDate!.month == today.month &&
           task.dueDate!.day == today.day;
  }).toList();
});

// Overdue Tasks Provider
final overdueTasksProvider = Provider<List<Task>>((ref) {
  final tasks = ref.watch(tasksProvider).tasks;
  final now = DateTime.now();
  
  return tasks.where((task) {
    if (task.dueDate == null) return false;
    if (task.status == TaskStatus.done) return false;
    return task.dueDate!.isBefore(now);
  }).toList();
});
