import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'features/auth/providers/auth_provider.dart';
import 'features/auth/screens/login_screen.dart';
import 'features/projects/screens/project_list_screen.dart';
import 'features/tasks/screens/task_list_screen.dart';
import 'features/time_tracking/screens/time_tracking_screen.dart';
import 'features/admin/screens/server_config_screen.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  runApp(
    const ProviderScope(
      child: NexusApp(),
    ),
  );
}

class NexusApp extends ConsumerWidget {
  const NexusApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final authState = ref.watch(authProvider);

    return MaterialApp(
      title: 'Nexus PM',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF3B82F6),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        cardTheme: CardTheme(
          elevation: 2,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        appBarTheme: const AppBarTheme(
          centerTitle: true,
          elevation: 0,
        ),
        bottomNavigationBarTheme: const BottomNavigationBarThemeData(
          type: BottomNavigationBarType.fixed,
        ),
      ),
      darkTheme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF3B82F6),
          brightness: Brightness.dark,
        ),
        useMaterial3: true,
      ),
      themeMode: ThemeMode.system,
      home: authState.isAuthenticated 
          ? const HomeScreen() 
          : const LoginScreen(),
      routes: {
        '/home': (context) => const HomeScreen(),
        '/login': (context) => const LoginScreen(),
        '/projects': (context) => const ProjectListScreen(),
        '/tasks': (context) => const TaskListScreen(),
        '/time': (context) => const TimeTrackingScreen(),
        '/admin/server-config': (context) => const ServerConfigScreen(),
      },
    );
  }
}

/// Main Home Screen with Bottom Navigation
class HomeScreen extends StatefulWidget {
  const HomeScreen({Key? key}) : super(key: key);

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;

  final List<Widget> _screens = [
    const DashboardScreen(),
    const ProjectListScreen(),
    const TaskListScreen(),
    const TimeTrackingScreen(),
    const ProfileScreen(),
  ];

  final List<BottomNavigationBarItem> _navItems = [
    const BottomNavigationBarItem(
      icon: Icon(Icons.dashboard_outlined),
      activeIcon: Icon(Icons.dashboard),
      label: 'Dashboard',
    ),
    const BottomNavigationBarItem(
      icon: Icon(Icons.folder_outlined),
      activeIcon: Icon(Icons.folder),
      label: 'Layihələr',
    ),
    const BottomNavigationBarItem(
      icon: Icon(Icons.task_outlined),
      activeIcon: Icon(Icons.task),
      label: 'Tapşırıqlar',
    ),
    const BottomNavigationBarItem(
      icon: Icon(Icons.timer_outlined),
      activeIcon: Icon(Icons.timer),
      label: 'Vaxt',
    ),
    const BottomNavigationBarItem(
      icon: Icon(Icons.person_outline),
      activeIcon: Icon(Icons.person),
      label: 'Profil',
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: _screens,
      ),
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) {
          setState(() {
            _currentIndex = index;
          });
        },
        destinations: _navItems.map((item) {
          return NavigationDestination(
            icon: item.icon,
            selectedIcon: item.activeIcon,
            label: item.label!,
          );
        }).toList(),
      ),
    );
  }
}

