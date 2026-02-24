import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../core/api/api_client.dart';
import '../../../features/auth/providers/auth_provider.dart';
import '../../../models/project.dart';

// Project Service
class ProjectService {
  final ApiClient _apiClient;

  ProjectService(this._apiClient);

  Future<List<Project>> getProjects() async {
    final response = await _apiClient.get('/projects');
    final List<dynamic> data = response.data;
    return data.map((json) => Project.fromJson(json)).toList();
  }

  Future<Project> getProject(int id) async {
    final response = await _apiClient.get('/projects/$id');
    return Project.fromJson(response.data);
  }

  Future<Project> createProject({
    required String projectName,
    String? description,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final response = await _apiClient.post('/projects', data: {
      'projectName': projectName,
      'description': description,
      'startDate': startDate?.toIso8601String(),
      'endDate': endDate?.toIso8601String(),
    });
    return Project.fromJson(response.data);
  }

  Future<Project> updateProject({
    required int projectId,
    String? projectName,
    String? description,
    ProjectStatus? status,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    final response = await _apiClient.put('/projects/$projectId', data: {
      if (projectName != null) 'projectName': projectName,
      if (description != null) 'description': description,
      if (status != null) 'status': status.name,
      if (startDate != null) 'startDate': startDate.toIso8601String(),
      if (endDate != null) 'endDate': endDate.toIso8601String(),
    });
    return Project.fromJson(response.data);
  }

  Future<void> deleteProject(int projectId) async {
    await _apiClient.delete('/projects/$projectId');
  }
}

// Project Service Provider
final projectServiceProvider = Provider<ProjectService>((ref) {
  final apiClient = ref.watch(apiClientProvider);
  return ProjectService(apiClient);
});

// Projects State
class ProjectsState {
  final bool isLoading;
  final List<Project> projects;
  final String? error;
  final Project? selectedProject;

  ProjectsState({
    this.isLoading = false,
    this.projects = const [],
    this.error,
    this.selectedProject,
  });

  ProjectsState copyWith({
    bool? isLoading,
    List<Project>? projects,
    String? error,
    Project? selectedProject,
  }) {
    return ProjectsState(
      isLoading: isLoading ?? this.isLoading,
      projects: projects ?? this.projects,
      error: error ?? this.error,
      selectedProject: selectedProject ?? this.selectedProject,
    );
  }
}

// Projects Notifier
class ProjectsNotifier extends StateNotifier<ProjectsState> {
  final ProjectService _projectService;

  ProjectsNotifier(this._projectService) : super(ProjectsState());

  Future<void> loadProjects() async {
    state = state.copyWith(isLoading: true, error: null);
    
    try {
      final projects = await _projectService.getProjects();
      state = state.copyWith(
        isLoading: false,
        projects: projects,
      );
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: 'Layihələri yükləmək mümkün olmadı: $e',
      );
    }
  }

  Future<void> refresh() async {
    await loadProjects();
  }

  void selectProject(Project project) {
    state = state.copyWith(selectedProject: project);
  }

  void clearSelection() {
    state = state.copyWith(selectedProject: null);
  }

  Future<void> createProject({
    required String projectName,
    String? description,
    DateTime? startDate,
    DateTime? endDate,
  }) async {
    try {
      final project = await _projectService.createProject(
        projectName: projectName,
        description: description,
        startDate: startDate,
        endDate: endDate,
      );
      
      state = state.copyWith(
        projects: [...state.projects, project],
      );
    } catch (e) {
      state = state.copyWith(error: 'Layihə yaratmaq mümkün olmadı: $e');
    }
  }

  Future<void> deleteProject(int projectId) async {
    try {
      await _projectService.deleteProject(projectId);
      state = state.copyWith(
        projects: state.projects.where((p) => p.projectId != projectId).toList(),
      );
    } catch (e) {
      state = state.copyWith(error: 'Layihəni silmək mümkün olmadı: $e');
    }
  }
}

// Projects Provider
final projectsProvider = StateNotifierProvider<ProjectsNotifier, ProjectsState>((ref) {
  final projectService = ref.watch(projectServiceProvider);
  return ProjectsNotifier(projectService);
});

// Projects List Provider
final projectsListProvider = Provider<List<Project>>((ref) {
  return ref.watch(projectsProvider).projects;
});

// Projects Loading Provider
final projectsLoadingProvider = Provider<bool>((ref) {
  return ref.watch(projectsProvider).isLoading;
});

// Selected Project Provider
final selectedProjectProvider = Provider<Project?>((ref) {
  return ref.watch(projectsProvider).selectedProject;
});

// Project by ID Provider
final projectByIdProvider = Provider.family<Project?, int>((ref, id) {
  final projects = ref.watch(projectsListProvider);
  return projects.firstWhere((p) => p.projectId == id, orElse: () => null as Project);
});
