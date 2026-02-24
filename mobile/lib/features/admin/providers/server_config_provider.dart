import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:dio/dio.dart';
import '../../../core/network/dio_client.dart';
import '../../../core/auth/auth_provider.dart';

/// Server Config State
class ServerConfigState {
  final Map<String, dynamic>? config;
  final bool isLoading;
  final String? error;

  const ServerConfigState({
    this.config,
    this.isLoading = false,
    this.error,
  });

  ServerConfigState copyWith({
    Map<String, dynamic>? config,
    bool? isLoading,
    String? error,
  }) {
    return ServerConfigState(
      config: config ?? this.config,
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
    );
  }
}

/// Server Config Notifier
class ServerConfigNotifier extends StateNotifier<AsyncValue<Map<String, dynamic>>> {
  final Dio _dio;
  final Ref _ref;

  ServerConfigNotifier(this._dio, this._ref) : super(const AsyncValue.loading()) {
    fetchConfig();
  }

  /// Config al
  Future<void> fetchConfig() async {
    try {
      state = const AsyncValue.loading();
      
      final response = await _dio.get('/api/admin/server-config');
      state = AsyncValue.data(response.data);
    } on DioException catch (e) {
      state = AsyncValue.error(
        e.response?.data?['message'] ?? e.message ?? 'Unknown error',
        StackTrace.current,
      );
    } catch (e) {
      state = AsyncValue.error(e.toString(), StackTrace.current);
    }
  }

  /// Status al
  Future<Map<String, dynamic>> fetchStatus() async {
    try {
      final response = await _dio.get('/api/admin/server-config/status');
      return response.data;
    } catch (e) {
      throw Exception('Status almaq mümkün olmadı: $e');
    }
  }

  /// Mode dəyiş (Messaging və ya Monitoring)
  Future<void> switchMode(String system, String mode) async {
    try {
      final endpoint = system == 'messaging' 
          ? '/api/admin/server-config/messaging/switch'
          : '/api/admin/server-config/monitoring/switch';

      final response = await _dio.post(
        endpoint,
        data: {'mode': mode},
      );

      // Refresh config after switch
      await fetchConfig();
      
      return response.data;
    } on DioException catch (e) {
      throw Exception(e.response?.data?['message'] ?? 'Switch uğursuz oldu');
    }
  }

  /// Hər ikisini birdən dəyiş
  Future<void> switchAll(String messagingMode, String monitoringMode) async {
    try {
      final response = await _dio.post(
        '/api/admin/server-config/switch-all',
        data: {
          'messagingMode': messagingMode,
          'monitoringMode': monitoringMode,
        },
      );

      await fetchConfig();
      return response.data;
    } on DioException catch (e) {
      throw Exception(e.response?.data?['message'] ?? 'Switch uğursuz oldu');
    }
  }

  /// Azure connection string yenilə
  Future<void> updateAzureConnection(String service, String connectionString) async {
    try {
      final endpoint = service == 'servicebus'
          ? '/api/admin/server-config/azure/servicebus-connection'
          : '/api/admin/server-config/azure/appinsights-connection';

      final response = await _dio.put(
        endpoint,
        data: {'connectionString': connectionString},
      );

      return response.data;
    } on DioException catch (e) {
      throw Exception(e.response?.data?['message'] ?? 'Connection string yenilənmədi');
    }
  }
}

/// Providers
final serverConfigProvider = StateNotifierProvider<ServerConfigNotifier, AsyncValue<Map<String, dynamic>>>((ref) {
  final dio = ref.watch(dioClientProvider);
  return ServerConfigNotifier(dio, ref);
});

final serverStatusProvider = FutureProvider<Map<String, dynamic>>((ref) async {
  final dio = ref.watch(dioClientProvider);
  final notifier = ServerConfigNotifier(dio, ref);
  return notifier.fetchStatus();
});

/// Connection String Input State
class ConnectionStringState {
  final String? serviceBusConnection;
  final String? appInsightsConnection;
  final bool isLoading;
  final String? error;

  const ConnectionStringState({
    this.serviceBusConnection,
    this.appInsightsConnection,
    this.isLoading = false,
    this.error,
  });

  ConnectionStringState copyWith({
    String? serviceBusConnection,
    String? appInsightsConnection,
    bool? isLoading,
    String? error,
  }) {
    return ConnectionStringState(
      serviceBusConnection: serviceBusConnection ?? this.serviceBusConnection,
      appInsightsConnection: appInsightsConnection ?? this.appInsightsConnection,
      isLoading: isLoading ?? this.isLoading,
      error: error ?? this.error,
    );
  }
}

/// Connection String Notifier
class ConnectionStringNotifier extends StateNotifier<ConnectionStringState> {
  final Dio _dio;

  ConnectionStringNotifier(this._dio) : super(const ConnectionStringState());

  Future<void> saveServiceBusConnection(String connectionString) async {
    try {
      state = state.copyWith(isLoading: true, error: null);
      
      await _dio.put(
        '/api/admin/server-config/azure/servicebus-connection',
        data: {'connectionString': connectionString},
      );

      state = state.copyWith(
        isLoading: false,
        serviceBusConnection: connectionString,
      );
    } on DioException catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: e.response?.data?['message'] ?? 'Xəta baş verdi',
      );
      throw Exception(state.error);
    }
  }

  Future<void> saveAppInsightsConnection(String connectionString) async {
    try {
      state = state.copyWith(isLoading: true, error: null);
      
      await _dio.put(
        '/api/admin/server-config/azure/appinsights-connection',
        data: {'connectionString': connectionString},
      );

      state = state.copyWith(
        isLoading: false,
        appInsightsConnection: connectionString,
      );
    } on DioException catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: e.response?.data?['message'] ?? 'Xəta baş verdi',
      );
      throw Exception(state.error);
    }
  }
}

final connectionStringProvider = StateNotifierProvider<ConnectionStringNotifier, ConnectionStringState>((ref) {
  final dio = ref.watch(dioClientProvider);
  return ConnectionStringNotifier(dio);
});
