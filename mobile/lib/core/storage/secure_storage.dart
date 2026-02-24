import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Secure Storage Service for sensitive data
class SecureStorage {
  static const _storage = FlutterSecureStorage(
    aOptions: AndroidOptions(
      encryptedSharedPreferences: true,
    ),
    iOptions: IOSOptions(
      accessibility: KeychainAccessibility.first_unlock,
    ),
  );

  // Keys
  static const String _tokenKey = 'auth_token';
  static const String _refreshTokenKey = 'refresh_token';
  static const String _userIdKey = 'user_id';
  static const String _userNameKey = 'user_name';
  static const String _userEmailKey = 'user_email';
  static const String _organizationCodeKey = 'org_code';

  // Token
  Future<void> saveToken(String token) async {
    await _storage.write(key: _tokenKey, value: token);
  }

  Future<String?> getToken() async {
    return await _storage.read(key: _tokenKey);
  }

  Future<void> clearToken() async {
    await _storage.delete(key: _tokenKey);
  }

  // Refresh Token
  Future<void> saveRefreshToken(String token) async {
    await _storage.write(key: _refreshTokenKey, value: token);
  }

  Future<String?> getRefreshToken() async {
    return await _storage.read(key: _refreshTokenKey);
  }

  // User Info
  Future<void> saveUserInfo({
    required String userId,
    required String userName,
    required String email,
    required String organizationCode,
  }) async {
    await _storage.write(key: _userIdKey, value: userId);
    await _storage.write(key: _userNameKey, value: userName);
    await _storage.write(key: _userEmailKey, value: email);
    await _storage.write(key: _organizationCodeKey, value: organizationCode);
  }

  Future<String?> getUserId() async {
    return await _storage.read(key: _userIdKey);
  }

  Future<String?> getUserName() async {
    return await _storage.read(key: _userNameKey);
  }

  Future<String?> getUserEmail() async {
    return await _storage.read(key: _userEmailKey);
  }

  Future<String?> getOrganizationCode() async {
    return await _storage.read(key: _organizationCodeKey);
  }

  // Clear all
  Future<void> clearAll() async {
    await _storage.deleteAll();
  }

  // Check if logged in
  Future<bool> isLoggedIn() async {
    final token = await getToken();
    return token != null && token.isNotEmpty;
  }
}
