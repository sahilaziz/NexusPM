import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/server_config_provider.dart';

/// Server Konfiqurasiya Screen
/// Admin Azure ↔ Öz sistemlər arasında switch edə bilər
class ServerConfigScreen extends ConsumerWidget {
  const ServerConfigScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final configAsync = ref.watch(serverConfigProvider);
    final statusAsync = ref.watch(serverStatusProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Server Konfiqurasiyası'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () {
              ref.invalidate(serverConfigProvider);
              ref.invalidate(serverStatusProvider);
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(serverConfigProvider);
          ref.invalidate(serverStatusProvider);
        },
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Header
              _buildHeader(context),
              const SizedBox(height: 24),

              // Status Card
              statusAsync.when(
                data: (status) => _buildStatusCard(context, status, ref),
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (err, _) => _buildErrorCard(context, err),
              ),
              const SizedBox(height: 24),

              // Messaging Config
              configAsync.when(
                data: (config) => _buildMessagingCard(context, config, ref),
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (err, _) => const SizedBox.shrink(),
              ),
              const SizedBox(height: 16),

              // Monitoring Config
              configAsync.when(
                data: (config) => _buildMonitoringCard(context, config, ref),
                loading: () => const Center(child: CircularProgressIndicator()),
                error: (err, _) => const SizedBox.shrink(),
              ),
              const SizedBox(height: 24),

              // Cost Info
              _buildCostCard(context),
              const SizedBox(height: 24),

