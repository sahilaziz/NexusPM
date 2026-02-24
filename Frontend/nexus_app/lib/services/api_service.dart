import 'package:dio/dio.dart';
import 'package:retrofit/retrofit.dart';
import '../models/document_node.dart';

part 'api_service.g.dart';

@RestApi(baseUrl: "http://localhost:5000/api/v1/")
abstract class ApiService {
  factory ApiService(Dio dio, {String baseUrl}) = _ApiService;

  // Documents
  @POST("documents/create-with-path")
  Future<DocumentNode> createDocumentWithPath(
    @Body() CreateDocumentRequest request,
  );

  @POST("documents/create-internal-project")
  Future<DocumentNode> createInternalProject(
    @Body() CreateInternalProjectRequest request,
  );

  @POST("documents/create-incoming-letter")
  Future<DocumentNode> createIncomingLetter(
    @Body() CreateIncomingLetterRequest request,
  );

  @GET("documents/tree")
  Future<List<DocumentNode>> getDocumentTree(
    @Query("parentId") int parentId,
  );

  @GET("documents/search")
  Future<List<DocumentNode>> searchDocuments(
    @Queries() SearchRequest request,
  );

  @GET("documents/search-by-number")
  Future<List<DocumentNode>> searchByNumber(
    @Query("number") String number,
  );

  @GET("documents/check-document-number")
  Future<DocumentNumberCheckResult> checkDocumentNumber(
    @Query("number") String number,
  );

  @GET("documents/{id}")
  Future<DocumentNode> getDocument(@Path("id") int id);

  // Auth
  @POST("auth/login")
  Future<LoginResponse> login(@Body() LoginRequest request);

  @GET("auth/me")
  Future<UserInfo> getCurrentUser();
}

class ApiClient {
  static final ApiClient _instance = ApiClient._internal();
  late final ApiService apiService;
  late final Dio dio;
  String? _authToken;

  factory ApiClient() => _instance;

  ApiClient._internal() {
    dio = Dio(BaseOptions(
      baseUrl: 'http://localhost:5000/api/v1/',
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ));

    // Auth interceptor
    dio.interceptors.add(InterceptorsWrapper(
      onRequest: (options, handler) {
        if (_authToken != null) {
          options.headers['Authorization'] = 'Bearer $_authToken';
        }
        return handler.next(options);
      },
    ));

    // Logging interceptor
    dio.interceptors.add(LogInterceptor(
      requestBody: true,
      responseBody: true,
    ));

    apiService = ApiService(dio);
  }

  void setAuthToken(String token) {
    _authToken = token;
  }

  void clearAuthToken() {
    _authToken = null;
  }

  Future<bool> checkConnectivity() async {
    try {
      await dio.get('documents/tree?parentId=1');
      return true;
    } catch (e) {
      return false;
    }
  }
}

// Auth DTOs
@freezed
class LoginRequest with _$LoginRequest {
  const factory LoginRequest({
    required String username,
    required String password,
  }) = _LoginRequest;

  factory LoginRequest.fromJson(Map<String, dynamic> json) =>
      _$LoginRequestFromJson(json);
}

@freezed
class LoginResponse with _$LoginResponse {
  const factory LoginResponse({
    required bool success,
    required String token,
    required int expiresIn,
    required UserInfo user,
  }) = _LoginResponse;

  factory LoginResponse.fromJson(Map<String, dynamic> json) =>
      _$LoginResponseFromJson(json);
}

@freezed
class UserInfo with _$UserInfo {
  const factory UserInfo({
    required int id,
    required String username,
    required String fullName,
    required String email,
    required String role,
    required String organizationCode,
  }) = _UserInfo;

  factory UserInfo.fromJson(Map<String, dynamic> json) =>
      _$UserInfoFromJson(json);
}
