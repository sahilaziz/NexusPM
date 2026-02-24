import 'package:file_picker/file_picker.dart';
import 'package:fluent_ui/fluent_ui.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';

import '../models/document_node.dart';
import '../providers/document_provider.dart';
import '../providers/sync_provider.dart';
import '../services/sync_service.dart';

class DocumentUploadScreen extends StatefulWidget {
  const DocumentUploadScreen({super.key});

  @override
  State<DocumentUploadScreen> createState() => _DocumentUploadScreenState();
}

class _DocumentUploadScreenState extends State<DocumentUploadScreen> {
  final _formKey = GlobalKey<FormState>();
  
  // Form controllers
  final _idareCodeController = TextEditingController();
  final _idareNameController = TextEditingController();
  final _quyuCodeController = TextEditingController();
  final _quyuNameController = TextEditingController();
  final _menteqeCodeController = TextEditingController();
  final _menteqeNameController = TextEditingController();
  final _docNumberController = TextEditingController();
  final _subjectController = TextEditingController();
  
  DateTime _selectedDate = DateTime.now();
  DocumentSourceType _sourceType = DocumentSourceType.incomingLetter;
  String? _selectedFilePath;
  bool _isSubmitting = false;
  bool _isCheckingNumber = false;
  bool? _isNumberValid;