              // Important Notes
              _buildImportantNotes(context),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildHeader(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [Colors.blue.shade700, Colors.blue.shade500],
        ),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              const Icon(Icons.cloud, color: Colors.white, size: 32),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Hybrid Infrastructure',
                      style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                            color: Colors.white,
                            fontWeight: FontWeight.bold,
                          ),
                    ),
                    Text(
                      'Öz sistemlərlə Azure arasında switch edin',
                      style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                            color: Colors.white70,
                          ),
                    ),
                  ],
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
            decoration: BoxDecoration(
              color: Colors.white.withOpacity(0.2),
              borderRadius: BorderRadius.circular(20),
            ),
            child: const Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                Icon(Icons.check_circle, color: Colors.white, size: 16),
                SizedBox(width: 6),
                Text(
                  'Default: Pulsuz (\$0/ay)',
                  style: TextStyle(color: Colors.white, fontSize: 12),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildStatusCard(BuildContext context, dynamic status, WidgetRef ref) {
    final messaging = status['messaging'];
    final monitoring = status['monitoring'];
    final costs = status['costs'];

    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.dns, color: Colors.blue.shade700),
                const SizedBox(width: 8),
                Text(
                  'Cari Status',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            
            // Messaging Status
            _buildStatusRow(
              'Messaging',
              messaging['currentMode'],
              messaging['isPrivate'] ? Icons.storage : Icons.cloud,
              messaging['isPrivate'] ? Colors.green : Colors.blue,
            ),
            const SizedBox(height: 8),
            
            // Monitoring Status
            _buildStatusRow(
              'Monitoring',
              monitoring['currentMode'],
              monitoring['isPrivate'] ? Icons.storage : Icons.cloud,
              monitoring['isPrivate'] ? Colors.green : Colors.blue,
            ),
            const Divider(),
            
            // Cost
            Row(
              children: [
                const Icon(Icons.attach_money, color: Colors.orange),
                const SizedBox(width: 8),
                const Text('Aylıq Xərc:'),
                const Spacer(),
                Text(
                  costs['current'],
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    color: costs['current'].toString().contains('0') 
                        ? Colors.green 
                        : Colors.orange,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildStatusRow(String label, String mode, IconData icon, Color color) {
    return Row(
      children: [
        Icon(icon, color: color, size: 20),
        const SizedBox(width: 8),
        Text('$label:'),
        const Spacer(),
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: color.withOpacity(0.1),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: color.withOpacity(0.3)),
          ),
          child: Text(
            mode == 'Private' ? 'Öz Sistem' : 'Azure',
            style: TextStyle(
              color: color,
              fontWeight: FontWeight.bold,
              fontSize: 12,
            ),
          ),
        ),
      ],
    );
  }

  Widget _buildMessagingCard(BuildContext context, dynamic config, WidgetRef ref) {
    final isPrivate = config['currentConfig']['messaging']['mode'] == 'Private';

    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.message, color: Colors.purple.shade700),
                const SizedBox(width: 8),
                Text(
                  'Messaging Sistemi',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const Spacer(),
                _buildModeChip(isPrivate),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            
            // Description
            Text(
              isPrivate
                  ? '📦 SQL Server Message Queue\n• Lokal database-də saxlanılır\n• 5000 user-ə qədər kifayət edir\n• Pulsuz'
                  : '☁️ Azure Service Bus\n• Cloud-based message queue\n• Unlimited scale\n• \$30/ay',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 16),
            
            // Switch Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: () => _showSwitchDialog(
                  context, 
                  'Messaging', 
                  isPrivate ? 'Azure' : 'Private',
                  ref,
                ),
                icon: Icon(isPrivate ? Icons.cloud_upload : Icons.storage),
                label: Text(isPrivate ? 'Azure-a Keç' : 'Öz Sistemə Qayıt'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: isPrivate ? Colors.blue : Colors.green,
                  foregroundColor: Colors.white,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildMonitoringCard(BuildContext context, dynamic config, WidgetRef ref) {
    final isPrivate = config['currentConfig']['monitoring']['mode'] == 'Private';

    return Card(
      elevation: 2,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.monitoring, color: Colors.orange.shade700),
                const SizedBox(width: 8),
                Text(
                  'Monitoring Sistemi',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
                const Spacer(),
                _buildModeChip(isPrivate),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            
            // Description
            Text(
              isPrivate
                  ? '📊 SQL Server Monitoring\n• Lokal database-də log-lar\n• 100GB-ə qədər kifayət edir\n• Pulsuz'
                  : '☁️ Azure Application Insights\n• Advanced analytics\n• AI-based insights\n• \$200/ay',
              style: Theme.of(context).textTheme.bodySmall,
            ),
            const SizedBox(height: 16),
            
            // Switch Button
            SizedBox(
              width: double.infinity,
              child: ElevatedButton.icon(
                onPressed: () => _showSwitchDialog(
                  context, 
                  'Monitoring', 
                  isPrivate ? 'Azure' : 'Private',
                  ref,
                ),
                icon: Icon(isPrivate ? Icons.cloud_upload : Icons.storage),
                label: Text(isPrivate ? 'Azure-a Keç' : 'Öz Sistemə Qayıt'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: isPrivate ? Colors.blue : Colors.green,
                  foregroundColor: Colors.white,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildModeChip(bool isPrivate) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
      decoration: BoxDecoration(
        color: isPrivate ? Colors.green.withOpacity(0.1) : Colors.blue.withOpacity(0.1),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: isPrivate ? Colors.green.withOpacity(0.3) : Colors.blue.withOpacity(0.3),
        ),
      ),
      child: Text(
        isPrivate ? 'Öz Sistem' : 'Azure',
        style: TextStyle(
          color: isPrivate ? Colors.green : Colors.blue,
          fontWeight: FontWeight.bold,
          fontSize: 12,
        ),
      ),
    );
  }

  Widget _buildCostCard(BuildContext context) {
    return Card(
      elevation: 2,
      color: Colors.grey.shade50,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(Icons.account_balance_wallet, color: Colors.green.shade700),
                const SizedBox(width: 8),
                Text(
                  'Xərc Müqayisəsi',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.bold,
                      ),
                ),
              ],
            ),
            const Divider(),
            const SizedBox(height: 8),
            _buildCostRow('Öz + Öz', '\$0/ay', '✅ 5000 user-ə qədər', true),
            _buildCostRow('Öz + Azure Monitoring', '\$200/ay', '100GB+ log', false),
            _buildCostRow('Azure Messaging + Öz', '\$30/ay', '10,000+ msg/s', false),
            _buildCostRow('Azure + Azure', '\$230/ay', 'Enterprise scale', false),
          ],
        ),
      ),
    );
  }

  Widget _buildCostRow(String config, String cost, String note, bool isRecommended) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4),
      child: Row(
        children: [
          if (isRecommended)
            const Icon(Icons.star, color: Colors.amber, size: 16)
          else
            const SizedBox(width: 16),
          const SizedBox(width: 8),
          Expanded(
            flex: 2,
            child: Text(config, style: const TextStyle(fontSize: 12)),
          ),
          Expanded(
            child: Text(
              cost,
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: cost.contains('0') ? Colors.green : Colors.orange,
                fontSize: 12,
              ),
            ),
          ),
          Expanded(
            flex: 2,
            child: Text(
              note,
              style: TextStyle(
                fontSize: 11,
                color: Colors.grey.shade600,
              ),
              textAlign: TextAlign.right,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildImportantNotes(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.orange.shade50,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Colors.orange.shade200),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.warning_amber, color: Colors.orange.shade700),
              const SizedBox(width: 8),
              Text(
                'Vacib Qeydlər',
                style: TextStyle(
                  fontWeight: FontWeight.bold,
                  color: Colors.orange.shade900,
                ),
              ),
            ],
          ),
          const SizedBox(height: 8),
          Text(
            '• Switch etdikdən sonra server restart tələb olunur\n'
            '• Azure-a keçməzdən əvvəl connection string əlavə edin\n'
            '• Geri qayıtma həmişə mümkündür (pulsuz)\n'
            '• SuperAdmin icazəsi tələb olunur',
            style: TextStyle(
              fontSize: 12,
              color: Colors.orange.shade800,
              height: 1.5,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildErrorCard(BuildContext context, Object error) {
    return Card(
      elevation: 2,
      color: Colors.red.shade50,
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Icon(Icons.error_outline, color: Colors.red.shade700, size: 48),
            const SizedBox(height: 8),
            Text(
              'Xəta baş verdi',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                color: Colors.red.shade700,
              ),
            ),
            Text(
              error.toString(),
              style: TextStyle(color: Colors.red.shade600),
              textAlign: TextAlign.center,
            ),
          ],
        ),
      ),
    );
  }

  void _showSwitchDialog(
    BuildContext context, 
    String system, 
    String newMode,
    WidgetRef ref,
  ) {
    final isToAzure = newMode == 'Azure';
    
    showDialog(
      context: context,
      builder: (context) => AlertDialog(
        title: Row(
          children: [
            Icon(
              isToAzure ? Icons.cloud_upload : Icons.storage,
              color: isToAzure ? Colors.blue : Colors.green,
            ),
            const SizedBox(width: 8),
            Text('$system Dəyişdirilsin?'),
          ],
        ),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              isToAzure
                  ? 'Azure-a keçmək istəyirsiniz?'
                  : 'Öz sisteminə qayıtmaq istəyirsiniz?',
            ),
            const SizedBox(height: 16),
            if (isToAzure)
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: Colors.orange.shade50,
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Row(
                  children: [
                    Icon(Icons.warning, color: Colors.orange.shade700),
                    const SizedBox(width: 8),
                    Expanded(
                      child: Text(
                        'Bu əməliyyat aylıq ödəniş tələb edə bilər!',
                        style: TextStyle(
                          color: Colors.orange.shade800,
                          fontSize: 12,
                        ),
                      ),
                    ),
                  ],
                ),
              ),
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.blue.shade50,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  Icon(Icons.info, color: Colors.blue.shade700),
                  const SizedBox(width: 8),
                  const Expanded(
                    child: Text(
                      'Server restart tələb olunacaq',
                      style: TextStyle(fontSize: 12),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('Ləğv et'),
          ),
          ElevatedButton(
            onPressed: () async {
              Navigator.pop(context);
              await _performSwitch(context, system, newMode, ref);
            },
            style: ElevatedButton.styleFrom(
              backgroundColor: isToAzure ? Colors.blue : Colors.green,
              foregroundColor: Colors.white,
            ),
            child: Text(isToAzure ? 'Azure-a Keç' : 'Öz Sistemə Qayıt'),
          ),
        ],
      ),
    );
  }

  Future<void> _performSwitch(
    BuildContext context,
    String system,
    String newMode,
    WidgetRef ref,
  ) async {
    // Show loading
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => const AlertDialog(
        content: Row(
          children: [
            CircularProgressIndicator(),
            SizedBox(width: 16),
            Text('Yüklənir...'),
          ],
        ),
      ),
    );

    try {
      final notifier = ref.read(serverConfigProvider.notifier);
      await notifier.switchMode(
        system.toLowerCase() == 'messaging' ? 'messaging' : 'monitoring',
        newMode,
      );

      Navigator.pop(context); // Close loading

      // Show success
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('$system $newMode moduna dəyişdirildi'),
          backgroundColor: Colors.green,
          action: SnackBarAction(
            label: 'OK',
            onPressed: () {},
            textColor: Colors.white,
          ),
        ),
      );

      // Refresh providers
      ref.invalidate(serverConfigProvider);
      ref.invalidate(serverStatusProvider);
    } catch (e) {
      Navigator.pop(context); // Close loading

      // Show error
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Xəta: \$e'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }
}
