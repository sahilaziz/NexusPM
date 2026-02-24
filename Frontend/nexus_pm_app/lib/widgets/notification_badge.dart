import 'package:flutter/material.dart';
import '../services/signalr_service.dart';

class NotificationBadge extends StatefulWidget {
  final VoidCallback? onTap;
  const NotificationBadge({Key? key, this.onTap}) : super(key: key);

  @override
  State<NotificationBadge> createState() => _NotificationBadgeState();
}

class _NotificationBadgeState extends State<NotificationBadge> {
  int _unreadCount = 0;
  final SignalRService _signalR = SignalRService();

  @override
  void initState() {
    super.initState();
    _listenToNotifications();
  }

  void _listenToNotifications() {
    _signalR.notificationStream.listen((notification) {
      if (notification.badge > 0) {
        setState(() => _unreadCount++);
        _showInAppNotification(notification);
      }
    });
  }

  void _showInAppNotification(NotificationDto notification) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(notification.title, style: const TextStyle(fontWeight: FontWeight.bold)),
            Text(notification.message),
          ],
        ),
        backgroundColor: _getNotificationColor(notification.type),
        duration: const Duration(seconds: 5),
        action: SnackBarAction(
          label: 'BAX',
          textColor: Colors.white,
          onPressed: () => _handleNotificationTap(notification),
        ),
      ),
    );
  }

  Color _getNotificationColor(String type) {
    switch (type) {
      case 'TaskAssigned': return Colors.blue;
      case 'TaskStatusChanged': return Colors.orange;
      case 'DeadlineReminder': return Colors.red;
      case 'DocumentUploaded': return Colors.green;
      default: return Colors.grey.shade800;
    }
  }

  void _handleNotificationTap(NotificationDto notification) {
    if (notification.entityType == 'Task' && notification.entityId != null) {
      Navigator.pushNamed(context, '/task/${notification.entityId}');
    }
  }

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: widget.onTap,
      child: Stack(
        clipBehavior: Clip.none,
        children: [
          const Icon(Icons.notifications_outlined, size: 28),
          if (_unreadCount > 0)
            Positioned(
              right: -2,
              top: -2,
              child: Container(
                padding: const EdgeInsets.all(4),
                decoration: const BoxDecoration(color: Colors.red, shape: BoxShape.circle),
                constraints: const BoxConstraints(minWidth: 18, minHeight: 18),
                child: Center(
                  child: Text(
                    _unreadCount > 99 ? '99+' : '$_unreadCount',
                    style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.bold),
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

class RealtimeTaskList extends StatefulWidget {
  final List<dynamic> initialTasks;
  final Function(dynamic) onTaskTap;
  const RealtimeTaskList({Key? key, required this.initialTasks, required this.onTaskTap}) : super(key: key);

  @override
  State<RealtimeTaskList> createState() => _RealtimeTaskListState();
}

class _RealtimeTaskListState extends State<RealtimeTaskList> {
  late List<dynamic> _tasks;
  final SignalRService _signalR = SignalRService();

  @override
  void initState() {
    super.initState();
    _tasks = List.from(widget.initialTasks);
    _listenToUpdates();
  }

  void _listenToUpdates() {
    _signalR.notificationStream.listen((notification) {
      if (notification.type == 'TaskAssigned' || notification.type == 'RefreshTaskList') {
        _refreshTasks();
      }
    });

    _signalR.taskUpdateStream.listen((update) {
      setState(() {
        final index = _tasks.indexWhere((t) => t['taskId'] == update.taskId);
        if (index != -1) _tasks[index]['status'] = update.status;
      });
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text(update.message), duration: const Duration(seconds: 3)),
      );
    });
  }

  void _refreshTasks() => setState(() {});

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      itemCount: _tasks.length,
      itemBuilder: (context, index) {
        final task = _tasks[index];
        return ListTile(
          leading: _getStatusIcon(task['status']),
          title: Text(task['taskTitle'] ?? 'Naməlum'),
          subtitle: Text('Status: ${task['status']}'),
          trailing: Text(_formatDate(task['dueDate']), 
            style: TextStyle(color: _isOverdue(task['dueDate']) ? Colors.red : Colors.grey)),
          onTap: () => widget.onTaskTap(task),
        );
      },
    );
  }

  Widget _getStatusIcon(String status) {
    IconData icon;
    Color color;
    switch (status) {
      case 'Todo': icon = Icons.circle_outlined; color = Colors.grey; break;
      case 'InProgress': icon = Icons.play_circle_outline; color = Colors.blue; break;
      case 'Review': icon = Icons.rate_review_outlined; color = Colors.orange; break;
      case 'Done': icon = Icons.check_circle; color = Colors.green; break;
      default: icon = Icons.help_outline; color = Colors.grey;
    }
    return Icon(icon, color: color);
  }

  String _formatDate(String? dateStr) {
    if (dateStr == null) return '-';
    final date = DateTime.parse(dateStr);
    return '${date.day}.${date.month}.${date.year}';
  }

  bool _isOverdue(String? dateStr) {
    if (dateStr == null) return false;
    return DateTime.parse(dateStr).isBefore(DateTime.now());
  }
}

class ConnectionStatusIndicator extends StatelessWidget {
  const ConnectionStatusIndicator({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    final signalR = SignalRService();
    return StreamBuilder<bool>(
      stream: signalR.connectionStream,
      initialData: signalR.isConnected,
      builder: (context, snapshot) {
        final isConnected = snapshot.data ?? false;
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: isConnected ? Colors.green : Colors.red,
            borderRadius: BorderRadius.circular(12),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(isConnected ? Icons.cloud_done : Icons.cloud_off, size: 16, color: Colors.white),
              const SizedBox(width: 4),
              Text(isConnected ? 'Online' : 'Offline', style: const TextStyle(color: Colors.white, fontSize: 12)),
            ],
          ),
        );
      },
    );
  }
}
