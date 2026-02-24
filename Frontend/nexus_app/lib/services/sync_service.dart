import 'dart:async';
import 'dart:convert';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:hive/hive.dart';
import 'package:signalr_netcore/signalr_client.dart';
import 'package:logger/logger.dart';

import '../models/document_node.dart';
import 'api_service.dart';

class SyncService {
  final Logger _logger = Logger();
  final ApiClient _apiClient = ApiClient();
  late final HubConnection _hubConnection;
  late Box<DocumentNode> _pendingBox;
  late Box<DocumentNode> _documentBox;
  
  final _syncStatusController = StreamController<SyncStatus>.broadcast();
  Stream<SyncStatus> get syncStatus => _syncStatusController.stream;
  
  bool _isOnline = false;
  bool get isOnline => _isOnline;
  
  Timer? _syncTimer;

  Future<void> initialize() async {
    // Initialize Hive boxes
    _pendingBox = await Hive.openBox<DocumentNode>('pending_documents');
    _documentBox = await Hive.openBox<DocumentNode>('documents');
    
    // Setup SignalR
    _hubConnection = HubConnectionBuilder()
        .withUrl('http://localhost:5000/hubs/sync')
        .withAutomaticReconnect()
        .build();
    
    _hubConnection.on('DocumentCreated', _handleDocumentCreated);
    _hubConnection.on('DocumentUpdated', _handleDocumentUpdated);
    
    // Start connectivity monitoring
    Connectivity().onConnectivityChanged.listen(_onConnectivityChanged);
    _checkConnectivity();
    
    // Start periodic sync
    _syncTimer = Timer.periodic(const Duration(minutes: 1), (_) => _syncPending());
  }

  Future<void> _checkConnectivity() async {
    final result = await Connectivity().checkConnectivity();
    await _onConnectivityChanged(result);
  }

  Future<void> _onConnectivityChanged(ConnectivityResult result) async {
    final wasOffline = !_isOnline;
    _isOnline = result != ConnectivityResult.none;
    
    if (_isOnline) {
      // Try to connect to server
      try {
        await _hubConnection.start();
        _logger.i('SignalR connected');
      } catch (e) {
        _logger.w('SignalR connection failed: $e');
      }
      
      // Sync pending documents
      if (wasOffline || _pendingBox.isNotEmpty) {
        await _syncPending();
      }
    } else {
      await _hubConnection.stop();
      _logger.i('SignalR disconnected - offline mode');
    }
    
    _syncStatusController.add(SyncStatus(
      isOnline: _isOnline,
      pendingCount: _pendingBox.length,
    ));
  }

  Future<void> queueDocument(CreateDocumentRequest request) async {
    final tempNode = DocumentNode(
      nodeId: DateTime.now().millisecondsSinceEpoch, // Temporary ID
      nodeType: NodeType.document,
      entityCode: request.documentNumber,
      entityName: '${request.documentDate} - Məktub №${request.documentNumber} - ${request.subject}.pdf',
      documentDate: request.documentDate,
      documentNumber: request.documentNumber,
      parentNodeId: null, // Will be set after path creation
      isPending: true,
      isSynced: false,
      createdAt: DateTime.now(),
    );
    
    await _pendingBox.put(tempNode.nodeId, tempNode);
    
    _syncStatusController.add(SyncStatus(
      isOnline: _isOnline,
      pendingCount: _pendingBox.length,
    ));
    
    // Try immediate sync if online
    if (_isOnline) {
      await _syncPending();
    }
  }

  Future<void> _syncPending() async {
    if (!_isOnline || _pendingBox.isEmpty) return;
    
    _syncStatusController.add(SyncStatus(
      isOnline: _isOnline,
      pendingCount: _pendingBox.length,
      isSyncing: true,
    ));
    
    final pending = _pendingBox.values.toList();
    
    for (final doc in pending) {
      try {
        // TODO: Parse request from pending doc and send to API
        // For now, just mark as synced
        await _pendingBox.delete(doc.nodeId);
        await _documentBox.put(doc.nodeId, doc.copyWith(isSynced: true, isPending: false));
      } catch (e) {
        _logger.e('Sync failed for doc ${doc.nodeId}: $e');
        break; // Stop syncing on error, will retry later
      }
    }
    
    _syncStatusController.add(SyncStatus(
      isOnline: _isOnline,
      pendingCount: _pendingBox.length,
      isSyncing: false,
    ));
  }

  void _handleDocumentCreated(List<Object?>? args) {
    // Handle real-time document creation from server
    _logger.i('Document created notification received');
  }

  void _handleDocumentUpdated(List<Object?>? args) {
    // Handle real-time document update from server
    _logger.i('Document updated notification received');
  }

  Future<List<DocumentNode>> getLocalDocuments() async {
    return _documentBox.values.toList();
  }

  Future<List<DocumentNode>> getPendingDocuments() async {
    return _pendingBox.values.toList();
  }

  void dispose() {
    _syncTimer?.cancel();
    _syncStatusController.close();
    _hubConnection.stop();
  }
}

class SyncStatus {
  final bool isOnline;
  final int pendingCount;
  final bool isSyncing;

  SyncStatus({
    required this.isOnline,
    required this.pendingCount,
    this.isSyncing = false,
  });
}
