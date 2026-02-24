import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart';
import '../storage/secure_storage.dart';

/// API Client configuration
class ApiConfig {
  static const String baseUrl = 'https://api.nexus.local/api';
  static const Duration connectTimeout = Duration(seconds: 30);
  static const Duration receiveTimeout = Duration(seconds: 30);
  static const Map<String, String> defaultHeaders = {
    'Content-Type': 'application/json',
    'Accept': 'application/json',
  };
}

/// Dio HTTP Client with interceptors
class ApiClient {
  late final Dio _dio;
  final SecureStorage _secureStorage;

  ApiClient(this._secureStorage) {
    _dio = Dio(BaseOptions(
      baseUrl: ApiConfig.baseUrl,
      connectTimeout: ApiConfig.connectTimeout,
      receiveTimeout: ApiConfig.receiveTimeout,
      headers: ApiConfig.defaultHeaders,
    ));

    _setupInterceptors();
  }

  Dio get dio => _dio;

  void _setupInterceptors() {
    // Request Interceptor - Add auth token
    _dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _secureStorage.getToken();
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        
        if (kDebugMode) {
          print('📤 REQUEST: ${options.method} ${options.path}');
          print('Headers: ${options.headers}');
          if (options.data != null) {
            print('Body: ${options.data}');
          }
        }
        
        return handler.next(options);
      },
      onResponse: (response, handler) {
        if (kDebugMode) {
          print('📥 RESPONSE: ${response.statusCode} ${response.requestOptions.path}');
          print('Data: ${response.data}');
        }
        return handler.next(response);
      },
      onError: (error, handler) async {
        if (kDebugMode) {
          print('❌ ERROR: ${error.response?.statusCode} ${error.requestOptions.path}');
          print('Message: ${error.message}');
          print('Response: ${error.response?.data}');
        }

        // Handle 401 Unauthorized - Token expired
        if (error.response?.statusCode == 401) {
          await _secureStorage.clearToken();
          // TODO: Navigate to login
        }

        return handler.next(error);
      },
    ));

    // Logging interceptor for debug
    if (kDebugMode) {
      _dio.interceptors.add(LogInterceptor(
        requestBody: true,
        responseBody: true,
        error: true,
      ));
    }
  }

  // HTTP Methods
  Future<Response> get(String path, {Map<String, dynamic>? queryParameters}) async {
    return await _dio.get(path, queryParameters: queryParameters);
  }

  Future<Response> post(String path, {dynamic data}) async {
    return await _dio.post(path, data: data);
  }

  Future<Response> put(String path, {dynamic data}) async {
    return await _dio.put(path, data: data);
  }

  Future<Response> delete(String path) async {
    return await _dio.delete(path);
  }

  Future<Response> patch(String path, {dynamic data}) async {
    return await _dio.patch(path, data: data);
  }
}

/// API Response wrapper
class ApiResponse<T> {
  final bool success;
  final T? data;
  final String? errorMessage;
  final int? statusCode;

  ApiResponse({
    required this.success,
    this.data,
    this.errorMessage,
    this.statusCode,
  });

  factory ApiResponse.success(T data, {int? statusCode}) {
    return ApiResponse(
      success: true,
      data: data,
      statusCode: statusCode,
    );
  }

  factory ApiResponse.error(String message, {int? statusCode}) {
    return ApiResponse(
      success: false,
      errorMessage: message,
      statusCode: statusCode,
    );
  }
}

/// API Exception
class ApiException implements Exception {
  final String message;
  final int? statusCode;

  ApiException(this.message, {this.statusCode});

  @override
  String toString() => 'ApiException: $message (Status: $statusCode)';
}
