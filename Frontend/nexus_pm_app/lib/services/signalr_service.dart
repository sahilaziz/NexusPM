import 'dart:async';
import 'package:signalr_core/signalr_core.dart';
import 'package:flutter/foundation.dart';

/// Real-time SignalR service for notifications and task updates
class SignalRService {
  static final SignalRService _instance = SignalRService._internal();
  factory SignalRService() => _instance;
  SignalRService._internal();

  HubConnection? _connection;
  final _notificationController = StreamController<NotificationDto>.broadcast();
  final _taskUpdateController = StreamController<TaskUpdateDto>.broadcast();
  final _documentController = StreamController<DocumentNotificationDto>.broadcast();
  final _connectionController = StreamController<bool>.broadcast();

  // Streams
  Stream<NotificationDto> get notificationStream => _notificationController.stream;
  Stream<TaskUpdateDto> get taskUpdateStream => _taskUpdateController.stream;
  Stream<DocumentNotificationDto> get documentStream => _documentController.stream;
  Stream<bool> get connectionStream => _connectionController.stream;

  bool get isConnected => _connection?.state == HubConnectionState.connected;
  String? _userId;
  String? _organizationCode;

  /// Initialize SignalR connection
  Future<void> initialize({
    required String serverUrl,
    required String userId,
    required String organizationCode,
    required String authToken,
  }) async {
    _userId = userId;
    _organizationCode = organizationCode;

    _connection = HubConnectionBuilder()
        .withUrl(
          '$serverUrl/hubs/notifications',
          HttpConnectionOptions(
            accessTokenFactory: () async => authToken,
            transport: HttpTransportType.webSockets,
            logging: (level, message) {
              if (kDebugMode) {
                print('SignalR [$level]: $message');
              }
            },
          ),
        )
        .withAutomaticReconnect(
          retryDelays: [2000, 5000, 10000, 30000],
        )
        .build();

    _registerHandlers();

    _connection!.onreconnecting(({error}) {
      debugPrint('SignalR reconnecting... Error: $error');
      _connectionController.add(false);
    });

    _connection!.onreconnected(({connectionId}) {
      debugPrint('SignalR reconnected: $connectionId');
      _connectionController.add(true);
      _joinUserGroup();
    });

    _connection!.onclose(({error}) {
      debugPrint('SignalR closed: $error');
      _connectionController.add(false);
    });

    await startConnection();
  }

  Future<void> startConnection() async {
    if (_connection == null) return;

    try {
      await _connection!.start();
      debugPrint('SignalR connected!');
      _connectionController.add(true);
      await _joinUserGroup();
    } catch (e) {
      debugPrint('SignalR connection error: $e');
      _connectionController.add(false);
    }
  }

  Future<void> _joinUserGroup() async {
    if (_connection?.state == HubConnectionState.connected) {
      try {
        await _connection!.invoke('JoinUserGroup', args: [_userId, _organizationCode]);
        debugPrint('Joined user group: $_userId');
      } catch (e) {
        debugPrint('Error joining user group: $e');
      }
    }
  }

  void _registerHandlers() {
    _connection!.on('NewNotification', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        final notification = NotificationDto.fromJson(data);
        _notificationController.add(notification);
        _showLocalNotification(notification);
      }
    });

    _connection!.on('TaskAssigned', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        final notification = NotificationDto.fromJson(data);
        _notificationController.add(notification);
      }
    });

    _connection!.on('TaskUpdated', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        final update = TaskUpdateDto.fromJson(data);
        _taskUpdateController.add(update);
      }
    });

    _connection!.on('RefreshTaskList', (arguments) {
      _notificationController.add(NotificationDto(
        type: 'RefreshTaskList',
        title: 'Task List Updated',
        message: 'Yeni tapşırıq əlavə edildi',
      ));
    });

    _connection!.on('DocumentUploaded', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        final doc = DocumentNotificationDto.fromJson(data);
        _documentController.add(doc);
      }
    });

    _connection!.on('DeadlineReminder', (arguments) {
      if (arguments != null && arguments.isNotEmpty) {
        final data = arguments[0] as Map<String, dynamic>;
        _notificationController.add(NotificationDto(
          type: 'DeadlineReminder',
          title: 'Deadline Xatırlatması',
          message: '${data['taskTitle']} tapşırığı üçün ${data['hoursRemaining']} saat qaldı',
          entityId: data['taskId'],
          entityType: 'Task',
        ));
      }
    });
  }

  void _showLocalNotification(NotificationDto notification) {
    debugPrint('NOTIFICATION: ${notification.title} - ${notification.message}');
  }

  Future<void> markAsRead(int notificationId) async {
    if (_connection?.state == HubConnectionState.connected) {
      await _connection!.invoke('MarkAsRead', args: [notificationId]);
    }
  }

  Future<void> stop() async {
    await _connection?.stop();
    _connectionController.add(false);
  }

  void dispose() {
    _notificationController.close();
    _taskUpdateController.close();
    _documentController.close();
    _connectionController.close();
    _connection?.stop();
  }
}

class NotificationDto {
  final int? notificationId;
  final String type;
  final String title;
  final String message;
  final String? entityType;
  final int? entityId;
  final String? metadata;
  final String? senderUserId;
  final DateTime createdAt;
  final int badge;

  NotificationDto({
    this.notificationId,
    required this.type,
    required this.title,
    required this.message,
    this.entityType,
    this.entityId,
    this.metadata,
    this.senderUserId,
    DateTime? createdAt,
    this.badge = 0,
  }) : createdAt = createdAt ?? DateTime.now();

  factory NotificationDto.fromJson(Map<String, dynamic> json) {
    return NotificationDto(
      notificationId: json['notificationId'],
      type: json['type'] ?? '',
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      entityType: json['entityType'],
      entityId: json['entityId'],
      metadata: json['metadata'],
      senderUserId: json['senderUserId'],
      createdAt: json['createdAt'] != null 
          ? DateTime.parse(json['createdAt']) 
          : DateTime.now(),
      badge: json['badge'] ?? 0,
    );
  }
}

class TaskUpdateDto {
  final int taskId;
  final String status;
  final String oldStatus;
  final String updatedBy;
  final DateTime updatedAt;
  final String message;

  TaskUpdateDto({
    required this.taskId,
    required this.status,
    required this.oldStatus,
    required this.updatedBy,
    required this.updatedAt,
    required this.message,
  });

  factory TaskUpdateDto.fromJson(Map<String, dynamic> json) {
    return TaskUpdateDto(
      taskId: json['taskId'],
      status: json['status'],
      oldStatus: json['oldStatus'],
      updatedBy: json['updatedBy'],
      updatedAt: DateTime.parse(json['updatedAt']),
      message: json['message'],
    );
  }
}

class DocumentNotificationDto {
  final int documentId;
  final String? documentNumber;
  final String documentSubject;
  final String materializedPath;
  final String uploadedBy;
  final DateTime uploadedAt;

  DocumentNotificationDto({
    required this.documentId,
    this.documentNumber,
    required this.documentSubject,
    required this.materializedPath,
    required this.uploadedBy,
    required this.uploadedAt,
  });

  factory DocumentNotificationDto.fromJson(Map<String, dynamic> json) {
    return DocumentNotificationDto(
      documentId: json['documentId'],
      documentNumber: json['documentNumber'],
      documentSubject: json['documentSubject'],
      materializedPath: json['materializedPath'],
      uploadedBy: json['uploadedBy'],
      uploadedAt: DateTime.parse(json['uploadedAt']),
    );
  }
}
