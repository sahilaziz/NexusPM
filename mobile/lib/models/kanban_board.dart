import 'package:freezed_annotation/freezed_annotation.dart';
import 'task.dart';

part 'kanban_board.freezed.dart';
part 'kanban_board.g.dart';

@freezed
class KanbanBoard with _$KanbanBoard {
  const factory KanbanBoard({
    required int projectId,
    required String projectName,
    required List<KanbanColumn> columns,
    required int totalTasks,
    int? wipLimit,
  }) = _KanbanBoard;

  factory KanbanBoard.fromJson(Map<String, dynamic> json) =>
      _$KanbanBoardFromJson(json);
}

@freezed
class KanbanColumn with _$KanbanColumn {
  const factory KanbanColumn({
    required String id,
    required String title,
    required TaskStatus status,
    required String color,
    required List<KanbanTask> tasks,
    int? wipLimit,
  }) = _KanbanColumn;

  factory KanbanColumn.fromJson(Map<String, dynamic> json) =>
      _$KanbanColumnFromJson(json);

  const KanbanColumn._();

  int get taskCount => tasks.length;
  bool get isOverLimit => wipLimit != null && tasks.length > wipLimit!;
}

@freezed
class KanbanTask with _$KanbanTask {
  const factory KanbanTask({
    required int taskId,
    required String taskTitle,
    String? taskDescription,
    required TaskStatus status,
    required TaskPriority priority,
    String? assignedToName,
    String? assignedToAvatar,
    String? assignedToInitials,
    DateTime? dueDate,
    bool? isOverdue,
    bool? isDueToday,
    int? commentsCount,
    int? attachmentsCount,
    int? subTasksCount,
    int? completedSubTasksCount,
    bool? hasDependencies,
    bool? isBlocked,
    int? trackedTimeMinutes,
    List<TaskLabel>? labels,
    required int sortOrder,
  }) = _KanbanTask;

  factory KanbanTask.fromJson(Map<String, dynamic> json) =>
      _$KanbanTaskFromJson(json);

  const KanbanTask._();

  double get progressPercent {
    if (subTasksCount == null || subTasksCount == 0) {
      return status == TaskStatus.done ? 1.0 : 0.0;
    }
    return (completedSubTasksCount ?? 0) / subTasksCount!;
  }
}
