import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../providers/auth_provider.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({Key? key}) : super(key: key);

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _usernameController = TextEditingController();
  final _passwordController = TextEditingController();
  bool _isPasswordVisible = false;
  bool _isADLogin = false;
  String? _selectedDomain;

  final List<String> _domains = [
    'nexus.local',
    'azneft.local',
    'socar.local',
  ];

  @override
  void dispose() {
    _usernameController.dispose();
    _passwordController.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    if (!_formKey.currentState!.validate()) return;

    if (_isADLogin) {
      await ref.read(authProvider.notifier).loginWithAD(
        domain: _selectedDomain ?? 'nexus.local',
        username: _usernameController.text.trim(),
        password: _passwordController.text,
      );
    } else {
      await ref.read(authProvider.notifier).login(
        username: _usernameController.text.trim(),
        password: _passwordController.text,
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final authState = ref.watch(authProvider);
    final theme = Theme.of(context);

    // Listen for authentication state changes
    ref.listen(authProvider, (previous, next) {
      if (next.isAuthenticated) {
        Navigator.of(context).pushReplacementNamed('/home');
      }
    });

    return Scaffold(
      body: SafeArea(
        child: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(24.0),
            child: ConstrainedBox(
              constraints: const BoxConstraints(maxWidth: 400),
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // Logo
                  Icon(
                    Icons.account_tree_outlined,
                    size: 80,
                    color: theme.colorScheme.primary,
                  ),
                  const SizedBox(height: 24),
                  
                  // Title
                  Text(
                    'Nexus PM',
                    style: theme.textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                      color: theme.colorScheme.primary,
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 8),
                  Text(
                    'Layihə İdarəetmə Sistemi',
                    style: theme.textTheme.bodyLarge?.copyWith(
                      color: Colors.grey[600],
                    ),
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 32),

                  // Login Type Toggle
                  SegmentedButton<bool>(
                    segments: const [
                      ButtonSegment(
                        value: false,
                        label: Text('Lokal'),
                        icon: Icon(Icons.person_outline),
                      ),
                      ButtonSegment(
                        value: true,
                        label: Text('Active Directory'),
                        icon: Icon(Icons.domain),
                      ),
                    ],
                    selected: {_isADLogin},
                    onSelectionChanged: (value) {
                      setState(() {
                        _isADLogin = value.first;
                      });
                    },
                  ),
                  const SizedBox(height: 24),

                  // Form
                  Form(
                    key: _formKey,
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.stretch,
                      children: [
                        // Domain Dropdown (AD only)
                        if (_isADLogin) ...[
                          DropdownButtonFormField<String>(
                            value: _selectedDomain,
                            decoration: const InputDecoration(
                              labelText: 'Domain',
                              prefixIcon: Icon(Icons.domain),
                              border: OutlineInputBorder(),
                            ),
                            items: _domains.map((domain) {
                              return DropdownMenuItem(
                                value: domain,
                                child: Text(domain),
                              );
                            }).toList(),
                            onChanged: (value) {
                              setState(() {
                                _selectedDomain = value;
                              });
                            },
                            validator: (value) {
                              if (_isADLogin && value == null) {
                                return 'Domain seçin';
                              }
                              return null;
                            },
                          ),
                          const SizedBox(height: 16),
                        ],

                        // Username
                        TextFormField(
                          controller: _usernameController,
                          decoration: InputDecoration(
                            labelText: _isADLogin ? 'İstifadəçi adı' : 'İstifadəçi adı və ya Email',
                            prefixIcon: const Icon(Icons.person_outline),
                            border: const OutlineInputBorder(),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'İstifadəçi adı daxil edin';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 16),

                        // Password
                        TextFormField(
                          controller: _passwordController,
                          obscureText: !_isPasswordVisible,
                          decoration: InputDecoration(
                            labelText: 'Şifrə',
                            prefixIcon: const Icon(Icons.lock_outline),
                            suffixIcon: IconButton(
                              icon: Icon(
                                _isPasswordVisible
                                    ? Icons.visibility_off
                                    : Icons.visibility,
                              ),
                              onPressed: () {
                                setState(() {
                                  _isPasswordVisible = !_isPasswordVisible;
                                });
                              },
                            ),
                            border: const OutlineInputBorder(),
                          ),
                          validator: (value) {
                            if (value == null || value.isEmpty) {
                              return 'Şifrə daxil edin';
                            }
                            return null;
                          },
                        ),
                        const SizedBox(height: 8),

                        // Forgot Password
                        Align(
                          alignment: Alignment.centerRight,
                          child: TextButton(
                            onPressed: () {
                              Navigator.pushNamed(context, '/forgot-password');
                            },
                            child: const Text('Şifrəni unutdunuz?'),
                          ),
                        ),
                        const SizedBox(height: 24),

                        // Error Message
                        if (authState.error != null) ...[
                          Container(
                            padding: const EdgeInsets.all(12),
                            decoration: BoxDecoration(
                              color: Colors.red[50],
                              borderRadius: BorderRadius.circular(8),
                              border: Border.all(color: Colors.red[200]!),
                            ),
                            child: Row(
                              children: [
                                Icon(Icons.error_outline, color: Colors.red[700]),
                                const SizedBox(width: 8),
                                Expanded(
                                  child: Text(
                                    authState.error!,
                                    style: TextStyle(color: Colors.red[700]),
                                  ),
                                ),
                              ],
                            ),
                          ),
                          const SizedBox(height: 16),
                        ],

                        // Login Button
                        FilledButton(
                          onPressed: authState.isLoading ? null : _login,
                          style: FilledButton.styleFrom(
                            padding: const EdgeInsets.symmetric(vertical: 16),
                          ),
                          child: authState.isLoading
                              ? const SizedBox(
                                  height: 20,
                                  width: 20,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white,
                                  ),
                                )
                              : const Text(
                                  'Daxil Ol',
                                  style: TextStyle(fontSize: 16),
                                ),
                        ),
                      ],
                    ),
                  ),

                  const SizedBox(height: 32),

                  // Version
                  Text(
                    'v1.0.0',
                    style: theme.textTheme.bodySmall?.copyWith(
                      color: Colors.grey,
                    ),
                    textAlign: TextAlign.center,
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}
