import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class TimeTrackingScreen extends ConsumerWidget {
  const TimeTrackingScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return DefaultTabController(
      length: 2,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Vaxt İzləmə'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Timer'),
              Tab(text: 'Hesabat'),
            ],
          ),
        ),
        body: const TabBarView(
          children: [
            TimerTab(),
            TimeReportTab(),
          ],
        ),
      ),
    );
  }
}

class TimerTab extends StatelessWidget {
  const TimerTab({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          // Timer Display
          Container(
            width: 250,
            height: 250,
            decoration: BoxDecoration(
              shape: BoxShape.circle,
              border: Border.all(
                color: Theme.of(context).colorScheme.primary,
                width: 8,
              ),
            ),
            child: Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Text(
                    '02:34:15',
                    style: Theme.of(context).textTheme.displayLarge?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'API Integration',
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                      color: Colors.grey[600],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 48),

          // Control Buttons
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              FloatingActionButton.large(
                onPressed: () {},
                backgroundColor: Colors.red,
                child: const Icon(Icons.stop, size: 32),
              ),
              const SizedBox(width: 24),
              FloatingActionButton.large(
                onPressed: () {},
                child: const Icon(Icons.pause, size: 32),
              ),
            ],
          ),
          const SizedBox(height: 32),

          // Task Selector
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 32),
            child: DropdownButtonFormField<String>(
              decoration: const InputDecoration(
                labelText: 'Tapşırıq',
                border: OutlineInputBorder(),
              ),
              value: 'api',
              items: const [
                DropdownMenuItem(
                  value: 'api',
                  child: Text('API Integration'),
                ),
                DropdownMenuItem(
                  value: 'ui',
                  child: Text('UI Design'),
                ),
                DropdownMenuItem(
                  value: 'docs',
                  child: Text('Documentation'),
                ),
              ],
              onChanged: (value) {},
            ),
          ),
        ],
      ),
    );
  }
}

class TimeReportTab extends StatelessWidget {
  const TimeReportTab({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Daily Summary
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Bugün',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      _buildTimeStat(context, 'Ümumi', '6h 30m', Colors.blue),
                      _buildTimeStat(context, 'Billable', '5h 45m', Colors.green),
                      _buildTimeStat(context, 'Tasks', '4', Colors.orange),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Weekly Chart Placeholder
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    'Bu Həftə',
                    style: Theme.of(context).textTheme.titleMedium,
                  ),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      _buildDayBar(context, 'Be', 0.8),
                      _buildDayBar(context, 'Ça', 0.6),
                      _buildDayBar(context, 'Ç', 0.9),
                      _buildDayBar(context, 'Ca', 0.4),
                      _buildDayBar(context, 'C', 0.3, isToday: true),
                      _buildDayBar(context, 'Ş', 0),
                      _buildDayBar(context, 'B', 0),
                    ],
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Recent Entries
          Text(
            'Son Qeydlər',
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
          _buildTimeEntry(context, 'API Integration', '2h 15m', '09:00 - 11:15'),
          _buildTimeEntry(context, 'UI Design Review', '1h 30m', '11:30 - 13:00'),
          _buildTimeEntry(context, 'Documentation', '45m', '14:00 - 14:45'),
        ],
      ),
    );
  }

  Widget _buildTimeStat(BuildContext context, String label, String value, Color color) {
    return Column(
      children: [
        Text(
          value,
          style: Theme.of(context).textTheme.headlineSmall?.copyWith(
            fontWeight: FontWeight.bold,
            color: color,
          ),
        ),
        Text(
          label,
          style: Theme.of(context).textTheme.bodySmall?.copyWith(
            color: Colors.grey[600],
          ),
        ),
      ],
    );
  }

  Widget _buildDayBar(BuildContext context, String day, double height, {bool isToday = false}) {
    return Column(
      children: [
        Container(
          width: 30,
          height: 100,
          decoration: BoxDecoration(
            color: Colors.grey[200],
            borderRadius: BorderRadius.circular(4),
          ),
          child: Align(
            alignment: Alignment.bottomCenter,
            child: Container(
              width: 30,
              height: 100 * height,
              decoration: BoxDecoration(
                color: isToday
                    ? Theme.of(context).colorScheme.primary
                    : Theme.of(context).colorScheme.primary.withOpacity(0.5),
                borderRadius: BorderRadius.circular(4),
              ),
            ),
          ),
        ),
        const SizedBox(height: 4),
        Text(
          day,
          style: TextStyle(
            fontSize: 12,
            fontWeight: isToday ? FontWeight.bold : FontWeight.normal,
            color: isToday ? Theme.of(context).colorScheme.primary : Colors.grey[600],
          ),
        ),
      ],
    );
  }

  Widget _buildTimeEntry(BuildContext context, String task, String duration, String time) {
    return Card(
      child: ListTile(
        leading: Container(
          width: 48,
          height: 48,
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.primary.withOpacity(0.1),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Icon(
            Icons.timer,
            color: Theme.of(context).colorScheme.primary,
          ),
        ),
        title: Text(task),
        subtitle: Text(time),
        trailing: Text(
          duration,
          style: Theme.of(context).textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
          ),
        ),
      ),
    );
  }
}
