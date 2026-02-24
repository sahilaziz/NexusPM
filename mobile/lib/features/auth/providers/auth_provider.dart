import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/api_client.dart';
import '../../../core/auth/auth_service.dart';
import '../../../core/storage/secure_storage.dart';

// Providers
final secureStorageProvider = Provider<SecureStorage>((ref) {
  return SecureStorage();
});

final apiClientProvider = Provider<ApiClient>((ref) {
  final storage = ref.watch(secureStorageProvider);
  return ApiClient(storage);
});

final authServiceProvider = Provider<AuthService>((ref) {
  final apiClient = ref.watch(apiClientProvider);
  final storage = ref.watch(secureStorageProvider);
  return AuthService(apiClient, storage);
});

// Auth State
class AuthState {
  final bool isLoading;
  final bool isAuthenticated;
  final User? user;
  final String? error;
  final bool requiresTwoFactor;

  AuthState({
    this.isLoading = false,
    this.isAuthenticated = false,
    this.user,
    this.error,
    this.requiresTwoFactor = false,
  });

  AuthState copyWith({
    bool? isLoading,
    bool? isAuthenticated,
    User? user,
    String? error,
    bool? requiresTwoFactor,
  }) {
    return AuthState(
      isLoading: isLoading ?? this.isLoading,
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      user: user ?? this.user,
      error: error ?? this.error,
      requiresTwoFactor: requiresTwoFactor ?? this.requiresTwoFactor,
    );
  }
}

// Auth Notifier
class AuthNotifier extends StateNotifier<AuthState> {
  final AuthService _authService;

  AuthNotifier(this._authService) : super(AuthState()) {
    checkAuthStatus();
  }

  Future<void> checkAuthStatus() async {
    state = state.copyWith(isLoading: true);
    
    final isLoggedIn = await _authService.isLoggedIn();
    if (isLoggedIn) {
      final user = await _authService.getCurrentUser();
      state = AuthState(
        isLoading: false,
        isAuthenticated: true,
        user: user,
      );
    } else {
      state = AuthState(isLoading: false);
    }
  }

  Future<void> login({
    required String username,
    required String password,
    String? twoFactorCode,
  }) async {
    state = state.copyWith(isLoading: true, error: null);

    final result = await _authService.login(
      username: username,
      password: password,
      twoFactorCode: twoFactorCode,
    );

    if (result.success) {
      if (result.requiresTwoFactor) {
        state = state.copyWith(
          isLoading: false,
          requiresTwoFactor: true,
        );
      } else {
        state = AuthState(
          isLoading: false,
          isAuthenticated: true,
          user: result.user,
        );
      }
    } else {
      state = state.copyWith(
        isLoading: false,
        error: result.errorMessage,
      );
    }
  }

  Future<void> loginWithAD({
    required String domain,
    required String username,
    required String password,
  }) async {
    state = state.copyWith(isLoading: true, error: null);

    final result = await _authService.loginWithAD(
      domain: domain,
      username: username,
      password: password,
    );

    if (result.success) {
      state = AuthState(
        isLoading: false,
        isAuthenticated: true,
        user: result.user,
      );
    } else {
      state = state.copyWith(
        isLoading: false,
        error: result.errorMessage,
      );
    }
  }

  Future<void> logout() async {
    state = state.copyWith(isLoading: true);
    
    await _authService.logout();
    
    state = AuthState(isLoading: false);
  }

  void clearError() {
    state = state.copyWith(error: null);
  }
}

// Auth State Provider
final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  final authService = ref.watch(authServiceProvider);
  return AuthNotifier(authService);
});

// Current User Provider
final currentUserProvider = Provider<User?>((ref) {
  return ref.watch(authProvider).user;
});

// Is Authenticated Provider
final isAuthenticatedProvider = Provider<bool>((ref) {
  return ref.watch(authProvider).isAuthenticated;
});

// Auth Loading Provider
final authLoadingProvider = Provider<bool>((ref) {
  return ref.watch(authProvider).isLoading;
});

// Auth Error Provider
final authErrorProvider = Provider<String?>((ref) {
  return ref.watch(authProvider).error;
});
