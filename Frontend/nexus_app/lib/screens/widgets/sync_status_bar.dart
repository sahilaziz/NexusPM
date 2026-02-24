import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';
import '../../providers/sync_provider.dart';

class SyncStatusBar extends StatelessWidget {
  const SyncStatusBar({super.key});

  @override
  Widget build(BuildContext context) {
    return Consumer<SyncProvider>(
      builder: (context, sync, child) {
        return Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
          decoration: BoxDecoration(
            color: sync.statusColor.withOpacity(0.1),
            borderRadius: BorderRadius.circular(4),
            border: Border.all(color: sync.statusColor.withOpacity(0.3)),
          ),
          child: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              Container(
                width: 8,
                height: 8,
                decoration: BoxDecoration(
                  color: sync.statusColor,
                  shape: BoxShape.circle,
                ),
              ),
              const SizedBox(width: 8),
              Text(
                sync.connectionStatus,
                style: TextStyle(
                  color: sync.statusColor,
                  fontSize: 12,
                  fontWeight: FontWeight.w500,
                ),
              ),
              if (sync.pendingCount > 0) ...[
                const SizedBox(width: 12),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: Colors.orange,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    '${sync.pendingCount} gözləyir',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.w500,
                    ),
                  ),
                ),
              ],
              if (sync.isSyncing) ...[
                const SizedBox(width: 8),
                const SizedBox(
                  width: 12,
                  height: 12,
                  child: ProgressRing(strokeWidth: 2),
                ),
              ],
            ],
          ),
        );
      },
    );
  }
}
