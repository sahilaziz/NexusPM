import 'package:flutter/foundation.dart';
import '../services/sync_service.dart';

class SyncProvider extends ChangeNotifier {
  final SyncService _syncService;
  
  SyncProvider(this._syncService) {
    _syncService.syncStatus.listen((status) {
      _isOnline = status.isOnline;
      _pendingCount = status.pendingCount;
      _isSyncing = status.isSyncing;
      notifyListeners();
    });
  }

  bool _isOnline = false;
  bool get isOnline => _isOnline;
  
  int _pendingCount = 0;
  int get pendingCount => _pendingCount;
  
  bool _isSyncing = false;
  bool get isSyncing => _isSyncing;

  String get connectionStatus => _isOnline ? 'Onlayn' : 'Oflayn';
  
  Color get statusColor => _isOnline 
    ? const Color(0xFF107C10) // Green
    : const Color(0xFFFFA500); // Orange
}
