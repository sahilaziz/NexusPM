import 'package:freezed_annotation/freezed_annotation.dart';

part 'time_entry.freezed.dart';
part 'time_entry.g.dart';

@freezed
class TimeEntry with _$TimeEntry {
  const factory TimeEntry({
    required int timeEntryId,
    required int taskId,
    String? taskTitle,
    required int userId,
    required DateTime startTime,
    DateTime? endTime,
    int? durationMinutes,
    String? formattedDuration,
    String? description,
    required WorkType workType,
    required bool isBillable,
    double? calculatedAmount,
    required bool isRunning,
    required bool isApproved,
  }) = _TimeEntry;

  factory TimeEntry.fromJson(Map<String, dynamic> json) =>
      _$TimeEntryFromJson(json);

  const TimeEntry._();

  Duration get duration {
    if (durationMinutes != null) {
      return Duration(minutes: durationMinutes!);
    }
    if (isRunning) {
      return DateTime.now().difference(startTime);
    }
    return Duration.zero;
  }

  String get formattedTime {
    final d = duration;
    final hours = d.inHours;
    final minutes = d.inMinutes % 60;
    if (hours > 0) {
      return '${hours}h ${minutes}m';
    }
    return '${minutes}m';
  }
}

enum WorkType {
  development,
  design,
  testing,
  documentation,
  meeting,
  research,
  bugFix,
  codeReview,
  deployment,
  maintenance,
  training,
  other,
}

@freezed
class RunningTimer with _$RunningTimer {
  const factory RunningTimer({
    required int timeEntryId,
    required int taskId,
    required String taskTitle,
    required DateTime startTime,
    required String currentDuration,
    String? description,
    required WorkType workType,
  }) = _RunningTimer;

  factory RunningTimer.fromJson(Map<String, dynamic> json) =>
      _$RunningTimerFromJson(json);
}

@freezed
class DailyTimeSummary with _$DailyTimeSummary {
  const factory DailyTimeSummary({
    required DateTime date,
    required int userId,
    required String userName,
    required int totalMinutes,
    String? formattedTotal,
    double? totalAmount,
    required int entryCount,
    List<TimeEntry>? entries,
  }) = _DailyTimeSummary;

  factory DailyTimeSummary.fromJson(Map<String, dynamic> json) =>
      _$DailyTimeSummaryFromJson(json);
}

@freezed
class WeeklyTimeSummary with _$WeeklyTimeSummary {
  const factory WeeklyTimeSummary({
    required int year,
    required int weekNumber,
    required DateTime weekStart,
    required DateTime weekEnd,
    required int userId,
    required String userName,
    required int totalMinutes,
    String? formattedTotal,
    double? billableAmount,
    List<DailyBreakdown>? dailyBreakdown,
  }) = _WeeklyTimeSummary;

  factory WeeklyTimeSummary.fromJson(Map<String, dynamic> json) =>
      _$WeeklyTimeSummaryFromJson(json);
}

@freezed
class DailyBreakdown with _$DailyBreakdown {
  const factory DailyBreakdown({
    required DateTime date,
    required String dayName,
    required int totalMinutes,
    required int entryCount,
    required bool isToday,
  }) = _DailyBreakdown;

  factory DailyBreakdown.fromJson(Map<String, dynamic> json) =>
      _$DailyBreakdownFromJson(json);
}