  @override
  void dispose() {
    _idareCodeController.dispose();
    _idareNameController.dispose();
    _quyuCodeController.dispose();
    _quyuNameController.dispose();
    _menteqeCodeController.dispose();
    _menteqeNameController.dispose();
    _docNumberController.dispose();
    _subjectController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return ContentDialog(
      title: const Text('Yeni Sənəd / Layihə Yarat'),
      constraints: const BoxConstraints(maxWidth: 700),
      content: SingleChildScrollView(
        child: Form(
          key: _formKey,
          child: Column(
            mainAxisSize: MainAxisSize.min,
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // Sənəd mənbəyi seçimi
              _buildSectionTitle('Sənəd Növü'),
              _buildSourceTypeSelector(),
              const SizedBox(height: 16),
              
              // Mənbəyə görə məlumat qutusu
              _buildSourceTypeInfo(),
              const SizedBox(height: 24),

              // Smart Foldering Section
              _buildSectionTitle('Təşkilati Struktur'),
              _buildRow(
                _buildTextField(
                  controller: _idareCodeController,
                  label: 'İdarə Kodu',
                  placeholder: 'Məs: AZNEFT_IB',
                  isRequired: true,
                ),
                _buildTextField(
                  controller: _idareNameController,
                  label: 'İdarə Adı',
                  placeholder: 'Məs: Azneft İB',
                  isRequired: true,
                ),
              ),
              const SizedBox(height: 12),
              
              _buildRow(
                _buildTextField(
                  controller: _quyuCodeController,
                  label: 'Quyu Kodu',
                  placeholder: 'Məs: QUYU_020',
                  isRequired: true,
                ),
                _buildTextField(
                  controller: _quyuNameController,
                  label: 'Quyu Adı',
                  placeholder: 'Məs: 20 saylı quyu',
                  isRequired: true,
                ),
              ),
              const SizedBox(height: 12),
              
              _buildRow(
                _buildTextField(
                  controller: _menteqeCodeController,
                  label: 'Məntəqə Kodu',
                  placeholder: 'Məs: MNT_001',
                  isRequired: true,
                ),
                _buildTextField(
                  controller: _menteqeNameController,
                  label: 'Məntəqə Adı',
                  placeholder: 'Məs: 1 nömrəli məntəqə',
                  isRequired: true,
                ),
              ),
              
              const SizedBox(height: 24),
              _buildSectionTitle('Sənəd Məlumatları'),
              
              _buildRow(
                _buildDatePicker(),
                _buildDocumentNumberField(),
              ),
              const SizedBox(height: 12),
              
              _buildTextField(
                controller: _subjectController,
                label: _sourceType == DocumentSourceType.internalProject 
                    ? 'Layihə Adı' 
                    : 'Mövzu / Qısa məzmun',
                placeholder: _sourceType == DocumentSourceType.internalProject 
                    ? 'Layihənin adı...' 
                    : 'Sənədin mövzusu...',
                isRequired: true,
              ),
              
              const SizedBox(height: 24),
              _buildSectionTitle('Əlavə Fayl (İstəyə bağlı)'),
              _buildFilePicker(),
              
              // Auto-generated filename preview
              const SizedBox(height: 12),
              _buildFilenamePreview(),
            ],
          ),
        ),
      ),
      actions: [
        Button(
          onPressed: () => Navigator.pop(context),
          child: const Text('Ləğv et'),
        ),
        FilledButton(
          onPressed: _isSubmitting || _isNumberValid == false ? null : _submit,
          child: _isSubmitting
              ? const SizedBox(
                  height: 20,
                  width: 20,
                  child: ProgressRing(strokeWidth: 2),
                )
              : const Text('Yarat'),
        ),
      ],
    );
  }

  Widget _buildSectionTitle(String title) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 12.0),
      child: Text(
        title,
        style: const TextStyle(
          fontSize: 14,
          fontWeight: FontWeight.w600,
          color: Colors.grey,
        ),
      ),
    );
  }

  Widget _buildRow(Widget left, Widget right) {
    return Row(
      children: [
        Expanded(child: left),
        const SizedBox(width: 12),
        Expanded(child: right),
      ],
    );
  }

  Widget _buildSourceTypeSelector() {
    return Row(
      children: [
        Expanded(
          child: RadioButton(
            checked: _sourceType == DocumentSourceType.incomingLetter,
            onChanged: (value) {
              if (value) {
                setState(() => _sourceType = DocumentSourceType.incomingLetter);
              }
            },
            content: const Text('Daxil olan məktub'),
          ),
        ),
        Expanded(
          child: RadioButton(
            checked: _sourceType == DocumentSourceType.internalProject,
            onChanged: (value) {
              if (value) {
                setState(() => _sourceType = DocumentSourceType.internalProject);
              }
            },
            content: const Text('Daxili layihə'),
          ),
        ),
      ],
    );
  }

  Widget _buildSourceTypeInfo() {
    String title;
    String content;
    InfoBarSeverity severity;

    switch (_sourceType) {
      case DocumentSourceType.incomingLetter:
        title = 'Daxil olan məktub';
        content = 'Xaricdən daxil olan məktub üçün sənəd nömrəsini daxil edin. '
            'Format: 1-4-8\\3-2-1243\\2026 və ya 45-а\\123\\2026';
        severity = InfoBarSeverity.info;
        break;
      case DocumentSourceType.internalProject:
        title = 'Daxili layihə';
        content = 'Sistem avtomatik olaraq PRJ-{İDARƏ}-{İL}-{SAY} formatında nömrə yaradacaq. '
            'Məsələn: PRJ-AZNEFT_IB-2026-0001';
        severity = InfoBarSeverity.success;
        break;
      default:
        title = '';
        content = '';
        severity = InfoBarSeverity.info;
    }

    return InfoBar(
      title: Text(title),
      content: Text(content),
      severity: severity,
      isIconVisible: true,
    );
  }

  Widget _buildTextField({
    required TextEditingController controller,
    required String label,
    String? placeholder,
    bool isRequired = false,
  }) {
    return TextFormBox(
      controller: controller,
      placeholder: placeholder,
      header: '$label${isRequired ? ' *' : ''}',
      validator: isRequired
          ? (value) => value == null || value.isEmpty ? '$label tələb olunur' : null
          : null,
    );
  }

  Widget _buildDatePicker() {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text(
          'Sənəd Tarixi *',
          style: TextStyle(fontSize: 12, color: Colors.grey),
        ),
        const SizedBox(height: 4),
        DatePicker(
          selected: _selectedDate,
          onChanged: (date) => setState(() => _selectedDate = date),
        ),
      ],
    );
  }

  Widget _buildDocumentNumberField() {
    // Daxili layihələr üçün nömrə field-i göstərmə (avtomatik yaradılacaq)
    if (_sourceType == DocumentSourceType.internalProject) {
      return Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text(
            'Sənəd Nömrəsi',
            style: TextStyle(fontSize: 12, color: Colors.grey),
          ),
          const SizedBox(height: 4),
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: Colors.grey.withOpacity(0.1),
              borderRadius: BorderRadius.circular(4),
            ),
            child: const Text(
              'Avtomatik yaradılacaq',
              style: TextStyle(color: Colors.grey),
            ),
          ),
        ],
      );
    }

    // Daxil olan məktublar üçün nömrə yoxlama ilə
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        TextBox(
          controller: _docNumberController,
          placeholder: 'Məs: 1-4-8\\3-2-1243\\2026',
          header: 'Sənəd Nömrəsi *',
          onChanged: (_) => _checkDocumentNumber(),
        ),
        if (_isCheckingNumber)
          const Padding(
            padding: EdgeInsets.only(top: 4),
            child: SizedBox(
              height: 16,
              width: 16,
              child: ProgressRing(strokeWidth: 2),
            ),
          )
        else if (_isNumberValid == false)
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              'Bu nömrə artıq istifadə olunur',
              style: TextStyle(color: Colors.red, fontSize: 12),
            ),
          )
        else if (_isNumberValid == true)
          Padding(
            padding: const EdgeInsets.only(top: 4),
            child: Text(
              '✓ Bu nömrə istifadə edilə bilər',
              style: TextStyle(color: Colors.green, fontSize: 12),
            ),
          ),
      ],
    );
  }

  Future<void> _checkDocumentNumber() async {
    if (_docNumberController.text.length < 3) {
      setState(() => _isNumberValid = null);
      return;
    }

    setState(() => _isCheckingNumber = true);

    try {
      // API call to check number
      // final result = await context.read<ApiClient>().apiService.checkDocumentNumber(
      //   _docNumberController.text,
      // );
      // setState(() => _isNumberValid = result.isUnique);
      
      // Demo üçün - local yoxlama
      await Future.delayed(const Duration(milliseconds: 500));
      setState(() => _isNumberValid = true);
    } catch (e) {
      setState(() => _isNumberValid = null);
    } finally {
      setState(() => _isCheckingNumber = false);
    }
  }

  Widget _buildFilePicker() {
    return GestureDetector(
      onTap: _pickFile,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          border: Border.all(color: Colors.grey.withOpacity(0.3)),
          borderRadius: BorderRadius.circular(4),
          color: Colors.grey.withOpacity(0.05),
        ),
        child: Row(
          children: [
            Icon(
              _selectedFilePath != null ? FluentIcons.document : FluentIcons.upload,
              color: _selectedFilePath != null ? Colors.green : Colors.grey,
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                _selectedFilePath != null
                    ? _selectedFilePath!.split('\\').last
                    : 'Fayl seçmək üçün klikləyin (istəyə bağlı)',
                style: TextStyle(
                  color: _selectedFilePath != null ? Colors.black : Colors.grey,
                ),
              ),
            ),
            if (_selectedFilePath != null)
              IconButton(
                icon: const Icon(FluentIcons.clear),
                onPressed: () => setState(() => _selectedFilePath = null),
              ),
          ],
        ),
      ),
    );
  }

  Widget _buildFilenamePreview() {
    final filename = _generateFilename();
    return InfoBar(
      title: const Text('Yaradılacaq fayl adı:'),
      content: Text(
        filename,
        style: const TextStyle(fontFamily: 'Consolas', fontSize: 12),
      ),
      severity: InfoBarSeverity.info,
      isIconVisible: true,
    );
  }

  String _generateFilename() {
    final date = DateFormat('yyyy-MM-dd').format(_selectedDate);
    final docNumber = _sourceType == DocumentSourceType.internalProject
        ? 'AVTO'
        : (_docNumberController.text.isEmpty ? 'XXX' : _docNumberController.text);
    final subject = _subjectController.text.isEmpty ? 'Mövzu' : _subjectController.text;
    return '$date - ${docNumber.replaceAll('\\', '-')} - $subject.pdf';
  }

  Future<void> _pickFile() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.custom,
      allowedExtensions: ['pdf', 'doc', 'docx'],
    );
    
    if (result != null) {
      setState(() {
        _selectedFilePath = result.files.single.path;
      });
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    if (_sourceType == DocumentSourceType.incomingLetter && 
        _docNumberController.text.isEmpty) {
      return;
    }

    setState(() => _isSubmitting = true);

    try {
      if (_sourceType == DocumentSourceType.internalProject) {
        // Daxili layihə yarat
        final request = CreateInternalProjectRequest(
          idareCode: _idareCodeController.text,
          idareName: _idareNameController.text,
          quyuCode: _quyuCodeController.text,
          quyuName: _quyuNameController.text,
          menteqeCode: _menteqeCodeController.text,
          menteqeName: _menteqeNameController.text,
          documentDate: _selectedDate,
          projectName: _subjectController.text,
        );
        
        // API call
        // await context.read<ApiClient>().apiService.createInternalProject(request);
      } else {
        // Daxil olan məktub yarat
        final request = CreateIncomingLetterRequest(
          idareCode: _idareCodeController.text,
          idareName: _idareNameController.text,
          quyuCode: _quyuCodeController.text,
          quyuName: _quyuNameController.text,
          menteqeCode: _menteqeCodeController.text,
          menteqeName: _menteqeNameController.text,
          documentDate: _selectedDate,
          documentNumber: _docNumberController.text,
          subject: _subjectController.text,
        );
        
        // API call
        // await context.read<ApiClient>().apiService.createIncomingLetter(request);
      }

      if (mounted) {
        Navigator.pop(context);
        
        displayInfoBar(context, builder: (context, close) {
          return InfoBar(
            title: const Text('Uğurlu'),
            content: Text(_sourceType == DocumentSourceType.internalProject
                ? 'Daxili layihə yaradıldı'
                : 'Daxil olan məktub qeydə alındı'),
            severity: InfoBarSeverity.success,
            onClose: close,
          );
        });
        
        context.read<DocumentProvider>().refresh();
      }
    } catch (e) {
      if (mounted) {
        displayInfoBar(context, builder: (context, close) {
          return InfoBar(
            title: const Text('Xəta'),
            content: Text('Xəta baş verdi: $e'),
            severity: InfoBarSeverity.error,
            onClose: close,
          );
        });
      }
    } finally {
      if (mounted) {
        setState(() => _isSubmitting = false);
      }
    }
  }
}