/// Dashboard Screen Placeholder
class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentUser = ref.watch(currentUserProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Welcome Header
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        'Salam, ${currentUser?.displayName ?? currentUser?.userName ?? 'İstifadəçi'}!',
                        style: Theme.of(context).textTheme.headlineSmall,
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Bugün nə iş görməliyik?',
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                          color: Colors.grey[600],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 16),

              // Quick Stats
              Row(
                children: [
                  _buildStatCard(
                    context,
                    icon: Icons.task_alt,
                    title: 'Tapşırıqlar',
                    value: '12',
                    color: Colors.blue,
                  ),
                  const SizedBox(width: 12),
                  _buildStatCard(
                    context,
                    icon: Icons.timer,
                    title: 'Bugün',
                    value: '4h 30m',
                    color: Colors.green,
                  ),
                ],
              ),
              const SizedBox(height: 16),

              // Today's Tasks
              Text(
                'Bugünkü Tapşırıqlar',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 12),
              _buildTaskItem(context, 'API Integration', 'High', true),
              _buildTaskItem(context, 'UI Design Review', 'Medium', false),
              _buildTaskItem(context, 'Documentation', 'Low', false),

              const SizedBox(height: 16),

              // Quick Actions
              Text(
                'Sürətli Əməliyyatlar',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 12),
              Wrap(
                spacing: 8,
                runSpacing: 8,
                children: [
                  _buildActionChip(context, 'Yeni Tapşırıq', Icons.add_task),
                  _buildActionChip(context, 'Timer Başlat', Icons.play_arrow),
                  _buildActionChip(context, 'Hesabat', Icons.insert_chart),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildStatCard(
    BuildContext context, {
    required IconData icon,
    required String title,
    required String value,
    required Color color,
  }) {
    return Expanded(
      child: Card(
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, color: color),
              const SizedBox(height: 12),
              Text(
                value,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: color,
                ),
              ),
              Text(
                title,
                style: Theme.of(context).textTheme.bodySmall?.copyWith(
                  color: Colors.grey[600],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildTaskItem(BuildContext context, String title, String priority, bool isDone) {
    final priorityColor = priority == 'High'
        ? Colors.red
        : priority == 'Medium'
            ? Colors.orange
            : Colors.green;

    return Card(
      child: ListTile(
        leading: Checkbox(
          value: isDone,
          onChanged: (value) {},
        ),
        title: Text(
          title,
          style: TextStyle(
            decoration: isDone ? TextDecoration.lineThrough : null,
          ),
        ),
        trailing: Container(
          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
          decoration: BoxDecoration(
            color: priorityColor.withOpacity(0.1),
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(
            priority,
            style: TextStyle(
              color: priorityColor,
              fontSize: 12,
              fontWeight: FontWeight.bold,
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildActionChip(BuildContext context, String label, IconData icon) {
    return ActionChip(
      avatar: Icon(icon, size: 18),
      label: Text(label),
      onPressed: () {},
    );
  }
}

/// Profile Screen Placeholder
class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final currentUser = ref.watch(currentUserProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Profil'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: Column(
            children: [
              // Profile Header
              CircleAvatar(
                radius: 50,
                backgroundColor: Theme.of(context).colorScheme.primary,
                child: Text(
                  currentUser?.initials ?? 'U',
                  style: const TextStyle(
                    fontSize: 32,
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ),
              const SizedBox(height: 16),
              Text(
                currentUser?.displayName ?? currentUser?.userName ?? 'İstifadəçi',
                style: Theme.of(context).textTheme.headlineSmall,
              ),
              Text(
                currentUser?.email ?? '',
                style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                  color: Colors.grey[600],
                ),
              ),
              const SizedBox(height: 24),

              // Menu Items
              _buildMenuItem(
                context,
                icon: Icons.person,
                title: 'Şəxsi Məlumatlar',
                onTap: () {},
              ),
              _buildMenuItem(
                context,
                icon: Icons.notifications_outlined,
                title: 'Bildirişlər',
                onTap: () {},
              ),
              _buildMenuItem(
                context,
                icon: Icons.timer_outlined,
                title: 'Vaxt Hesabatı',
                onTap: () {},
              ),
              _buildMenuItem(
                context,
                icon: Icons.settings_outlined,
                title: 'Parametrlər',
                onTap: () {},
              ),
              const Divider(),
              _buildMenuItem(
                context,
                icon: Icons.admin_panel_settings_outlined,
                title: 'Server Konfiqurasiyası',
                onTap: () {
                  Navigator.pushNamed(context, '/admin/server-config');
                },
              ),
              const Divider(),
              _buildMenuItem(
                context,
                icon: Icons.logout,
                title: 'Çıxış',
                color: Colors.red,
                onTap: () {
                  ref.read(authProvider.notifier).logout();
                },
              ),
            ],
          ),
        ),
      ),
    );
  }

  Widget _buildMenuItem(
    BuildContext context, {
    required IconData icon,
    required String title,
    required VoidCallback onTap,
    Color? color,
  }) {
    return ListTile(
      leading: Icon(icon, color: color),
      title: Text(title, style: TextStyle(color: color)),
      trailing: const Icon(Icons.chevron_right),
      onTap: onTap,
    );
  }
}
