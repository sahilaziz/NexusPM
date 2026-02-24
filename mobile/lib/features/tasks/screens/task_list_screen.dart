import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

class TaskListScreen extends ConsumerWidget {
  const TaskListScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return DefaultTabController(
      length: 4,
      child: Scaffold(
        appBar: AppBar(
          title: const Text('Tapşırıqlar'),
          bottom: const TabBar(
            tabs: [
              Tab(text: 'Hamısı'),
              Tab(text: 'Mənim'),
              Tab(text: 'Bugün'),
              Tab(text: 'Gecikmiş'),
            ],
          ),
        ),
        body: TabBarView(
          children: [
            _buildTaskList('Bütün tapşırıqlar'),
            _buildTaskList('Mənim tapşırıqlarım'),
            _buildTaskList('Bugünkü tapşırıqlar'),
            _buildTaskList('Gecikmiş tapşırıqlar'),
          ],
        ),
        floatingActionButton: FloatingActionButton(
          onPressed: () {
            // TODO: Create task
          },
          child: const Icon(Icons.add),
        ),
      ),
    );
  }

  Widget _buildTaskList(String title) {
    return ListView.builder(
      padding: const EdgeInsets.all(16),
      itemCount: 5,
      itemBuilder: (context, index) {
        return Card(
          child: ListTile(
            leading: Checkbox(
              value: index == 0,
              onChanged: (value) {},
            ),
            title: Text('Tapşırıq ${index + 1}'),
            subtitle: Text('Layihə ${index + 1}'),
            trailing: Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
              decoration: BoxDecoration(
                color: index == 0 ? Colors.green.withOpacity(0.1) : Colors.orange.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Text(
                index == 0 ? 'Tamamlanıb' : 'Davam edir',
                style: TextStyle(
                  color: index == 0 ? Colors.green : Colors.orange,
                  fontSize: 12,
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}
