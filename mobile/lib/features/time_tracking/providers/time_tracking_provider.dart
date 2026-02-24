import 'dart:async';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/api_client.dart';
import '../../../models/time_entry.dart';

// Time Tracking Service
class TimeTrackingService {
  final ApiClient _apiClient;

  TimeTrackingService(this._apiClient);

  // Timer Operations
  Future<RunningTimer?> getRunningTimer() async {
    try {
      final response = await _apiClient.get('/time/timer');
      if (response.data['isRunning'] == true) {
        return RunningTimer.fromJson(response.data['timer']);
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  Future<TimeEntry> startTimer({
    required int taskId,
    String? description,
    WorkType workType = WorkType.development,
    bool isBillable = true,
  }) async {
    final response = await _apiClient.post('/time/timer/start', data: {
      'taskId': taskId,
      'description': description,
      'workType': workType.name,
      'isBillable': isBillable,
    });
    return TimeEntry.fromJson(response.data);
  }

  Future<TimeEntry?> stopTimer({String? description}) async {
    final response = await _apiClient.post('/time/timer/stop', data: {
      if (description != null) 'description': description,
    });
    if (response.data == null) return null;
    return TimeEntry.fromJson(response.data);
  }

  // Time Entries
  Future<List<TimeEntry>> getTimeEntries({
    DateTime? from,
    DateTime? to,
  }) async {
    final queryParams = <String, dynamic>{
      if (from != null) 'from': from.toIso8601String(),
      if (to != null) 'to': to.toIso8601String(),
    };

    final response = await _apiClient.get('/time/entries', queryParameters: queryParams);
    final List<dynamic> data = response.data;
    return data.map((json) => TimeEntry.fromJson(json)).toList();
  }

  Future<DailyTimeSummary> getDailySummary(DateTime date) async {
    final response = await _apiClient.get('/time/summary/daily', queryParameters: {
      'date': date.toIso8601String(),
    });
    return DailyTimeSummary.fromJson(response.data);
  }

  Future<WeeklyTimeSummary> getWeeklySummary(int year, int weekNumber) async {
    final response = await _apiClient.get('/time/summary/weekly', queryParameters: {
      'year': year,
      'week': weekNumber,
    });
    return WeeklyTimeSummary.fromJson(response.data);
  }

  // Manual Time Entry
  Future<TimeEntry> logTime({
    required int taskId,
    required DateTime startTime,
    required DateTime endTime,
    String? description,
    WorkType workType = WorkType.development,
    bool isBillable = true,
  }) async {
    final response = await _apiClient.post('/time/entries', data: {
      'taskId': taskId,
      'startTime': startTime.toIso8601String(),
      'endTime': endTime.toIso8601String(),
      'description': description,
      'workType': workType.name,
      'isBillable': isBillable,
    });
    return TimeEntry.fromJson(response.data);
  }
}

// Time Tracking Service Provider
final timeTrackingServiceProvider = Provider<TimeTrackingService>((ref) {
  final apiClient = ref.watch(apiClientProvider);
  return TimeTrackingService(apiClient);
});

// Timer State
class TimerState {
  final bool isRunning;
  final RunningTimer? runningTimer;
  final Duration elapsed;
  final bool isLoading;
  final String? error;

  TimerState({
    this.isRunning = false,
    this.runningTimer,
    this.elapsed = Duration.zero,
    this.isLoading = false,
    this.error,
  });

  TimerState copyWith({
    bool? isRunning,
    RunningTimer? runningTimer,
    Duration? elapsed,
    bool? isLoading,
    String? error,
  }) {
    return TimerState(
      isRunning: isRunning ?? this.isRunning,
      runningTimer: runningTimer ?? this.runningTimer,
      elapsed: elapsed ?? this.elapsed,
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
    );
  }
}

// Timer Notifier with periodic updates
class TimerNotifier extends StateNotifier<TimerState> {
  final TimeTrackingService _service;
  Timer? _timer;

  TimerNotifier(this._service) : super(TimerState()) {
    _loadRunningTimer();
  }

  Future<void> _loadRunningTimer() async {
    state = state.copyWith(isLoading: true);
    
    final runningTimer = await _service.getRunningTimer();
    
    if (runningTimer != null) {
      final startTime = runningTimer.startTime;
      final elapsed = DateTime.now().difference(startTime);
      
      state = TimerState(
        isRunning: true,
        runningTimer: runningTimer,
        elapsed: elapsed,
        isLoading: false,
      );
      
      _startPeriodicUpdate();
    } else {
      state = TimerState(isLoading: false);
    }
  }

  void _startPeriodicUpdate() {
    _timer?.cancel();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (state.isRunning && state.runningTimer != null) {
        final elapsed = DateTime.now().difference(state.runningTimer!.startTime);
        state = state.copyWith(elapsed: elapsed);
      }
    });
  }

  Future<void> startTimer({
    required int taskId,
    String? description,
    WorkType workType = WorkType.development,
  }) async {
    state = state.copyWith(isLoading: true);
    
    try {
      final entry = await _service.startTimer(
        taskId: taskId,
        description: description,
        workType: workType,
      );
      
      final runningTimer = RunningTimer(
        timeEntryId: entry.timeEntryId,
        taskId: entry.taskId,
        taskTitle: entry.taskTitle ?? 'Unknown',
        startTime: entry.startTime,
        currentDuration: entry.formattedTime,
        description: entry.description,
        workType: entry.workType,
      );
      
      state = TimerState(
        isRunning: true,
        runningTimer: runningTimer,
        elapsed: Duration.zero,
        isLoading: false,
      );
      
      _startPeriodicUpdate();
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Timer başlatmaq mümkün olmadı: $e',
      );
    }
  }

