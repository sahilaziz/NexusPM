import 'package:freezed_annotation/freezed_annotation.dart';

part 'project.freezed.dart';
part 'project.g.dart';

@freezed
class Project with _$Project {
  const factory Project({
    required int projectId,
    required String projectCode,
    required String projectName,
    String? description,
    required String organizationCode,
    required ProjectStatus status,
    DateTime? startDate,
    DateTime? endDate,
    int? totalTasks,
    int? completedTasks,
    double? progressPercent,
    DateTime? createdAt,
  }) = _Project;

  factory Project.fromJson(Map<String, dynamic> json) =>
      _$ProjectFromJson(json);
}

enum ProjectStatus {
  planning,
  active,
  onHold,
  completed,
  cancelled,
}

@freezed
class ProjectSummary with _$ProjectSummary {
  const factory ProjectSummary({
    required int projectId,
    required String projectName,
    required ProjectStatus status,
    required int taskCount,
    required int completedTasks,
    required double progressPercent,
  }) = _ProjectSummary;

  factory ProjectSummary.fromJson(Map<String, dynamic> json) =>
      _$ProjectSummaryFromJson(json);
}
