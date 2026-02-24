import 'package:fluent_ui/fluent_ui.dart';
import 'package:provider/provider.dart';

import '../providers/document_provider.dart';
import '../providers/sync_provider.dart';
import '../models/document_node.dart';
import 'document_upload_screen.dart';
import 'widgets/document_tree_view.dart';
import 'widgets/sync_status_bar.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _selectedIndex = 0;
  final TextEditingController _searchController = TextEditingController();

  @override
  void initState() {
    super.initState();
    // Load root documents
    WidgetsBinding.instance.addPostFrameCallback((_) {
      context.read<DocumentProvider>().loadTree(1);
    });
  }

  @override
  Widget build(BuildContext context) {
    return NavigationView(
      appBar: NavigationAppBar(
        title: const Text('Nexus Project Management'),
        actions: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SyncStatusBar(),
            const SizedBox(width: 16),
            Button(
              onPressed: () => _showUploadDialog(context),
              child: const Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(FluentIcons.upload),
                  SizedBox(width: 8),
                  Text('Sənəd Yüklə'),
                ],
              ),
            ),
            const SizedBox(width: 16),
          ],
        ),
      ),
      pane: NavigationPane(
        selected: _selectedIndex,
        onChanged: (index) => setState(() => _selectedIndex = index),
        displayMode: PaneDisplayMode.open,
        items: [
          PaneItem(
            icon: const Icon(FluentIcons.folder_list),
            title: const Text('Sənədlər'),
            body: const DocumentExplorer(),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.search),
            title: const Text('Axtarış'),
            body: const DocumentSearch(),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.sync),
            title: const Text('Sinxronizasiya'),
            body: const SyncManager(),
          ),
          PaneItem(
            icon: const Icon(FluentIcons.settings),
            title: const Text('Tənzimləmələr'),
            body: const SettingsPage(),
          ),
        ],
      ),
    );
  }

  void _showUploadDialog(BuildContext context) {
    showDialog(
      context: context,
      builder: (context) => const DocumentUploadScreen(),
    );
  }
}

class DocumentExplorer extends StatelessWidget {
  const DocumentExplorer({super.key});

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: const PageHeader(title: Text('Sənəd Kataloqu')),
      content: Consumer<DocumentProvider>(
        builder: (context, provider, child) {
          if (provider.isLoading) {
            return const Center(child: ProgressRing());
          }

          if (provider.error != null) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(FluentIcons.error, size: 48, color: Colors.red),
                  const SizedBox(height: 16),
                  Text('Xəta: ${provider.error}'),
                  const SizedBox(height: 16),
                  Button(
                    onPressed: () => provider.refresh(),
                    child: const Text('Yenidən Yüklə'),
                  ),
                ],
              ),
            );
          }

          return DocumentTreeView(
            nodes: provider.documents,
            onNodeTap: (node) => _showNodeDetails(context, node),
            onExpand: (node) => provider.loadChildren(node.nodeId),
          );
        },
      ),
    );
  }

  void _showNodeDetails(BuildContext context, DocumentNode node) {
    showDialog(
      context: context,
      builder: (context) => ContentDialog(
        title: Text(node.entityName),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            InfoRow(label: 'Tip', value: node.nodeType.displayName),
            InfoRow(label: 'Kod', value: node.entityCode),
            if (node.documentDate != null)
              InfoRow(label: 'Tarix', value: node.documentDate.toString()),
            if (node.documentNumber != null)
              InfoRow(label: 'Nömrə', value: node.documentNumber!),
            if (node.materializedPath != null)
              InfoRow(label: 'Yol', value: node.materializedPath!),
          ],
        ),
        actions: [
          Button(
            onPressed: () => Navigator.pop(context),
            child: const Text('Bağla'),
          ),
        ],
      ),
    );
  }
}

class DocumentSearch extends StatefulWidget {
  const DocumentSearch({super.key});

  @override
  State<DocumentSearch> createState() => _DocumentSearchState();
}

class _DocumentSearchState extends State<DocumentSearch> {
  final TextEditingController _searchController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    return ScaffoldPage(
      header: const PageHeader(title: Text('Sənəd Axtarışı')),
      content: Padding(
        padding: const EdgeInsets.all(16.0),
        child: Column(
          children: [
            TextBox(
              controller: _searchController,
              placeholder: 'Axtarış...',
              prefix: const Icon(FluentIcons.search),
              suffix: Button(
                onPressed: _performSearch,
                child: const Text('Axtar'),
              ),
              onSubmitted: (_) => _performSearch(),
            ),
            const SizedBox(height: 16),
            Expanded(
              child: Consumer<DocumentProvider>(
                builder: (context, provider, child) {
                  if (provider.isLoading) {
                    return const Center(child: ProgressRing());
                  }

                  return ListView.builder(
                    itemCount: provider.documents.length,
                    itemBuilder: (context, index) {
                      final doc = provider.documents[index];
                      return ListTile.selectable(
                        title: Text(doc.entityName),
                        subtitle: Text('${doc.nodeType.displayName} • ${doc.entityCode}'),
                        leading: Icon(_getIconForType(doc.nodeType)),
                        onPressed: () {},
                      );
                    },
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  void _performSearch() {
    final provider = context.read<DocumentProvider>();
    provider.searchDocuments(
      SearchRequest(searchTerm: _searchController.text),
    );
  }

  IconData _getIconForType(NodeType type) {
    switch (type) {
      case NodeType.idare:
        return FluentIcons.factory;
      case NodeType.quyu:
        return FluentIcons.oil_field;
      case NodeType.menteqe:
        return FluentIcons.location;
      case NodeType.document:
        return FluentIcons.document;
      default:
        return FluentIcons.folder;
    }
  }
}

class SyncManager extends StatelessWidget {
  const SyncManager({super.key});

  @override
  Widget build(BuildContext context) {
    return const ScaffoldPage(
      header: PageHeader(title: Text('Sinxronizasiya')),
      content: Center(child: Text('Sync Manager')),
    );
  }
}

class SettingsPage extends StatelessWidget {
  const SettingsPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const ScaffoldPage(
      header: PageHeader(title: Text('Tənzimləmələr')),
      content: Center(child: Text('Settings')),
    );
  }
}

class InfoRow extends StatelessWidget {
  final String label;
  final String value;

  const InfoRow({super.key, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 100,
            child: Text(
              '$label:',
              style: const TextStyle(fontWeight: FontWeight.bold),
            ),
          ),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}
