import 'package:flutter/foundation.dart';
import '../models/document_node.dart';
import '../services/api_service.dart';

class DocumentProvider extends ChangeNotifier {
  final ApiClient _apiClient = ApiClient();
  
  List<DocumentNode> _documents = [];
  List<DocumentNode> get documents => _documents;
  
  DocumentNode? _selectedNode;
  DocumentNode? get selectedNode => _selectedNode;
  
  bool _isLoading = false;
  bool get isLoading => _isLoading;
  
  String? _error;
  String? get error => _error;

  // Tree structure
  Map<int, List<DocumentNode>> _childrenMap = {};
  Map<int, List<DocumentNode>> get childrenMap => _childrenMap;

  Future<void> loadTree(int parentId) async {
    _isLoading = true;
    _error = null;
    notifyListeners();
    
    try {
      final docs = await _apiClient.apiService.getDocumentTree(parentId);
      _childrenMap[parentId] = docs;
      
      // Build flat list for display
      if (parentId == 1) {
        _documents = docs;
      }
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  Future<void> loadChildren(int parentId) async {
    if (_childrenMap.containsKey(parentId)) return;
    
    try {
      final children = await _apiClient.apiService.getDocumentTree(parentId);
      _childrenMap[parentId] = children;
      notifyListeners();
    } catch (e) {
      _error = e.toString();
      notifyListeners();
    }
  }

  Future<void> searchDocuments(SearchRequest request) async {
    _isLoading = true;
    notifyListeners();
    
    try {
      final results = await _apiClient.apiService.searchDocuments(request);
      _documents = results;
    } catch (e) {
      _error = e.toString();
    } finally {
      _isLoading = false;
      notifyListeners();
    }
  }

  void selectNode(DocumentNode node) {
    _selectedNode = node;
    notifyListeners();
  }

  Future<void> refresh() async {
    _childrenMap.clear();
    await loadTree(1);
  }

  // Helper: Build breadcrumb path
  List<DocumentNode> getBreadcrumb(int nodeId) {
    // Find node in all loaded children
    for (final entry in _childrenMap.entries) {
      final found = entry.value.where((d) => d.nodeId == nodeId).firstOrNull;
      if (found != null) {
        // TODO: Build full path from materializedPath
        return [found];
      }
    }
    return [];
  }
}
