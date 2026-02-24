import 'package:freezed_annotation/freezed_annotation.dart';
import 'package:hive/hive.dart';

part 'document_node.freezed.dart';
part 'document_node.g.dart';

@HiveType(typeId: 1)
enum NodeType {
  @HiveField(0)
  root,
  @HiveField(1)
  idare,
  @HiveField(2)
  quyu,
  @HiveField(3)
  menteqe,
  @HiveField(4)
  document,
}

@HiveType(typeId: 2)
enum DocumentSourceType {
  @HiveField(0)
  incomingLetter,   // Daxil olan məktub
  @HiveField(1)
  internalProject,  // Daxili layihə
  @HiveField(2)
  externalDocument, // Xarici sənəd
}

@freezed
class DocumentNode with _$DocumentNode {
  @HiveType(typeId: 0, adapterName: 'DocumentNodeAdapter')
  const factory DocumentNode({
    @HiveField(0) required int nodeId,
    @HiveField(1) int? parentNodeId,
    @HiveField(2) required NodeType nodeType,
    @HiveField(3) required String entityCode,
    @HiveField(4) required String entityName,
    @HiveField(5) String? materializedPath,
    @HiveField(6) @Default(0) int depth,
    @HiveField(7) DateTime? documentDate,
    @HiveField(8) String? documentNumber,              // Original: 1-4-8\\3-2-1243\\2026
    @HiveField(9) String? normalizedDocumentNumber,   // Search: 1 4 8 3 2 1243 2026
    @HiveField(10) String? externalDocumentNumber,    // Xarici nömrə (əgər varsa)
    @HiveField(11) DocumentSourceType? sourceType,     // Daxili/Xarici/Layihə
    @HiveField(12) DateTime? createdAt,
    @HiveField(13) @Default(false) bool isSynced,
    @HiveField(14) @Default(false) bool isPending,
  }) = _DocumentNode;

  factory DocumentNode.fromJson(Map<String, dynamic> json) =>
      _$DocumentNodeFromJson(json);
}

@freezed
class CreateDocumentRequest with _$CreateDocumentRequest {
  const factory CreateDocumentRequest({
    required String idareCode,
    required String idareName,
    required String quyuCode,
    required String quyuName,
    required String menteqeCode,
    required String menteqeName,
    required DateTime documentDate,
    required String documentSubject,
    String? createdBy,
    @Default(DocumentSourceType.incomingLetter) DocumentSourceType sourceType,
    String? externalDocumentNumber,  // Daxil olan məktub nömrəsi
  }) = _CreateDocumentRequest;

  factory CreateDocumentRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateDocumentRequestFromJson(json);
}

@freezed
class CreateIncomingLetterRequest with _$CreateIncomingLetterRequest {
  const factory CreateIncomingLetterRequest({
    required String idareCode,
    required String idareName,
    required String quyuCode,
    required String quyuName,
    required String menteqeCode,
    required String menteqeName,
    required DateTime documentDate,
    required String documentNumber,  // Məs: 1-4-8\\3-2-1243\\2026
    required String subject,
  }) = _CreateIncomingLetterRequest;

  factory CreateIncomingLetterRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateIncomingLetterRequestFromJson(json);
}

@freezed
class CreateInternalProjectRequest with _$CreateInternalProjectRequest {
  const factory CreateInternalProjectRequest({
    required String idareCode,
    required String idareName,
    required String quyuCode,
    required String quyuName,
    required String menteqeCode,
    required String menteqeName,
    required DateTime documentDate,
    required String projectName,
  }) = _CreateInternalProjectRequest;

  factory CreateInternalProjectRequest.fromJson(Map<String, dynamic> json) =>
      _$CreateInternalProjectRequestFromJson(json);
}

@freezed
class SearchRequest with _$SearchRequest {
  const factory SearchRequest({
    String? searchTerm,
    String? idareCode,
    DateTime? dateFrom,
    DateTime? dateTo,
  }) = _SearchRequest;

  factory SearchRequest.fromJson(Map<String, dynamic> json) =>
      _$SearchRequestFromJson(json);
}

@freezed
class DocumentNumberCheckResult with _$DocumentNumberCheckResult {
  const factory DocumentNumberCheckResult({
    required bool isUnique,
    required String original,
    required String normalized,
    required String message,
  }) = _DocumentNumberCheckResult;

  factory DocumentNumberCheckResult.fromJson(Map<String, dynamic> json) =>
      _$DocumentNumberCheckResultFromJson(json);
}

extension NodeTypeExtension on NodeType {
  String get displayName {
    switch (this) {
      case NodeType.root:
        return 'Kök';
      case NodeType.idare:
        return 'İdarə';
      case NodeType.quyu:
        return 'Quyu';
      case NodeType.menteqe:
        return 'Məntəqə';
      case NodeType.document:
        return 'Sənəd';
    }
  }

  String get icon {
    switch (this) {
      case NodeType.root:
        return '🏢';
      case NodeType.idare:
        return '🏭';
      case NodeType.quyu:
        return '🛢️';
      case NodeType.menteqe:
        return '📍';
      case NodeType.document:
        return '📄';
    }
  }
}

extension DocumentSourceTypeExtension on DocumentSourceType {
  String get displayName {
    switch (this) {
      case DocumentSourceType.incomingLetter:
        return 'Daxil olan məktub';
      case DocumentSourceType.internalProject:
        return 'Daxili layihə';
      case DocumentSourceType.externalDocument:
        return 'Xarici sənəd';
    }
  }

  String get icon {
    switch (this) {
      case DocumentSourceType.incomingLetter:
        return '📨';
      case DocumentSourceType.internalProject:
        return '📁';
      case DocumentSourceType.externalDocument:
        return '📎';
    }
  }
}

/// Sənəd nömrəsini normalize et (axtarış üçün)
String normalizeDocumentNumber(String documentNumber) {
  if (documentNumber.isEmpty) return '';
  
  // Böyük hərfə çevir
  var normalized = documentNumber.toUpperCase();
  
  // Xüsusi simvolları boşluqla əvəz et
  normalized = normalized.replaceAll(RegExp(r'[^\w\d]'), ' ');
  
  // Çoxlu boşluqları təkə endir
  normalized = normalized.replaceAll(RegExp(r'\s+'), ' ').trim();
  
  return normalized;
}

/// Sənəd nömrəsinin formatını yoxla
bool isValidDocumentNumber(String documentNumber) {
  // Minimum uzunluq
  if (documentNumber.length < 3) return false;
  
  // Ən azı bir rəqəm olmalıdır
  if (!documentNumber.contains(RegExp(r'\d'))) return false;
  
  return true;
}