  Future<void> stopTimer({String? description}) async {
    state = state.copyWith(isLoading: true);
    
    try {
      final entry = await _service.stopTimer(description: description);
      
      _timer?.cancel();
      state = TimerState(
        isRunning: false,
        isLoading: false,
      );
      
      // Refresh entries
      await ref.read(timeEntriesProvider.notifier).refresh();
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Timer dayandırmaq mümkün olmadı: $e',
      );
    }
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}

// Timer Provider
final timerProvider = StateNotifierProvider<TimerNotifier, TimerState>((ref) {
  final service = ref.watch(timeTrackingServiceProvider);
  return TimerNotifier(service);
});

// Time Entries State
class TimeEntriesState {
  final bool isLoading;
  final List<TimeEntry> entries;
  final DailyTimeSummary? todaySummary;
  final WeeklyTimeSummary? weeklySummary;
  final String? error;

  TimeEntriesState({
    this.isLoading = false,
    this.entries = const [],
    this.todaySummary,
    this.weeklySummary,
    this.error,
  });

  TimeEntriesState copyWith({
    bool? isLoading,
    List<TimeEntry>? entries,
    DailyTimeSummary? todaySummary,
    WeeklyTimeSummary? weeklySummary,
    String? error,
  }) {
    return TimeEntriesState(
      isLoading: isLoading ?? this.isLoading,
      entries: entries ?? this.entries,
      todaySummary: todaySummary ?? this.todaySummary,
      weeklySummary: weeklySummary ?? this.weeklySummary,
      error: error ?? this.error,
    );
  }
}

// Time Entries Notifier
class TimeEntriesNotifier extends StateNotifier<TimeEntriesState> {
  final TimeTrackingService _service;

  TimeEntriesNotifier(this._service) : super(TimeEntriesState()) {
    loadTodaySummary();
  }

  Future<void> loadTodaySummary() async {
    state = state.copyWith(isLoading: true);
    
    try {
      final summary = await _service.getDailySummary(DateTime.now());
      state = state.copyWith(
        isLoading: false,
        todaySummary: summary,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Hesabat yüklənmədi: $e',
      );
    }
  }

  Future<void> loadWeeklySummary(int year, int weekNumber) async {
    state = state.copyWith(isLoading: true);
    
    try {
      final summary = await _service.getWeeklySummary(year, weekNumber);
      state = state.copyWith(
        isLoading: false,
        weeklySummary: summary,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Həftəlik hesabat yüklənmədi: $e',
      );
    }
  }

  Future<void> refresh() async {
    await loadTodaySummary();
  }
}

// Time Entries Provider
final timeEntriesProvider = StateNotifierProvider<TimeEntriesNotifier, TimeEntriesState>((ref) {
  final service = ref.watch(timeTrackingServiceProvider);
  return TimeEntriesNotifier(service);
});

// Formatted Time Provider
final formattedTimerProvider = Provider<String>((ref) {
  final elapsed = ref.watch(timerProvider).elapsed;
  final hours = elapsed.inHours.toString().padLeft(2, '0');
  final minutes = (elapsed.inMinutes % 60).toString().padLeft(2, '0');
  final seconds = (elapsed.inSeconds % 60).toString().padLeft(2, '0');
  return '$hours:$minutes:$seconds';
});
