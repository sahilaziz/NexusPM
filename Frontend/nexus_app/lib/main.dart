import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:window_manager/window_manager.dart';
import 'package:fluent_ui/fluent_ui.dart' as fluent;

import 'providers/document_provider.dart';
import 'providers/sync_provider.dart';
import 'screens/home_screen.dart';
import 'services/sync_service.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  
  // Windows window manager
  await windowManager.ensureInitialized();
  
  WindowOptions windowOptions = const WindowOptions(
    size: Size(1400, 900),
    center: true,
    title: 'Nexus Project Management',
    minimumSize: Size(1024, 768),
  );
  
  await windowManager.waitUntilReadyToShow(windowOptions, () async {
    await windowManager.show();
    await windowManager.focus();
  });
  
  // Initialize services
  final syncService = SyncService();
  await syncService.initialize();
  
  runApp(NexusApp(syncService: syncService));
}

class NexusApp extends StatelessWidget {
  final SyncService syncService;
  
  const NexusApp({super.key, required this.syncService});

  @override
  Widget build(BuildContext context) {
    return MultiProvider(
      providers: [
        ChangeNotifierProvider(create: (_) => DocumentProvider()),
        ChangeNotifierProvider(create: (_) => SyncProvider(syncService)),
      ],
      child: fluent.FluentApp(
        title: 'Nexus Project Management',
        debugShowCheckedModeBanner: false,
        themeMode: ThemeMode.light,
        theme: fluent.FluentThemeData(
          accentColor: fluent.Colors.blue,
          visualDensity: VisualDensity.standard,
        ),
        darkTheme: fluent.FluentThemeData(
          brightness: Brightness.dark,
          accentColor: fluent.Colors.blue,
        ),
        home: const HomeScreen(),
      ),
    );
  }
}
