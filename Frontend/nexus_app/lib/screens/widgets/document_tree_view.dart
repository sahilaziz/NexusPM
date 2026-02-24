import 'package:fluent_ui/fluent_ui.dart';
import '../../models/document_node.dart';

class DocumentTreeView extends StatefulWidget {
  final List<DocumentNode> nodes;
  final Function(DocumentNode) onNodeTap;
  final Function(DocumentNode) onExpand;

  const DocumentTreeView({
    super.key,
    required this.nodes,
    required this.onNodeTap,
    required this.onExpand,
  });

  @override
  State<DocumentTreeView> createState() => _DocumentTreeViewState();
}

class _DocumentTreeViewState extends State<DocumentTreeView> {
  final Set<int> _expandedNodes = {};
  final Map<int, List<DocumentNode>> _children = {};
  final Map<int, bool> _loadingNodes = {};

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      itemCount: widget.nodes.length,
      itemBuilder: (context, index) {
        return _buildNodeTile(widget.nodes[index], 0);
      },
    );
  }

  Widget _buildNodeTile(DocumentNode node, int depth) {
    final isExpanded = _expandedNodes.contains(node.nodeId);
    final hasChildren = node.nodeType != NodeType.document;
    final isLoading = _loadingNodes[node.nodeId] ?? false;
    final children = _children[node.nodeId] ?? [];

    return Column(
      children: [
        ListTile.selectable(
          title: Text(node.entityName),
          subtitle: Text('${node.nodeType.displayName} • ${node.entityCode}'),
          leading: _buildLeadingIcon(node.nodeType, hasChildren, isExpanded, isLoading),
          onPressed: () => widget.onNodeTap(node),
          onExpansionChanged: hasChildren
              ? (expanded) => _onExpansionChanged(node, expanded)
              : null,
          selectionMode: ListTileSelectionMode.none,
        ),
        if (isExpanded && children.isNotEmpty)
          Padding(
            padding: EdgeInsets.only(left: 20.0 * (depth + 1)),
            child: Column(
              children: children.map((child) => _buildNodeTile(child, depth + 1)).toList(),
            ),
          ),
      ],
    );
  }

  Widget _buildLeadingIcon(NodeType type, bool hasChildren, bool isExpanded, bool isLoading) {
    if (isLoading) {
      return const SizedBox(
        width: 20,
        height: 20,
        child: ProgressRing(strokeWidth: 2),
      );
    }

    IconData iconData;
    Color iconColor;

    switch (type) {
      case NodeType.root:
        iconData = FluentIcons.home;
        iconColor = Colors.blue;
        break;
      case NodeType.idare:
        iconData = FluentIcons.factory;
        iconColor = Colors.orange;
        break;
      case NodeType.quyu:
        iconData = FluentIcons.oil_field;
        iconColor = Colors.brown;
        break;
      case NodeType.menteqe:
        iconData = FluentIcons.location;
        iconColor = Colors.purple;
        break;
      case NodeType.document:
        iconData = FluentIcons.document_pdf;
        iconColor = Colors.red;
        break;
    }

    if (hasChildren && isExpanded) {
      iconData = FluentIcons.folder_open;
    } else if (hasChildren) {
      iconData = FluentIcons.folder;
    }

    return Icon(iconData, color: iconColor);
  }

  Future<void> _onExpansionChanged(DocumentNode node, bool expanded) async {
    setState(() {
      if (expanded) {
        _expandedNodes.add(node.nodeId);
        _loadingNodes[node.nodeId] = true;
      } else {
        _expandedNodes.remove(node.nodeId);
      }
    });

    if (expanded && !_children.containsKey(node.nodeId)) {
      await widget.onExpand(node);
      
      setState(() {
        _children[node.nodeId] = [];
        _loadingNodes[node.nodeId] = false;
      });
    }
  }
}
