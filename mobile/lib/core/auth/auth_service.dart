import 'package:dio/dio.dart';
import '../api/api_client.dart';
import '../storage/secure_storage.dart';

/// Authentication Service
class AuthService {
  final ApiClient _apiClient;
  final SecureStorage _secureStorage;

  AuthService(this._apiClient, this._secureStorage);

  /// Login with username and password
  Future<AuthResult> login({
    required String username,
    required String password,
    String? twoFactorCode,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/auth/login',
        data: {
          'username': username,
          'password': password,
          if (twoFactorCode != null) 'twoFactorCode': twoFactorCode,
        },
      );

      final data = response.data;
      
      // Save tokens
      await _secureStorage.saveToken(data['token']);
      await _secureStorage.saveRefreshToken(data['refreshToken']);
      
      // Save user info
      await _secureStorage.saveUserInfo(
        userId: data['userId'].toString(),
        userName: data['userName'],
        email: data['email'],
        organizationCode: data['organizationCode'] ?? 'default',
      );

      return AuthResult.success(
        user: User.fromJson(data),
        requiresTwoFactor: data['requiresTwoFactor'] ?? false,
      );
    } on DioException catch (e) {
      return AuthResult.error(
        message: e.response?.data?['message'] ?? 'Giriş uğursuz oldu',
        statusCode: e.response?.statusCode,
      );
    } catch (e) {
      return AuthResult.error(message: 'Xəta baş verdi: $e');
    }
  }

  /// Login with Active Directory
  Future<AuthResult> loginWithAD({
    required String domain,
    required String username,
    required String password,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/auth/login/ad',
        data: {
          'domain': domain,
          'username': username,
          'password': password,
        },
      );

      final data = response.data;
      
      await _secureStorage.saveToken(data['token']);
      await _secureStorage.saveRefreshToken(data['refreshToken']);
      
      await _secureStorage.saveUserInfo(
        userId: data['userId'].toString(),
        userName: data['userName'],
        email: data['email'] ?? '',
        organizationCode: data['organizationCode'] ?? 'default',
      );

      return AuthResult.success(user: User.fromJson(data));
    } on DioException catch (e) {
      return AuthResult.error(
        message: e.response?.data?['message'] ?? 'AD giriş uğursuz oldu',
      );
    }
  }

  /// Verify 2FA code
  Future<AuthResult> verifyTwoFactor({
    required String code,
    required String tempToken,
  }) async {
    try {
      final response = await _apiClient.dio.post(
        '/auth/2fa/verify',
        data: {
          'code': code,
          'tempToken': tempToken,
        },
      );

      final data = response.data;
      
      await _secureStorage.saveToken(data['token']);
      await _secureStorage.saveRefreshToken(data['refreshToken']);

      return AuthResult.success(user: User.fromJson(data));
    } on DioException catch (e) {
      return AuthResult.error(
        message: e.response?.data?['message'] ?? '2FA təsdiqi uğursuz oldu',
      );
    }
  }

  /// Forgot password
  Future<bool> forgotPassword(String email) async {
    try {
      await _apiClient.dio.post(
        '/auth/forgot-password',
        data: {'email': email},
      );
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Reset password
  Future<bool> resetPassword({
    required String token,
    required String newPassword,
  }) async {
    try {
      await _apiClient.dio.post(
        '/auth/reset-password',
        data: {
          'token': token,
          'newPassword': newPassword,
        },
      );
      return true;
    } catch (e) {
      return false;
    }
  }

  /// Logout
  Future<void> logout() async {
    try {
      await _apiClient.dio.post('/auth/logout');
    } catch (e) {
      // Ignore error
    } finally {
      await _secureStorage.clearAll();
    }
  }

  /// Check if user is logged in
  Future<bool> isLoggedIn() async {
    return await _secureStorage.isLoggedIn();
  }

  /// Get current user info
  Future<User?> getCurrentUser() async {
    try {
      final userId = await _secureStorage.getUserId();
      final userName = await _secureStorage.getUserName();
      final email = await _secureStorage.getUserEmail();
      
      if (userId == null) return null;
      
      return User(
        id: int.parse(userId),
        userName: userName ?? '',
        email: email ?? '',
      );
    } catch (e) {
      return null;
    }
  }

  /// Refresh token
  Future<bool> refreshToken() async {
    try {
      final refreshToken = await _secureStorage.getRefreshToken();
      if (refreshToken == null) return false;

      final response = await _apiClient.dio.post(
        '/auth/refresh',
        data: {'refreshToken': refreshToken},
      );

      final data = response.data;
      await _secureStorage.saveToken(data['token']);
      await _secureStorage.saveRefreshToken(data['refreshToken']);
      
      return true;
    } catch (e) {
      await _secureStorage.clearAll();
      return false;
    }
  }
}

/// Auth Result
class AuthResult {
  final bool success;
  final User? user;
  final String? errorMessage;
  final int? statusCode;
  final bool requiresTwoFactor;
  final String? tempToken;

  AuthResult({
    required this.success,
    this.user,
    this.errorMessage,
    this.statusCode,
    this.requiresTwoFactor = false,
    this.tempToken,
  });

  factory AuthResult.success({
    required User user,
    bool requiresTwoFactor = false,
  }) {
    return AuthResult(
      success: true,
      user: user,
      requiresTwoFactor: requiresTwoFactor,
    );
  }

  factory AuthResult.error({
    required String message,
    int? statusCode,
  }) {
    return AuthResult(
      success: false,
      errorMessage: message,
      statusCode: statusCode,
    );
  }
}

/// User Model
class User {
  final int id;
  final String userName;
  final String email;
  final String? displayName;
  final String? avatarUrl;
  final String? role;
  final String? organizationCode;

  User({
    required this.id,
    required this.userName,
    required this.email,
    this.displayName,
    this.avatarUrl,
    this.role,
    this.organizationCode,
  });

  factory User.fromJson(Map<String, dynamic> json) {
    return User(
      id: json['userId'] ?? json['id'],
      userName: json['userName'] ?? json['username'],
      email: json['email'] ?? '',
      displayName: json['displayName'],
      avatarUrl: json['avatarUrl'],
      role: json['role'],
      organizationCode: json['organizationCode'],
    );
  }

  String get initials {
    if (displayName != null && displayName!.isNotEmpty) {
      final parts = displayName!.split(' ');
      if (parts.length > 1) {
        return '${parts[0][0]}${parts[1][0]}'.toUpperCase();
      }
      return displayName!.substring(0, 1).toUpperCase();
    }
    return userName.substring(0, 1).toUpperCase();
  }
}
