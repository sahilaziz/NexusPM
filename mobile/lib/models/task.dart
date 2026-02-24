import 'package:freezed_annotation/freezed_annotation.dart';

part 'task.freezed.dart';
part 'task.g.dart';

@freezed
class Task with _$Task {
  const factory Task({
    required int taskId,
    required int projectId,
    String? projectName,
    int? parentTaskId,
    required String taskTitle,
    String? taskDescription,
    String? assignedTo,
    String? assignedToName,
    String? assignedToAvatar,
    required TaskStatus status,
    required TaskPriority priority,
    DateTime? dueDate,
    DateTime? completedAt,
    DateTime? createdAt,
    List<TaskLabel>? labels,
    int? commentsCount,
    int? attachmentsCount,
    bool? isBlocked,
    int? trackedTimeMinutes,
  }) = _Task;

  factory Task.fromJson(Map<String, dynamic> json) => _$TaskFromJson(json);

  const Task._();

  bool get isOverdue {
    if (dueDate == null || status == TaskStatus.done) return false;
    return dueDate!.isBefore(DateTime.now());
  }

  bool get isDueToday {
    if (dueDate == null) return false;
    final now = DateTime.now();
    return dueDate!.year == now.year &&
        dueDate!.month == now.month &&
        dueDate!.day == now.day;
  }
}

enum TaskStatus {
  todo,
  inProgress,
  review,
  done,
  cancelled,
}

enum TaskPriority {
  low,
  medium,
  high,
  critical,
}

@freezed
class TaskLabel with _$TaskLabel {
  const factory TaskLabel({
    required int labelId,
    required String name,
    required String color,
  }) = _TaskLabel;

  factory TaskLabel.fromJson(Map<String, dynamic> json) =>
      _$TaskLabelFromJson(json);
}

@freezed
class TaskDetail with _$TaskDetail {
  const factory TaskDetail({
    required Task task,
    List<Task>? subTasks,
    List<TaskComment>? comments,
    List<TaskDependency>? dependencies,
  }) = _TaskDetail;

  factory TaskDetail.fromJson(Map<String, dynamic> json) =>
      _$TaskDetailFromJson(json);
}

@freezed
class TaskComment with _$TaskComment {
  const factory TaskComment({
    required int commentId,
    required int taskId,
    required String commentText,
    required String createdBy,
    String? createdByName,
    String? createdByAvatar,
    required DateTime createdAt,
    List<TaskComment>? replies,
  }) = _TaskComment;

  factory TaskComment.fromJson(Map<String, dynamic> json) =>
      _$TaskCommentFromJson(json);
}

@freezed
class TaskDependency with _$TaskDependency {
  const factory TaskDependency({
    required int dependencyId,
    required int taskId,
    required int dependsOnTaskId,
    required String dependsOnTaskTitle,
    required DependencyType type,
    bool? isBlocking,
  }) = _TaskDependency;

  factory TaskDependency.fromJson(Map<String, dynamic> json) =>
      _$TaskDependencyFromJson(json);
}

enum DependencyType {
  finishToStart,
  startToStart,
  finishToFinish,
  startToFinish,
}
