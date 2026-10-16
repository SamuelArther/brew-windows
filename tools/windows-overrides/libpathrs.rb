class Libpathrs < Formula
  desc "C-friendly API to make path resolution safer on Linux"
  homepage "https://github.com/cyphar/libpathrs"
  url "https://github.com/cyphar/libpathrs/archive/refs/tags/v0.2.6.tar.gz"
  sha256 "6aac480634e9b1dd8a74db4022dbb2ae70208fe5335f4d25345513ac53dbf3e9"
  license any_of: ["MPL-2.0", "LGPL-3.0-or-later"]

  # libpathrs resolves paths inside a root directory so that symlinks, ".." and concurrent renames
  # can never reach outside it (openat2 with RESOLVE_IN_ROOT on Linux). Windows has the same
  # attack surface (symbolic links, junctions, WSL symlinks), so this keg implements libpathrs' C API
  # (upstream's include/pathrs.h) on Windows: every component is opened relative to its parent's
  # handle with NtCreateFile and FILE_OPEN_REPARSE_POINT, links are read and resolved against the
  # root by the library (absolute targets restart at the root, ".." stops at it), and results are C
  # runtime file descriptors. Creation, mkdir_all, rename (RENAME_NOREPLACE), hard and symbolic links,
  # unlink, rmdir and remove_all all work on handles of the resolved parent. Device, FIFO and socket
  # nodes don't exist on Windows file systems (mknod refuses them), and pathrs_proc_* serve what Linux's
  # /proc offers for the calling process on Windows: exe, cwd and fd/<n>.

  def install
    (buildpath/"win/pathrs.c").write <<~'C'
      /* libpathrs for Windows: path resolution scoped to a root directory, race-free against
       * concurrent renames and link swaps. Linux libpathrs walks paths with openat2(RESOLVE_IN_ROOT);
       * here every component is opened relative to its parent's handle (NtCreateFile with
       * RootDirectory, FILE_OPEN_REPARSE_POINT), so the kernel never follows a link or ".." on its own.
       * Symbolic links, junctions and WSL symlinks are read and resolved by this code against the root:
       * absolute targets (C:\x, \??\C:\x, \x, /x) start again at the root and ".." never climbs above
       * it. Handles are returned as C runtime file descriptors (_open_osfhandle); close() releases them.
       * Linux's /proc has no Windows equivalent beyond the process's executable, working directory and
       * open file descriptors, which pathrs_proc_* map to. */
      #define WIN32_LEAN_AND_MEAN
      #include <windows.h>
      #include <winioctl.h>
      #include <errno.h>
      #include <fcntl.h>
      #include <io.h>
      #include <stdio.h>
      #include <stdlib.h>
      #include <string.h>
      #include <sys/stat.h>
      #include "pathrs.h"

      #define PATHRS_VERSION "0.2.6"
      #define MAX_SYMLINKS 40

      #ifndef ELOOP
      #define ELOOP 114
      #endif
      #ifndef ENOTSUP
      #define ENOTSUP 129
      #endif
      #ifndef EXDEV
      #define EXDEV 18
      #endif
      #ifndef SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE
      #define SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE 0x2
      #endif
      #ifndef SYMBOLIC_LINK_FLAG_DIRECTORY
      #define SYMBOLIC_LINK_FLAG_DIRECTORY 0x1
      #endif

      /* ---- ntdll ---- */

      typedef LONG NTSTATUS;
      typedef struct { USHORT Length, MaximumLength; PWSTR Buffer; } NT_UNICODE_STRING;
      typedef struct {
          ULONG Length;
          HANDLE RootDirectory;
          NT_UNICODE_STRING *ObjectName;
          ULONG Attributes;
          PVOID SecurityDescriptor, SecurityQualityOfService;
      } NT_OBJECT_ATTRIBUTES;
      typedef struct { union { NTSTATUS Status; PVOID Pointer; }; ULONG_PTR Information; } NT_IO_STATUS_BLOCK;
      typedef NTSTATUS (NTAPI *nt_create_file_fn)(PHANDLE, ACCESS_MASK, NT_OBJECT_ATTRIBUTES *, NT_IO_STATUS_BLOCK *,
                                                  PLARGE_INTEGER, ULONG, ULONG, ULONG, ULONG, PVOID, ULONG);
      typedef NTSTATUS (NTAPI *nt_set_information_file_fn)(HANDLE, NT_IO_STATUS_BLOCK *, PVOID, ULONG, ULONG);
      typedef ULONG (NTAPI *rtl_status_to_error_fn)(NTSTATUS);

      #define NT_OBJ_CASE_INSENSITIVE 0x40
      #define NT_FILE_OPEN 1
      #define NT_FILE_CREATE 2
      #define NT_FILE_OPEN_IF 3
      #define NT_FILE_OVERWRITE_IF 5
      #define NT_FILE_DIRECTORY_FILE 0x1
      #define NT_FILE_SYNCHRONOUS_IO_NONALERT 0x20
      #define NT_FILE_NON_DIRECTORY_FILE 0x40
      #define NT_FILE_OPEN_FOR_BACKUP_INTENT 0x4000
      #define NT_FILE_OPEN_REPARSE_POINT 0x200000
      #define NT_FILE_LINK_INFORMATION 11
      #define NT_FILE_RENAME_INFORMATION 10

      static nt_create_file_fn nt_create_file;
      static nt_set_information_file_fn nt_set_information_file;
      static rtl_status_to_error_fn rtl_status_to_error;

      static void load_ntdll(void)
      {
          HMODULE ntdll;
          if (nt_create_file != NULL) return;
          ntdll = GetModuleHandleW(L"ntdll.dll");
          nt_set_information_file = (nt_set_information_file_fn)(void *)GetProcAddress(ntdll, "NtSetInformationFile");
          rtl_status_to_error = (rtl_status_to_error_fn)(void *)GetProcAddress(ntdll, "RtlNtStatusToDosError");
          nt_create_file = (nt_create_file_fn)(void *)GetProcAddress(ntdll, "NtCreateFile");
      }

      /* ---- errors: negative ids below __PATHRS_MAX_ERR_VALUE, looked up by pathrs_errorinfo ---- */

      typedef struct error_entry { int id; int saved_errno; char *description; struct error_entry *next; } error_entry;
      static SRWLOCK error_lock = SRWLOCK_INIT;
      static error_entry *errors;
      static int next_error_id = __PATHRS_MAX_ERR_VALUE - 1;

      static int win32_to_errno(DWORD error)
      {
          switch (error) {
          case ERROR_FILE_NOT_FOUND: case ERROR_PATH_NOT_FOUND: case ERROR_INVALID_DRIVE: case ERROR_BAD_NETPATH:
          case ERROR_BAD_PATHNAME: case ERROR_INVALID_NAME: return ENOENT;
          case ERROR_ACCESS_DENIED: case ERROR_SHARING_VIOLATION: case ERROR_LOCK_VIOLATION:
          case ERROR_PRIVILEGE_NOT_HELD: case ERROR_CANT_ACCESS_FILE: return EACCES;
          case ERROR_FILE_EXISTS: case ERROR_ALREADY_EXISTS: return EEXIST;
          case ERROR_DIRECTORY: return ENOTDIR;
          case ERROR_DIR_NOT_EMPTY: return ENOTEMPTY;
          case ERROR_NOT_SAME_DEVICE: return EXDEV;
          case ERROR_NOT_ENOUGH_MEMORY: case ERROR_OUTOFMEMORY: return ENOMEM;
          case ERROR_DISK_FULL: return ENOSPC;
          case ERROR_WRITE_PROTECT: return EROFS;
          case ERROR_TOO_MANY_OPEN_FILES: return EMFILE;
          case ERROR_FILENAME_EXCED_RANGE: return ENAMETOOLONG;
          case ERROR_NOT_A_REPARSE_POINT: case ERROR_INVALID_PARAMETER: return EINVAL;
          case ERROR_NOT_SUPPORTED: case ERROR_INVALID_FUNCTION: return ENOTSUP;
          case ERROR_CANT_RESOLVE_FILENAME: return ELOOP;
          case ERROR_INVALID_HANDLE: return EBADF;
          default: return EIO;
          }
      }

      static int fail(int saved_errno, const char *format, ...)
      {
          char buffer[1024];
          error_entry *entry;
          va_list va;
          int id;
          va_start(va, format);
          vsnprintf(buffer, sizeof(buffer), format, va);
          va_end(va);
          entry = calloc(1, sizeof(*entry));
          if (entry == NULL) return __PATHRS_MAX_ERR_VALUE - 1;
          entry->saved_errno = saved_errno;
          entry->description = _strdup(buffer);
          AcquireSRWLockExclusive(&error_lock);
          id = next_error_id--;
          if (next_error_id < INT32_MIN / 2) next_error_id = __PATHRS_MAX_ERR_VALUE - 1;
          entry->id = id;
          entry->next = errors;
          errors = entry;
          ReleaseSRWLockExclusive(&error_lock);
          return id;
      }

      static int fail_win32(DWORD error, const char *what, const char *path)
      {
          char message[512] = "";
          FormatMessageA(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_IGNORE_INSERTS, NULL, error, 0, message, sizeof(message), NULL);
          message[strcspn(message, "\r\n")] = 0;
          return fail(win32_to_errno(error), "%s %s: %s", what, path != NULL ? path : "", message);
      }

      static int fail_status(NTSTATUS status, const char *what, const char *path)
      {
          return fail_win32(rtl_status_to_error != NULL ? rtl_status_to_error(status) : ERROR_GEN_FAILURE, what, path);
      }

      pathrs_error_t *pathrs_errorinfo(int err_id)
      {
          error_entry **link, *found = NULL;
          pathrs_error_t *info;
          AcquireSRWLockExclusive(&error_lock);
          for (link = &errors; *link != NULL; link = &(*link)->next)
              if ((*link)->id == err_id) { found = *link; *link = found->next; break; }
          ReleaseSRWLockExclusive(&error_lock);
          if (found == NULL) return NULL;
          info = calloc(1, sizeof(*info));
          if (info != NULL) {
              info->saved_errno = (uint64_t)found->saved_errno;
              info->description = found->description;
          }
          else free(found->description);
          free(found);
          return info;
      }

      void pathrs_errorinfo_free(pathrs_error_t *ptr)
      {
          if (ptr == NULL) return;
          free((char *)ptr->description);
          free(ptr);
      }

      int pathrs_version(pathrs_version_info_t *dst, size_t dst_size)
      {
          pathrs_version_info_t info = { PATHRS_VERSION };
          if (dst == NULL) return fail(EINVAL, "pathrs_version: NULL buffer");
          memcpy(dst, &info, dst_size < sizeof(info) ? dst_size : sizeof(info));
          return dst_size < sizeof(info) ? (int)sizeof(info) : 0;
      }

      /* ---- handles and file descriptors ---- */

      static int to_fd(HANDLE handle, const char *path)
      {
          int fd = _open_osfhandle((intptr_t)handle, _O_BINARY | _O_NOINHERIT);
          if (fd < 0) {
              CloseHandle(handle);
              return fail(EMFILE, "no file descriptor for %s", path != NULL ? path : "handle");
          }
          return fd;
      }

      static HANDLE from_fd(int fd)
      {
          return fd < 0 ? INVALID_HANDLE_VALUE : (HANDLE)_get_osfhandle(fd);
      }

      static wchar_t *widen_n(const char *s, int n)
      {
          int count = MultiByteToWideChar(CP_UTF8, 0, s, n, NULL, 0);
          wchar_t *w = malloc(sizeof(wchar_t) * (size_t)(count + 1));
          if (w == NULL) return NULL;
          MultiByteToWideChar(CP_UTF8, 0, s, n, w, count);
          w[count] = 0;
          return w;
      }

      static char *narrow(const wchar_t *w, int n)
      {
          int count = WideCharToMultiByte(CP_UTF8, 0, w, n, NULL, 0, NULL, NULL);
          char *s = malloc((size_t)count + 1);
          if (s == NULL) return NULL;
          WideCharToMultiByte(CP_UTF8, 0, w, n, s, count, NULL, NULL);
          s[count] = 0;
          return s;
      }

      /* Open one path component (no separators) relative to dir, never following reparse points. */
      static NTSTATUS open_at(HANDLE dir, const wchar_t *name, size_t length, ACCESS_MASK access, ULONG disposition,
                              ULONG options, ULONG attributes, HANDLE *out)
      {
          NT_UNICODE_STRING us;
          NT_OBJECT_ATTRIBUTES oa;
          NT_IO_STATUS_BLOCK io;
          us.Buffer = (PWSTR)name;
          us.Length = us.MaximumLength = (USHORT)(length * sizeof(wchar_t));
          memset(&oa, 0, sizeof(oa));
          oa.Length = sizeof(oa);
          oa.RootDirectory = dir;
          oa.ObjectName = &us;
          oa.Attributes = NT_OBJ_CASE_INSENSITIVE;
          memset(&io, 0, sizeof(io));
          *out = NULL;
          return nt_create_file(out, access | SYNCHRONIZE, &oa, &io, NULL, attributes,
                                FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, disposition,
                                options | NT_FILE_OPEN_REPARSE_POINT | NT_FILE_SYNCHRONOUS_IO_NONALERT | NT_FILE_OPEN_FOR_BACKUP_INTENT,
                                NULL, 0);
      }

      /* "." of a directory handle: a new handle to the same directory. */
      static HANDLE dup_dir(HANDLE dir)
      {
          HANDLE copy = NULL;
          if (open_at(dir, L"", 0, FILE_READ_ATTRIBUTES | FILE_TRAVERSE | FILE_LIST_DIRECTORY, NT_FILE_OPEN, 0, 0, &copy) < 0) {
              if (!DuplicateHandle(GetCurrentProcess(), dir, GetCurrentProcess(), &copy, 0, FALSE, DUPLICATE_SAME_ACCESS)) return NULL;
          }
          return copy;
      }

      #define LINK_NONE 0
      #define LINK_SYMLINK 1
      #define LINK_JUNCTION 2
      #define LINK_WSL 3

      #ifndef IO_REPARSE_TAG_LX_SYMLINK
      #define IO_REPARSE_TAG_LX_SYMLINK 0xA000001DUL
      #endif

      /* The link target of a reparse point we follow (symlink, junction, WSL symlink), as UTF-16 with
       * '/' turned into '\'. Sets *kind to LINK_NONE for any other file. */
      static wchar_t *read_link(HANDLE handle, int *kind, bool *relative)
      {
          BYTE *raw;
          FILE_ATTRIBUTE_TAG_INFO tag;
          DWORD returned, reparse_tag;
          wchar_t *target = NULL;
          *kind = LINK_NONE;
          *relative = false;
          if (!GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag))) return NULL;
          if (!(tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)) return NULL;
          if (tag.ReparseTag != IO_REPARSE_TAG_SYMLINK && tag.ReparseTag != IO_REPARSE_TAG_MOUNT_POINT &&
              tag.ReparseTag != IO_REPARSE_TAG_LX_SYMLINK) return NULL;
          raw = malloc(MAXIMUM_REPARSE_DATA_BUFFER_SIZE);
          if (raw == NULL) return NULL;
          if (!DeviceIoControl(handle, FSCTL_GET_REPARSE_POINT, NULL, 0, raw, MAXIMUM_REPARSE_DATA_BUFFER_SIZE, &returned, NULL)) {
              free(raw);
              return NULL;
          }
          reparse_tag = *(DWORD *)raw;
          if (reparse_tag == IO_REPARSE_TAG_SYMLINK || reparse_tag == IO_REPARSE_TAG_MOUNT_POINT) {
              /* Tag, data length, reserved; then substitute/print name offsets and lengths (+ flags for symlinks). */
              USHORT sub_offset = *(USHORT *)(raw + 8), sub_length = *(USHORT *)(raw + 10);
              USHORT print_offset = *(USHORT *)(raw + 12), print_length = *(USHORT *)(raw + 14);
              BYTE *path_buffer = raw + (reparse_tag == IO_REPARSE_TAG_SYMLINK ? 20 : 16);
              USHORT offset = print_length > 0 ? print_offset : sub_offset, length = print_length > 0 ? print_length : sub_length;
              if (reparse_tag == IO_REPARSE_TAG_SYMLINK) *relative = (*(ULONG *)(raw + 16) & 1) != 0;   /* SYMLINK_FLAG_RELATIVE */
              target = malloc(length + sizeof(wchar_t));
              if (target != NULL) {
                  memcpy(target, path_buffer + offset, length);
                  target[length / sizeof(wchar_t)] = 0;
              }
              *kind = reparse_tag == IO_REPARSE_TAG_SYMLINK ? LINK_SYMLINK : LINK_JUNCTION;
          }
          else {
              /* WSL symlink: tag, length, reserved, version, then the UTF-8 target. */
              USHORT length = *(USHORT *)(raw + 4);
              if (length >= 4) {
                  int n = length - 4;
                  target = widen_n((const char *)raw + 12, n);
                  *relative = target != NULL && target[0] != L'/';
              }
              *kind = LINK_WSL;
          }
          free(raw);
          if (target != NULL)
              for (wchar_t *p = target; *p; p++) if (*p == L'/') *p = L'\\';
          return target;
      }

      /* Strip a link target's volume prefix (\??\C:\, \\?\C:\, C:\, \): what remains is relative to the root. */
      static const wchar_t *root_relative(const wchar_t *target)
      {
          if (wcsncmp(target, L"\\??\\", 4) == 0 || wcsncmp(target, L"\\\\?\\", 4) == 0) target += 4;
          if (wcsncmp(target, L"UNC\\", 4) == 0) return NULL;   /* a network share is never inside the root */
          if (((target[0] >= L'A' && target[0] <= L'Z') || (target[0] >= L'a' && target[0] <= L'z')) && target[1] == L':') target += 2;
          while (*target == L'\\') target++;
          return target;
      }

      /* The unresolved remainder of a path: a stack of pending path strings, consumed component by component. */
      typedef struct { wchar_t *text; size_t at; } pending_path;
      typedef struct { pending_path items[MAX_SYMLINKS + 2]; int count; } pending_stack;

      static bool next_component(pending_stack *stack, const wchar_t **name, size_t *length)
      {
          while (stack->count > 0) {
              pending_path *top = &stack->items[stack->count - 1];
              wchar_t *s = top->text + top->at;
              while (*s == L'\\' || *s == L'/') s++;
              if (*s == 0) {
                  free(top->text);
                  stack->count--;
                  continue;
              }
              *name = s;
              *length = wcscspn(s, L"\\/");
              top->at = (size_t)(s - top->text) + *length;
              return true;
          }
          return false;
      }

      static bool more_components(const pending_stack *stack)
      {
          for (int i = stack->count - 1; i >= 0; i--) {
              const wchar_t *s = stack->items[i].text + stack->items[i].at;
              while (*s == L'\\' || *s == L'/') s++;
              if (*s != 0) return true;
          }
          return false;
      }

      static void clear_stack(pending_stack *stack)
      {
          while (stack->count > 0) free(stack->items[--stack->count].text);
      }

      /* Directory handles from the root down to the current position, so ".." can go back up. */
      typedef struct { HANDLE *items; int count, capacity; } handle_chain;

      static void chain_push(handle_chain *chain, HANDLE h)
      {
          if (chain->count == chain->capacity) {
              chain->capacity = chain->capacity ? chain->capacity * 2 : 16;
              chain->items = realloc(chain->items, sizeof(HANDLE) * (size_t)chain->capacity);
          }
          chain->items[chain->count++] = h;
      }

      static void chain_free(handle_chain *chain)
      {
          for (int i = 1; i < chain->count; i++) CloseHandle(chain->items[i]);   /* items[0] is the caller's root */
          free(chain->items);
      }

      /* Resolve path inside root. With want_parent, stop before the last component and return the parent
       * directory's handle plus the (unresolved) last name; otherwise return a handle to the final file
       * (follow_last decides whether a trailing link is followed). */
      static int resolve(HANDLE root, const char *path, bool follow_last, bool want_parent, ACCESS_MASK final_access,
                         HANDLE *out, wchar_t **last_name)
      {
          pending_stack stack;
          handle_chain chain = { 0 };
          const wchar_t *name;
          size_t length;
          int links = 0;
          load_ntdll();
          if (nt_create_file == NULL) return fail(ENOSYS, "NtCreateFile is unavailable");
          if (path == NULL) return fail(EINVAL, "NULL path");
          stack.count = 1;
          stack.items[0].text = widen_n(path, -1);
          stack.items[0].at = 0;
          if (stack.items[0].text == NULL) return fail(ENOMEM, "out of memory");
          chain_push(&chain, root);
          if (last_name != NULL) *last_name = NULL;

          while (next_component(&stack, &name, &length)) {
              HANDLE current = chain.items[chain.count - 1], next = NULL;
              bool last = !more_components(&stack);
              NTSTATUS status;
              FILE_ATTRIBUTE_TAG_INFO tag;
              if (length == 1 && name[0] == L'.') {
                  if (last && want_parent) { *last_name = _wcsdup(L"."); break; }
                  continue;
              }
              if (length == 2 && name[0] == L'.' && name[1] == L'.') {
                  if (last && want_parent) { *last_name = _wcsdup(L".."); break; }
                  /* ".." never climbs above the root. */
                  if (chain.count > 1) CloseHandle(chain.items[--chain.count]);
                  continue;
              }
              if (last && want_parent) {
                  *last_name = malloc((length + 1) * sizeof(wchar_t));
                  memcpy(*last_name, name, length * sizeof(wchar_t));
                  (*last_name)[length] = 0;
                  break;
              }
              status = open_at(current, name, length,
                               last ? final_access | FILE_READ_ATTRIBUTES : FILE_READ_ATTRIBUTES | FILE_TRAVERSE | FILE_LIST_DIRECTORY,
                               NT_FILE_OPEN, 0, 0, &next);
              if (status < 0) {
                  int id = fail_status(status, "resolve", path);
                  clear_stack(&stack);
                  chain_free(&chain);
                  return id;
              }
              if (!GetFileInformationByHandleEx(next, FileAttributeTagInfo, &tag, sizeof(tag))) tag.FileAttributes = 0;
              if ((tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) && (!last || follow_last)) {
                  int kind;
                  bool relative;
                  wchar_t *target = read_link(next, &kind, &relative);
                  if (kind != LINK_NONE) {
                      const wchar_t *rest;
                      CloseHandle(next);
                      if (target == NULL || ++links > MAX_SYMLINKS || stack.count >= MAX_SYMLINKS + 1) {
                          free(target);
                          clear_stack(&stack);
                          chain_free(&chain);
                          return fail(ELOOP, "resolve %s: too many levels of symbolic links", path);
                      }
                      if (!relative) {
                          /* Absolute target: start again at the root. */
                          rest = root_relative(target);
                          if (rest == NULL) {
                              free(target);
                              clear_stack(&stack);
                              chain_free(&chain);
                              return fail(EXDEV, "resolve %s: link to a network share", path);
                          }
                          while (chain.count > 1) CloseHandle(chain.items[--chain.count]);
                          memmove(target, rest, (wcslen(rest) + 1) * sizeof(wchar_t));
                      }
                      stack.items[stack.count].text = target;
                      stack.items[stack.count].at = 0;
                      stack.count++;
                      continue;
                  }
                  free(target);
              }
              if (!last && !(tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
                  CloseHandle(next);
                  clear_stack(&stack);
                  chain_free(&chain);
                  return fail(ENOTDIR, "resolve %s: not a directory", path);
              }
              if (last) {
                  *out = next;
                  chain_free(&chain);
                  clear_stack(&stack);
                  return 0;
              }
              chain_push(&chain, next);
          }
          clear_stack(&stack);
          /* Ran out of components on a directory (the root itself, or a path ending in "." or ".."). */
          *out = chain.count > 1 ? chain.items[--chain.count] : dup_dir(root);
          chain_free(&chain);
          if (*out == NULL) return fail_win32(GetLastError(), "resolve", path);
          return 0;
      }

      static ACCESS_MASK access_for(uint64_t flags)
      {
          ACCESS_MASK access = FILE_READ_ATTRIBUTES | READ_CONTROL;
          if (flags & O_PATH) return FILE_READ_ATTRIBUTES;
          switch (flags & 3) {
          case O_WRONLY: access |= FILE_WRITE_DATA | FILE_APPEND_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA; break;
          case O_RDWR: access |= FILE_GENERIC_READ | FILE_WRITE_DATA | FILE_APPEND_DATA | FILE_WRITE_ATTRIBUTES | FILE_WRITE_EA; break;
          default: access |= FILE_GENERIC_READ; break;
          }
          if ((flags & O_APPEND) && (flags & 3) != O_RDONLY) access = (access & ~FILE_WRITE_DATA) | FILE_APPEND_DATA;
          return access;
      }

      static int crt_flags(uint64_t flags)
      {
          int crt = _O_BINARY | _O_NOINHERIT;
          if ((flags & 3) == O_WRONLY) crt |= _O_WRONLY;
          else if ((flags & 3) == O_RDWR) crt |= _O_RDWR;
          else crt |= _O_RDONLY;
          if (flags & O_APPEND) crt |= _O_APPEND;
          return crt;
      }

      static int finish_open(HANDLE handle, uint64_t flags, const char *path)
      {
          FILE_ATTRIBUTE_TAG_INFO tag;
          int fd;
          if ((flags & O_DIRECTORY) && GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) &&
              !(tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
              CloseHandle(handle);
              return fail(ENOTDIR, "open %s: not a directory", path);
          }
          if (flags & O_TRUNC) {
              FILE_END_OF_FILE_INFO eof = { 0 };
              SetFileInformationByHandle(handle, FileEndOfFileInfo, &eof, sizeof(eof));
          }
          fd = _open_osfhandle((intptr_t)handle, crt_flags(flags));
          if (fd < 0) {
              CloseHandle(handle);
              return fail(EMFILE, "no file descriptor for %s", path);
          }
          return fd;
      }

      /* ---- public API ---- */

      int pathrs_open_root(const char *path)
      {
          wchar_t *wide;
          HANDLE handle;
          FILE_ATTRIBUTE_TAG_INFO tag;
          load_ntdll();
          if (path == NULL) return fail(EINVAL, "pathrs_open_root: NULL path");
          wide = widen_n(path, -1);
          if (wide == NULL) return fail(ENOMEM, "out of memory");
          handle = CreateFileW(wide, FILE_READ_ATTRIBUTES | FILE_TRAVERSE | FILE_LIST_DIRECTORY | SYNCHRONIZE,
                               FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING,
                               FILE_FLAG_BACKUP_SEMANTICS, NULL);
          free(wide);
          if (handle == INVALID_HANDLE_VALUE) return fail_win32(GetLastError(), "open root", path);
          if (!GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) || !(tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY)) {
              CloseHandle(handle);
              return fail(ENOTDIR, "open root %s: not a directory", path);
          }
          return to_fd(handle, path);
      }

      int pathrs_reopen(int fd, uint64_t flags)
      {
          HANDLE handle = from_fd(fd), copy;
          FILE_ATTRIBUTE_TAG_INFO tag;
          DWORD attributes = FILE_FLAG_BACKUP_SEMANTICS;
          if (handle == INVALID_HANDLE_VALUE) return fail(EBADF, "reopen: bad file descriptor %d", fd);
          if (flags & O_CREAT) return fail(EINVAL, "reopen: O_CREAT is not supported");
          if (GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) && (tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT))
              attributes |= FILE_FLAG_OPEN_REPARSE_POINT;
          copy = ReOpenFile(handle, access_for(flags) | SYNCHRONIZE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, attributes);
          if (copy == INVALID_HANDLE_VALUE) return fail_win32(GetLastError(), "reopen", "");
          return finish_open(copy, flags, "reopened file");
      }

      int pathrs_inroot_resolve(int root_fd, const char *path)
      {
          HANDLE handle;
          int r = resolve(from_fd(root_fd), path, true, false, FILE_READ_ATTRIBUTES, &handle, NULL);
          return r < 0 ? r : to_fd(handle, path);
      }

      int pathrs_inroot_resolve_nofollow(int root_fd, const char *path)
      {
          HANDLE handle;
          int r = resolve(from_fd(root_fd), path, false, false, FILE_READ_ATTRIBUTES, &handle, NULL);
          return r < 0 ? r : to_fd(handle, path);
      }

      int pathrs_inroot_open(int root_fd, const char *path, uint64_t flags)
      {
          HANDLE handle;
          int r;
          if (flags & O_CREAT) return fail(EINVAL, "pathrs_inroot_open: O_CREAT is not supported (use pathrs_inroot_creat)");
          r = resolve(from_fd(root_fd), path, !(flags & O_NOFOLLOW), false, access_for(flags), &handle, NULL);
          return r < 0 ? r : finish_open(handle, flags, path);
      }

      int pathrs_inroot_readlink(int root_fd, const char *path, char *linkbuf, size_t linkbuf_size)
      {
          HANDLE handle;
          int kind, r, n;
          bool relative;
          wchar_t *target;
          char *utf8;
          r = resolve(from_fd(root_fd), path, false, false, FILE_READ_ATTRIBUTES, &handle, NULL);
          if (r < 0) return r;
          target = read_link(handle, &kind, &relative);
          CloseHandle(handle);
          if (kind == LINK_NONE || target == NULL) {
              free(target);
              return fail(EINVAL, "readlink %s: not a symbolic link", path);
          }
          if (wcsncmp(target, L"\\??\\", 4) == 0) memmove(target, target + 4, (wcslen(target + 4) + 1) * sizeof(wchar_t));
          utf8 = narrow(target, -1);
          free(target);
          if (utf8 == NULL) return fail(ENOMEM, "out of memory");
          n = (int)strlen(utf8);
          if (linkbuf != NULL) memcpy(linkbuf, utf8, (size_t)n < linkbuf_size ? (size_t)n : linkbuf_size);
          free(utf8);
          return n;
      }

      /* The parent directory of path (resolved) and its last component. */
      static int parent_of(int root_fd, const char *path, HANDLE *parent, wchar_t **name)
      {
          int r = resolve(from_fd(root_fd), path, true, true, FILE_READ_ATTRIBUTES, parent, name);
          if (r < 0) return r;
          if (*name == NULL || wcscmp(*name, L".") == 0 || wcscmp(*name, L"..") == 0) {
              CloseHandle(*parent);
              free(*name);
              return fail(EINVAL, "%s: refers to the root or a directory traversal", path);
          }
          return 0;
      }

      int pathrs_inroot_creat(int root_fd, const char *path, uint64_t flags, unsigned int mode)
      {
          HANDLE parent, handle;
          wchar_t *name;
          NTSTATUS status;
          ULONG disposition;
          int r = parent_of(root_fd, path, &parent, &name);
          if (r < 0) return r;
          disposition = (flags & O_EXCL) ? NT_FILE_CREATE : (flags & O_TRUNC) ? NT_FILE_OVERWRITE_IF : NT_FILE_OPEN_IF;
          status = open_at(parent, name, wcslen(name), access_for(flags & ~(uint64_t)O_PATH), disposition,
                           NT_FILE_NON_DIRECTORY_FILE, (mode & S_IWRITE) ? FILE_ATTRIBUTE_NORMAL : FILE_ATTRIBUTE_READONLY, &handle);
          if (status >= 0) {
              FILE_ATTRIBUTE_TAG_INFO tag;
              /* An existing link in the last component is followed (O_CREAT without O_EXCL), within the root. */
              if (!(flags & O_EXCL) && GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) &&
                  (tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)) {
                  CloseHandle(handle);
                  CloseHandle(parent);
                  free(name);
                  if (flags & O_NOFOLLOW) return fail(ELOOP, "creat %s: is a symbolic link", path);
                  r = resolve(from_fd(root_fd), path, true, false, access_for(flags), &handle, NULL);
                  return r < 0 ? r : finish_open(handle, flags, path);
              }
          }
          CloseHandle(parent);
          free(name);
          if (status < 0) return fail_status(status, "creat", path);
          return finish_open(handle, flags & ~(uint64_t)O_TRUNC, path);
      }

      int pathrs_inroot_mkdir(int root_fd, const char *path, unsigned int mode)
      {
          HANDLE parent, handle;
          wchar_t *name;
          NTSTATUS status;
          int r = parent_of(root_fd, path, &parent, &name);
          (void)mode;
          if (r < 0) return r;
          status = open_at(parent, name, wcslen(name), FILE_READ_ATTRIBUTES, NT_FILE_CREATE, NT_FILE_DIRECTORY_FILE, FILE_ATTRIBUTE_NORMAL, &handle);
          CloseHandle(parent);
          free(name);
          if (status < 0) return fail_status(status, "mkdir", path);
          CloseHandle(handle);
          return 0;
      }

      int pathrs_inroot_mkdir_all(int root_fd, const char *path, unsigned int mode)
      {
          /* Create each missing directory in turn, resolving everything that exists inside the root. */
          size_t n = path != NULL ? strlen(path) : 0;
          char *prefix;
          HANDLE handle;
          int r;
          if (path == NULL) return fail(EINVAL, "mkdir_all: NULL path");
          prefix = malloc(n + 1);
          if (prefix == NULL) return fail(ENOMEM, "out of memory");
          for (size_t i = 0; i <= n; i++) {
              if (i < n && path[i] != '/' && path[i] != '\\') continue;
              if (i == 0 || path[i - 1] == '/' || path[i - 1] == '\\') continue;
              memcpy(prefix, path, i);
              prefix[i] = 0;
              r = resolve(from_fd(root_fd), prefix, true, false, FILE_READ_ATTRIBUTES, &handle, NULL);
              if (r >= 0) {
                  FILE_ATTRIBUTE_TAG_INFO tag;
                  bool is_dir = GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) &&
                                (tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY);
                  CloseHandle(handle);
                  if (!is_dir) { free(prefix); return fail(ENOTDIR, "mkdir_all %s: %s is not a directory", path, prefix); }
                  continue;
              }
              {
                  pathrs_error_t *info = pathrs_errorinfo(r);
                  bool missing = info != NULL && info->saved_errno == ENOENT;
                  pathrs_errorinfo_free(info);
                  if (!missing) { free(prefix); return fail(EACCES, "mkdir_all %s: cannot resolve %s", path, prefix); }
              }
              r = pathrs_inroot_mkdir(root_fd, prefix, mode);
              if (r < 0) {
                  pathrs_error_t *info = pathrs_errorinfo(r);
                  bool exists = info != NULL && info->saved_errno == EEXIST;   /* lost a race with another creator */
                  pathrs_errorinfo_free(info);
                  if (!exists) { free(prefix); return fail(EACCES, "mkdir_all %s: cannot create %s", path, prefix); }
              }
          }
          free(prefix);
          r = resolve(from_fd(root_fd), path, true, false, FILE_READ_ATTRIBUTES | FILE_TRAVERSE | FILE_LIST_DIRECTORY, &handle, NULL);
          return r < 0 ? r : to_fd(handle, path);
      }

      int pathrs_inroot_mknod(int root_fd, const char *path, unsigned int mode, dev_t dev)
      {
          (void)dev;
          switch (mode & S_IFMT) {
          case 0:
          case S_IFREG: {
              int fd = pathrs_inroot_creat(root_fd, path, O_WRONLY | O_EXCL, mode);
              if (fd < 0) return fd;
              _close(fd);
              return 0;
          }
          case S_IFDIR:
              return pathrs_inroot_mkdir(root_fd, path, mode);
          default:
              return fail(EPERM, "mknod %s: Windows file systems have no device, FIFO or socket nodes", path);
          }
      }

      static int delete_handle(HANDLE handle, const char *what, const char *path)
      {
          FILE_DISPOSITION_INFO disposition = { TRUE };
          FILE_BASIC_INFO basic;
          /* A read-only file can't be deleted; clear the attribute first, like unlink(2) ignores modes. */
          if (GetFileInformationByHandleEx(handle, FileBasicInfo, &basic, sizeof(basic)) && (basic.FileAttributes & FILE_ATTRIBUTE_READONLY)) {
              basic.FileAttributes &= ~(DWORD)FILE_ATTRIBUTE_READONLY;
              SetFileInformationByHandle(handle, FileBasicInfo, &basic, sizeof(basic));
          }
          if (!SetFileInformationByHandle(handle, FileDispositionInfo, &disposition, sizeof(disposition)))
              return fail_win32(GetLastError(), what, path);
          return 0;
      }

      static int remove_entry(int root_fd, const char *path, ULONG options, const char *what)
      {
          HANDLE parent, handle;
          wchar_t *name;
          NTSTATUS status;
          int r = parent_of(root_fd, path, &parent, &name);
          if (r < 0) return r;
          status = open_at(parent, name, wcslen(name), DELETE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, NT_FILE_OPEN, options, 0, &handle);
          CloseHandle(parent);
          free(name);
          if (status < 0) return fail_status(status, what, path);
          r = delete_handle(handle, what, path);
          CloseHandle(handle);
          return r;
      }

      int pathrs_inroot_rmdir(int root_fd, const char *path)
      {
          return remove_entry(root_fd, path, NT_FILE_DIRECTORY_FILE, "rmdir");
      }

      int pathrs_inroot_unlink(int root_fd, const char *path)
      {
          /* Links to directories (symlinks, junctions) are unlinked too: they're opened as the link itself. */
          HANDLE parent, handle;
          wchar_t *name;
          NTSTATUS status;
          FILE_ATTRIBUTE_TAG_INFO tag;
          int r = parent_of(root_fd, path, &parent, &name);
          if (r < 0) return r;
          status = open_at(parent, name, wcslen(name), DELETE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, NT_FILE_OPEN, 0, 0, &handle);
          CloseHandle(parent);
          free(name);
          if (status < 0) return fail_status(status, "unlink", path);
          if (GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) &&
              (tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) && !(tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)) {
              CloseHandle(handle);
              return fail(EISDIR, "unlink %s: is a directory", path);
          }
          r = delete_handle(handle, "unlink", path);
          CloseHandle(handle);
          return r;
      }

      /* Delete everything below dir (never following links), then the caller deletes dir itself. */
      static int remove_tree(HANDLE dir, const char *path, int depth)
      {
          BYTE buffer[64 * 1024];
          FILE_INFO_BY_HANDLE_CLASS info_class = FileFullDirectoryRestartInfo;
          int result = 0;
          if (depth > 4096) return fail(ELOOP, "remove_all %s: too deep", path);
          for (;;) {
              FILE_FULL_DIR_INFO *entry;
              BOOL any = FALSE;
              if (!GetFileInformationByHandleEx(dir, info_class, buffer, sizeof(buffer))) {
                  DWORD error = GetLastError();
                  if (error == ERROR_NO_MORE_FILES) break;
                  return fail_win32(error, "remove_all", path);
              }
              info_class = FileFullDirectoryInfo;
              for (entry = (FILE_FULL_DIR_INFO *)buffer;; entry = (FILE_FULL_DIR_INFO *)((BYTE *)entry + entry->NextEntryOffset)) {
                  size_t length = entry->FileNameLength / sizeof(wchar_t);
                  if (!((length == 1 && entry->FileName[0] == L'.') || (length == 2 && entry->FileName[0] == L'.' && entry->FileName[1] == L'.'))) {
                      HANDLE child;
                      bool recurse = (entry->FileAttributes & FILE_ATTRIBUTE_DIRECTORY) && !(entry->FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT);
                      NTSTATUS status = open_at(dir, entry->FileName, length,
                                                DELETE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES | (recurse ? FILE_LIST_DIRECTORY | FILE_TRAVERSE : 0),
                                                NT_FILE_OPEN, 0, 0, &child);
                      any = TRUE;
                      if (status < 0) { result = fail_status(status, "remove_all", path); continue; }
                      if (recurse) {
                          int r = remove_tree(child, path, depth + 1);
                          if (r < 0) result = r;
                      }
                      if (delete_handle(child, "remove_all", path) < 0 && result == 0) result = fail(ENOTEMPTY, "remove_all %s: could not delete an entry", path);
                      CloseHandle(child);
                  }
                  if (entry->NextEntryOffset == 0) break;
              }
              if (!any) break;
          }
          return result;
      }

      int pathrs_inroot_remove_all(int root_fd, const char *path)
      {
          HANDLE parent, handle;
          wchar_t *name;
          NTSTATUS status;
          FILE_ATTRIBUTE_TAG_INFO tag;
          int r = parent_of(root_fd, path, &parent, &name);
          if (r < 0) return r;
          status = open_at(parent, name, wcslen(name), DELETE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES | FILE_LIST_DIRECTORY | FILE_TRAVERSE,
                           NT_FILE_OPEN, 0, 0, &handle);
          if (status < 0)
              status = open_at(parent, name, wcslen(name), DELETE | FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, NT_FILE_OPEN, 0, 0, &handle);
          CloseHandle(parent);
          free(name);
          if (status < 0) {
              /* Like os.RemoveAll, a missing path is not an error. */
              DWORD error = rtl_status_to_error(status);
              if (error == ERROR_FILE_NOT_FOUND || error == ERROR_PATH_NOT_FOUND) return 0;
              return fail_status(status, "remove_all", path);
          }
          if (GetFileInformationByHandleEx(handle, FileAttributeTagInfo, &tag, sizeof(tag)) &&
              (tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) && !(tag.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT)) {
              r = remove_tree(handle, path, 0);
              if (r < 0) { CloseHandle(handle); return r; }
          }
          r = delete_handle(handle, "remove_all", path);
          CloseHandle(handle);
          return r;
      }

      /* FILE_RENAME_INFORMATION / FILE_LINK_INFORMATION share this layout. */
      typedef struct { BOOLEAN ReplaceIfExists; HANDLE RootDirectory; ULONG FileNameLength; WCHAR FileName[1]; } name_information;

      static int set_name(HANDLE handle, HANDLE new_parent, const wchar_t *new_name, bool replace, ULONG info_class,
                          const char *what, const char *path)
      {
          size_t length = wcslen(new_name) * sizeof(wchar_t);
          size_t size = sizeof(name_information) + length;
          name_information *info = calloc(1, size);
          NT_IO_STATUS_BLOCK io;
          NTSTATUS status;
          if (info == NULL) return fail(ENOMEM, "out of memory");
          info->ReplaceIfExists = replace;
          info->RootDirectory = new_parent;
          info->FileNameLength = (ULONG)length;
          memcpy(info->FileName, new_name, length);
          status = nt_set_information_file(handle, &io, info, (ULONG)size, info_class);
          free(info);
          return status < 0 ? fail_status(status, what, path) : 0;
      }

      int pathrs_inroot_rename(int old_root_fd, const char *old_path, int new_root_fd, const char *new_path, uint64_t flags)
      {
          HANDLE old_parent, new_parent, handle;
          wchar_t *old_name, *new_name;
          NTSTATUS status;
          int r;
          if (flags & ~(uint64_t)RENAME_NOREPLACE)
              return fail(EINVAL, "rename %s: only RENAME_NOREPLACE is supported on Windows (no exchange or whiteout)", old_path);
          r = parent_of(old_root_fd, old_path, &old_parent, &old_name);
          if (r < 0) return r;
          r = parent_of(new_root_fd, new_path, &new_parent, &new_name);
          if (r < 0) { CloseHandle(old_parent); free(old_name); return r; }
          status = open_at(old_parent, old_name, wcslen(old_name), DELETE | FILE_READ_ATTRIBUTES, NT_FILE_OPEN, 0, 0, &handle);
          if (status < 0) r = fail_status(status, "rename", old_path);
          else {
              r = set_name(handle, new_parent, new_name, !(flags & RENAME_NOREPLACE), NT_FILE_RENAME_INFORMATION, "rename", old_path);
              CloseHandle(handle);
          }
          CloseHandle(old_parent);
          CloseHandle(new_parent);
          free(old_name);
          free(new_name);
          return r;
      }

      int pathrs_inroot_hardlink(int old_root_fd, const char *old_path, int new_root_fd, const char *new_path, uint64_t flags)
      {
          HANDLE source, new_parent;
          wchar_t *new_name;
          int r = resolve(from_fd(old_root_fd), old_path, (flags & AT_SYMLINK_FOLLOW) != 0, false,
                          FILE_READ_ATTRIBUTES | FILE_WRITE_ATTRIBUTES, &source, NULL);
          if (r < 0) return r;
          r = parent_of(new_root_fd, new_path, &new_parent, &new_name);
          if (r < 0) { CloseHandle(source); return r; }
          r = set_name(source, new_parent, new_name, false, NT_FILE_LINK_INFORMATION, "hardlink", new_path);
          CloseHandle(source);
          CloseHandle(new_parent);
          free(new_name);
          return r;
      }

      /* Windows links are typed: a directory link when the target (resolved in the root, relative to the
       * link's directory) is a directory; a dangling link is a file link. */
      static bool link_is_directory(int root_fd, const char *target, const char *linkpath)
      {
          size_t cut = 0, n = strlen(linkpath);
          char *in_root;
          HANDLE probe;
          bool directory = false;
          int r;
          for (size_t i = 0; i < n; i++) if (linkpath[i] == '/' || linkpath[i] == '\\') cut = i + 1;
          in_root = malloc(cut + strlen(target) + 1);
          if (in_root == NULL) return false;
          if (target[0] == '/' || target[0] == '\\') strcpy(in_root, target);
          else { memcpy(in_root, linkpath, cut); strcpy(in_root + cut, target); }
          r = resolve(from_fd(root_fd), in_root, true, false, FILE_READ_ATTRIBUTES, &probe, NULL);
          if (r >= 0) {
              FILE_ATTRIBUTE_TAG_INFO tag;
              directory = GetFileInformationByHandleEx(probe, FileAttributeTagInfo, &tag, sizeof(tag)) &&
                          (tag.FileAttributes & FILE_ATTRIBUTE_DIRECTORY);
              CloseHandle(probe);
          }
          else pathrs_errorinfo_free(pathrs_errorinfo(r));
          free(in_root);
          return directory;
      }

      int pathrs_inroot_symlink(const char *target, int root_fd, const char *linkpath)
      {
          HANDLE parent, check;
          wchar_t *name, *wide_target, *parent_path, *full;
          DWORD n, link_flags = SYMBOLIC_LINK_FLAG_ALLOW_UNPRIVILEGED_CREATE;
          NTSTATUS status;
          int r;
          if (target == NULL) return fail(EINVAL, "symlink: NULL target");
          r = parent_of(root_fd, linkpath, &parent, &name);
          if (r < 0) return r;
          wide_target = widen_n(target, -1);
          n = GetFinalPathNameByHandleW(parent, NULL, 0, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
          parent_path = malloc((n + 1) * sizeof(wchar_t));
          if (wide_target == NULL || parent_path == NULL || GetFinalPathNameByHandleW(parent, parent_path, n + 1, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS) == 0) {
              DWORD error = GetLastError();
              free(wide_target); free(parent_path); free(name); CloseHandle(parent);
              return fail_win32(error, "symlink", linkpath);
          }
          for (wchar_t *p = wide_target; *p; p++) if (*p == L'/') *p = L'\\';
          full = malloc((wcslen(parent_path) + wcslen(name) + 2) * sizeof(wchar_t));
          wcscpy(full, parent_path);
          wcscat(full, L"\\");
          wcscat(full, name);
          if (link_is_directory(root_fd, target, linkpath)) link_flags |= SYMBOLIC_LINK_FLAG_DIRECTORY;
          if (!CreateSymbolicLinkW(full, wide_target, link_flags)) {
              DWORD error = GetLastError();
              free(full); free(wide_target); free(parent_path); free(name); CloseHandle(parent);
              if (error == ERROR_PRIVILEGE_NOT_HELD)
                  return fail(EPERM, "symlink %s: creating symbolic links needs Developer Mode or administrator rights", linkpath);
              return fail_win32(error, "symlink", linkpath);
          }
          /* The link must have landed in the directory we resolved (not one swapped in meanwhile). */
          status = open_at(parent, name, wcslen(name), FILE_READ_ATTRIBUTES, NT_FILE_OPEN, 0, 0, &check);
          if (status >= 0) CloseHandle(check);
          free(full); free(wide_target); free(parent_path); free(name); CloseHandle(parent);
          return status < 0 ? fail(EAGAIN, "symlink %s: the parent directory changed during creation", linkpath) : 0;
      }

      /* ---- /proc ---- */

      int pathrs_procfs_open(const pathrs_procfs_open_how *args, size_t size)
      {
          (void)args; (void)size;
          return fail(ENOENT, "procfs: Windows has no /proc; pathrs_proc_* serve exe, cwd and fd/<n> of this process");
      }

      /* The Windows path behind /proc/<base>/<path>, for what Windows has: exe, cwd, fd/<n>. */
      static wchar_t *proc_target(pathrs_proc_base_t base, const char *path, int *error_id)
      {
          wchar_t *result = NULL;
          DWORD n;
          uint64_t type = base & __PATHRS_PROC_TYPE_MASK;
          *error_id = 0;
          if (path == NULL) { *error_id = fail(EINVAL, "proc: NULL path"); return NULL; }
          while (*path == '/') path++;
          if (base == PATHRS_PROC_ROOT) {
              char self[32];
              snprintf(self, sizeof(self), "%lu/", (unsigned long)GetCurrentProcessId());
              if (strncmp(path, "self/", 5) == 0 || strncmp(path, "thread-self/", 12) == 0) path = strchr(path, '/') + 1;
              else if (strncmp(path, self, strlen(self)) == 0) path += strlen(self);
              else { *error_id = fail(ENOENT, "proc %s: only this process is visible on Windows", path); return NULL; }
          }
          else if (type == __PATHRS_PROC_TYPE_PID && (DWORD)(base & 0xffffffffu) != GetCurrentProcessId()) {
              *error_id = fail(ENOENT, "proc %s: only this process is visible on Windows", path);
              return NULL;
          }
          if (strcmp(path, "exe") == 0) {
              result = malloc(32768 * sizeof(wchar_t));
              if (result != NULL && GetModuleFileNameW(NULL, result, 32768) > 0) return result;
          }
          else if (strcmp(path, "cwd") == 0) {
              n = GetCurrentDirectoryW(0, NULL);
              result = malloc((n + 1) * sizeof(wchar_t));
              if (result != NULL && GetCurrentDirectoryW(n + 1, result) > 0) return result;
          }
          else if (strncmp(path, "fd/", 3) == 0) {
              HANDLE handle = from_fd(atoi(path + 3));
              if (handle == INVALID_HANDLE_VALUE) { *error_id = fail(EBADF, "proc %s: bad file descriptor", path); return NULL; }
              n = GetFinalPathNameByHandleW(handle, NULL, 0, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
              result = malloc((n + 1) * sizeof(wchar_t));
              if (result != NULL && n > 0 && GetFinalPathNameByHandleW(handle, result, n + 1, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS) > 0) {
                  if (wcsncmp(result, L"\\\\?\\", 4) == 0) memmove(result, result + 4, (wcslen(result + 4) + 1) * sizeof(wchar_t));
                  return result;
              }
          }
          else {
              *error_id = fail(ENOENT, "proc %s: Windows has no /proc; only exe, cwd and fd/<n> exist", path);
              return NULL;
          }
          free(result);
          *error_id = fail_win32(GetLastError(), "proc", path);
          return NULL;
      }

      int pathrs_proc_openat(int proc_rootfd, pathrs_proc_base_t base, const char *path, uint64_t flags)
      {
          int error_id, fd;
          char *utf8;
          wchar_t *target;
          (void)proc_rootfd;
          target = proc_target(base, path, &error_id);
          if (target == NULL) return error_id;
          utf8 = narrow(target, -1);
          free(target);
          if (utf8 == NULL) return fail(ENOMEM, "out of memory");
          {
              HANDLE handle;
              wchar_t *wide = widen_n(utf8, -1);
              handle = CreateFileW(wide, access_for(flags) | SYNCHRONIZE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL,
                                   OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, NULL);
              free(wide);
              if (handle == INVALID_HANDLE_VALUE) { fd = fail_win32(GetLastError(), "proc open", utf8); free(utf8); return fd; }
              fd = finish_open(handle, flags, utf8);
          }
          free(utf8);
          return fd;
      }

      int pathrs_proc_open(pathrs_proc_base_t base, const char *path, uint64_t flags)
      {
          return pathrs_proc_openat(PATHRS_PROC_DEFAULT_ROOTFD, base, path, flags);
      }

      int pathrs_proc_readlinkat(int proc_rootfd, pathrs_proc_base_t base, const char *path, char *linkbuf, size_t linkbuf_size)
      {
          int error_id, n;
          char *utf8;
          wchar_t *target;
          (void)proc_rootfd;
          target = proc_target(base, path, &error_id);
          if (target == NULL) return error_id;
          utf8 = narrow(target, -1);
          free(target);
          if (utf8 == NULL) return fail(ENOMEM, "out of memory");
          n = (int)strlen(utf8);
          if (linkbuf != NULL) memcpy(linkbuf, utf8, (size_t)n < linkbuf_size ? (size_t)n : linkbuf_size);
          free(utf8);
          return n;
      }

      int pathrs_proc_readlink(pathrs_proc_base_t base, const char *path, char *linkbuf, size_t linkbuf_size)
      {
          return pathrs_proc_readlinkat(PATHRS_PROC_DEFAULT_ROOTFD, base, path, linkbuf, linkbuf_size);
      }
    C

    # open(2)/rename(2) flags the Windows C runtime doesn't define.
    inreplace "include/pathrs.h", "#endif  /* LIBPATHRS_H */", <<~'H'
      #ifdef _WIN32
      /*
       * Windows: open(2)/rename(2)/linkat(2) flags the C runtime lacks, with their libpathrs meaning.
       * File descriptors from libpathrs are never inherited by child processes, so O_CLOEXEC is implied.
       */
      #include <fcntl.h>
      #ifndef O_PATH
      #define O_PATH 0x01000000
      #endif
      #ifndef O_NOFOLLOW
      #define O_NOFOLLOW 0x02000000
      #endif
      #ifndef O_DIRECTORY
      #define O_DIRECTORY 0x04000000
      #endif
      #ifndef O_CLOEXEC
      #define O_CLOEXEC 0x08000000
      #endif
      #ifndef O_NOCTTY
      #define O_NOCTTY 0
      #endif
      #ifndef RENAME_NOREPLACE
      #define RENAME_NOREPLACE (1 << 0)
      #endif
      #ifndef RENAME_EXCHANGE
      #define RENAME_EXCHANGE (1 << 1)
      #endif
      #ifndef RENAME_WHITEOUT
      #define RENAME_WHITEOUT (1 << 2)
      #endif
      #ifndef AT_SYMLINK_FOLLOW
      #define AT_SYMLINK_FOLLOW 0x400
      #endif
      #endif /* _WIN32 */

      #endif  /* LIBPATHRS_H */
    H

    lib.mkpath
    bin.mkpath
    system ENV.cc, "-O2", "-shared", "-Iinclude", "-o", bin/"libpathrs.dll", "win/pathrs.c",
           "-Wl,--out-implib,#{lib}/libpathrs.dll.a"
    include.install "include/pathrs.h"
    (lib/"pkgconfig/pathrs.pc").write <<~PC
      prefix=${pcfiledir}/../..
      libdir=${prefix}/lib
      includedir=${prefix}/include

      Name: pathrs
      Description: Safe path resolution library (Windows)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -lpathrs
    PC
    doc.install "README.md"
  end

  test do
    (testpath/"root/sub").mkpath
    (testpath/"outside.txt").write "outside"
    (testpath/"test.c").write <<~C
      #include <pathrs.h>
      #include <fcntl.h>
      #include <stdio.h>
      #include <unistd.h>

      int main(int argc, char **argv) {
        int fd = pathrs_open_root(argv[1]);
        if (fd < 0) return 1;
        int resolved = pathrs_inroot_resolve(fd, ".");
        if (resolved < 0) return 1;
        close(resolved);
        /* ".." can't leave the root: ../outside.txt is looked up inside it. */
        int escaped = pathrs_inroot_open(fd, "sub/../../outside.txt", O_RDONLY);
        if (!IS_PATHRS_ERR(escaped)) return 2;
        pathrs_errorinfo_free(pathrs_errorinfo(escaped));
        int file = pathrs_inroot_creat(fd, "sub/new.txt", O_WRONLY, 0644);
        if (file < 0) return 3;
        write(file, "inside", 6);
        close(file);
        int dir = pathrs_inroot_mkdir_all(fd, "a/b/c", 0755);
        if (dir < 0) return 4;
        close(dir);
        if (pathrs_inroot_remove_all(fd, "a") < 0) return 5;
        close(fd);
        puts("ok");
        return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-lpathrs", "-o", "test.exe"
    cp bin/"libpathrs.dll", testpath
    assert_equal "ok", shell_output("./test.exe #{testpath}/root").strip
    assert_equal "inside", (testpath/"root/sub/new.txt").read
    refute_path_exists testpath/"root/a"
  end
end
