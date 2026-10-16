class Liburing < Formula
  desc "Helpers to setup and teardown io_uring instances"
  homepage "https://github.com/axboe/liburing"
  url "https://github.com/axboe/liburing/archive/refs/tags/liburing-2.15.tar.gz"
  sha256 "8d052f2622dcb3678cbaee5ff582a87572672a6c0a56533cdda5b65cb636120a"
  license any_of: ["MIT", "LGPL-2.1-only"]

  # io_uring is a Linux kernel interface; Windows' counterpart is the I/O ring (ioringapi.h,
  # Windows 11 and Server 2022+), the same submission/completion-queue design. The liburing.h
  # below implements liburing's API on it (header-only; -luring links an empty archive so build
  # scripts written for Linux still link). The man pages are upstream's.

  livecheck do
    url :stable
    regex(/^liburing[._-]v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    (include/"liburing.h").write <<~'C'
      /* liburing for Windows: the io_uring submission/completion API on Windows I/O rings (ioringapi,
       * Windows 11 and Windows Server 2022 or later). Header-only; -luring links an empty library.
       *
       * Reads and writes (plain and fixed-buffer), fsync and cancel go to the kernel I/O ring; nop,
       * readv/writev, openat, close and timeouts complete in user mode at submit or wait time, as
       * io_uring allows. Other opcodes complete with -EOPNOTSUPP. File descriptors are C runtime fds
       * (_open, _fileno); fixed files and buffers map onto the ring's registered handles and buffers.
       * IOSQE_IO_LINK/IO_HARDLINK order the chain (like IO_DRAIN) but a failure doesn't cancel the rest.
       * On Windows without I/O rings, io_uring_queue_init returns -ENOSYS. */
      #ifndef LIBURING_H
      #define LIBURING_H

      #include <errno.h>
      #include <fcntl.h>
      #include <io.h>
      #include <stdint.h>
      #include <stdlib.h>
      #include <string.h>
      #include <windows.h>

      #ifdef __cplusplus
      extern "C" {
      #endif

      #define IO_URING_VERSION_MAJOR 2
      #define IO_URING_VERSION_MINOR 15

      #ifndef _STRUCT_IOVEC
      #define _STRUCT_IOVEC
      struct iovec {
          void *iov_base;
          size_t iov_len;
      };
      #endif

      struct __kernel_timespec {
          int64_t tv_sec;
          long long tv_nsec;
      };

      #ifndef AT_FDCWD
      #define AT_FDCWD -100
      #endif
      #ifndef ETIME
      #define ETIME 62
      #endif

      /* io_uring_setup flags: accepted; the I/O ring has no equivalents. */
      #define IORING_SETUP_IOPOLL (1U << 0)
      #define IORING_SETUP_SQPOLL (1U << 1)
      #define IORING_SETUP_SQ_AFF (1U << 2)
      #define IORING_SETUP_CQSIZE (1U << 3)
      #define IORING_SETUP_CLAMP (1U << 4)
      #define IORING_SETUP_SINGLE_ISSUER (1U << 12)

      #define IOSQE_FIXED_FILE (1U << 0)
      #define IOSQE_IO_DRAIN (1U << 1)
      #define IOSQE_IO_LINK (1U << 2)
      #define IOSQE_IO_HARDLINK (1U << 3)
      #define IOSQE_ASYNC (1U << 4)
      #define IOSQE_BUFFER_SELECT (1U << 5)
      #define IOSQE_CQE_SKIP_SUCCESS (1U << 6)

      #define IORING_FSYNC_DATASYNC (1U << 0)
      #define IORING_TIMEOUT_ABS (1U << 0)

      enum io_uring_op {
          IORING_OP_NOP, IORING_OP_READV, IORING_OP_WRITEV, IORING_OP_FSYNC, IORING_OP_READ_FIXED,
          IORING_OP_WRITE_FIXED, IORING_OP_POLL_ADD, IORING_OP_POLL_REMOVE, IORING_OP_SYNC_FILE_RANGE,
          IORING_OP_SENDMSG, IORING_OP_RECVMSG, IORING_OP_TIMEOUT, IORING_OP_TIMEOUT_REMOVE,
          IORING_OP_ACCEPT, IORING_OP_ASYNC_CANCEL, IORING_OP_LINK_TIMEOUT, IORING_OP_CONNECT,
          IORING_OP_FALLOCATE, IORING_OP_OPENAT, IORING_OP_CLOSE, IORING_OP_FILES_UPDATE,
          IORING_OP_STATX, IORING_OP_READ, IORING_OP_WRITE, IORING_OP_LAST
      };

      struct io_uring_sqe {
          uint8_t opcode;
          uint8_t flags;
          uint16_t ioprio;
          int32_t fd;
          union { uint64_t off; uint64_t addr2; };
          union { uint64_t addr; uint64_t splice_off_in; };
          uint32_t len;
          union {
              uint32_t rw_flags;
              uint32_t fsync_flags;
              uint32_t timeout_flags;
              uint32_t cancel_flags;
              uint32_t open_flags;
          };
          uint64_t user_data;
          uint16_t buf_index;
          uint16_t personality;
          union { int32_t splice_fd_in; uint32_t file_index; };
          uint64_t addr3;
          uint64_t __pad2[1];
      };

      struct io_uring_cqe {
          uint64_t user_data;
          int32_t res;
          uint32_t flags;
      };

      struct io_uring_params {
          uint32_t sq_entries;
          uint32_t cq_entries;
          uint32_t flags;
          uint32_t sq_thread_cpu;
          uint32_t sq_thread_idle;
          uint32_t features;
          uint32_t wq_fd;
          uint32_t resv[3];
      };

      /* ---- private: the I/O ring API, resolved at run time (layouts as in ioringapi.h) ---- */

      typedef struct { int Kind; union { HANDLE Handle; UINT32 Index; } u; } liburing__handle_ref;
      typedef struct { int Kind; union { void *Address; struct { UINT32 Index, Offset; } Registered; } u; } liburing__buffer_ref;
      typedef struct { UINT_PTR UserData; HRESULT ResultCode; ULONG_PTR Information; } liburing__cqe;
      typedef struct { int MaxVersion; UINT32 MaxSubmissionQueueSize, MaxCompletionQueueSize; int FeatureFlags; } liburing__caps;
      typedef struct { void *Address; UINT32 Length; } liburing__buffer_info;

      struct liburing__api {
          int loaded;
          HRESULT (WINAPI *QueryCaps)(liburing__caps *);
          HRESULT (WINAPI *Create)(int, UINT64, UINT32, UINT32, void **);
          HRESULT (WINAPI *Close)(void *);
          HRESULT (WINAPI *Submit)(void *, UINT32, UINT32, UINT32 *);
          HRESULT (WINAPI *Pop)(void *, liburing__cqe *);
          HRESULT (WINAPI *SetEvent)(void *, HANDLE);
          HRESULT (WINAPI *Cancel)(void *, liburing__handle_ref, UINT_PTR, UINT_PTR);
          HRESULT (WINAPI *Read)(void *, liburing__handle_ref, liburing__buffer_ref, UINT32, UINT64, UINT_PTR, int);
          HRESULT (WINAPI *Write)(void *, liburing__handle_ref, liburing__buffer_ref, UINT32, UINT64, int, UINT_PTR, int);
          HRESULT (WINAPI *Flush)(void *, liburing__handle_ref, int, UINT_PTR, int);
          HRESULT (WINAPI *RegisterFiles)(void *, UINT32, HANDLE const *, UINT_PTR);
          HRESULT (WINAPI *RegisterBuffers)(void *, UINT32, liburing__buffer_info const *, UINT_PTR);
      };

      static inline struct liburing__api *liburing__get_api(void)
      {
          static struct liburing__api api;
          if (!api.loaded) {
              HMODULE kb = GetModuleHandleW(L"kernelbase.dll");
      #define LIBURING__SYM(field, name) *(FARPROC *)&api.field = kb ? GetProcAddress(kb, name) : NULL
              LIBURING__SYM(QueryCaps, "QueryIoRingCapabilities");
              LIBURING__SYM(Create, "CreateIoRing");
              LIBURING__SYM(Close, "CloseIoRing");
              LIBURING__SYM(Submit, "SubmitIoRing");
              LIBURING__SYM(Pop, "PopIoRingCompletion");
              LIBURING__SYM(SetEvent, "SetIoRingCompletionEvent");
              LIBURING__SYM(Cancel, "BuildIoRingCancelRequest");
              LIBURING__SYM(Read, "BuildIoRingReadFile");
              LIBURING__SYM(Write, "BuildIoRingWriteFile");
              LIBURING__SYM(Flush, "BuildIoRingFlushFile");
              LIBURING__SYM(RegisterFiles, "BuildIoRingRegisterFileHandles");
              LIBURING__SYM(RegisterBuffers, "BuildIoRingRegisterBuffers");
      #undef LIBURING__SYM
              api.loaded = 1;
          }
          return &api;
      }

      #define LIBURING__UDATA_INTERNAL ((uint64_t)-2)

      struct liburing__inflight { uint64_t user_data; HANDLE handle; int used; };
      struct liburing__timeout { uint64_t user_data; ULONGLONG deadline; int used; };

      struct io_uring {
          void *ioring;
          HANDLE event;
          unsigned flags;
          unsigned sq_entries, sq_head, sq_tail;
          struct io_uring_sqe *sqes;
          unsigned cq_entries, cq_head, cq_tail;
          struct io_uring_cqe *cqes;
          struct liburing__inflight *inflight;
          unsigned nr_inflight;
          struct liburing__timeout *timeouts;
          void **buffers;
          unsigned nr_buffers;
          int internal_res, internal_done;
          int chained;
          int version;
      };

      static inline int liburing__errno(HRESULT hr)
      {
          DWORD code = HRESULT_FACILITY(hr) == FACILITY_WIN32 ? HRESULT_CODE(hr) : 0;
          if (hr == E_OUTOFMEMORY || code == ERROR_NOT_ENOUGH_MEMORY) return ENOMEM;
          if (hr == E_INVALIDARG || code == ERROR_INVALID_PARAMETER) return EINVAL;
          if (hr == E_NOTIMPL || code == ERROR_NOT_SUPPORTED || code == ERROR_CALL_NOT_IMPLEMENTED) return EOPNOTSUPP;
          switch (code) {
          case ERROR_INVALID_HANDLE: return EBADF;
          case ERROR_ACCESS_DENIED: return EACCES;
          case ERROR_FILE_NOT_FOUND: case ERROR_PATH_NOT_FOUND: return ENOENT;
          case ERROR_OPERATION_ABORTED: return ECANCELED;
          case ERROR_DISK_FULL: case ERROR_HANDLE_DISK_FULL: return ENOSPC;
          case ERROR_IO_PENDING: return EAGAIN;
          case ERROR_NOT_FOUND: return ENOENT;
          default: return EIO;
          }
      }

      static inline unsigned liburing__pow2(unsigned n)
      {
          unsigned p = 1;
          while (p < n) p <<= 1;
          return p;
      }

      /* Queue a completion that happened in user mode. */
      static inline int liburing__post(struct io_uring *ring, uint64_t user_data, int res)
      {
          if (user_data == LIBURING__UDATA_INTERNAL) {
              ring->internal_res = res;
              ring->internal_done = 1;
              return 0;
          }
          if (ring->cq_tail - ring->cq_head >= ring->cq_entries) return -EBUSY;
          struct io_uring_cqe *cqe = &ring->cqes[ring->cq_tail & (ring->cq_entries - 1)];
          cqe->user_data = user_data;
          cqe->res = res;
          cqe->flags = 0;
          ring->cq_tail++;
          return 0;
      }

      /* Move kernel completions into the local completion queue (and expire timeouts). */
      static inline void liburing__reap(struct io_uring *ring)
      {
          struct liburing__api *api = liburing__get_api();
          liburing__cqe c;
          while (ring->cq_tail - ring->cq_head < ring->cq_entries && api->Pop(ring->ioring, &c) == S_OK) {
              int res;
              if (SUCCEEDED(c.ResultCode)) res = (int)c.Information;
              else if (HRESULT_CODE(c.ResultCode) == ERROR_HANDLE_EOF) res = 0;
              else res = -liburing__errno(c.ResultCode);
              for (unsigned i = 0; i < ring->cq_entries; i++)
                  if (ring->inflight[i].used && ring->inflight[i].user_data == (uint64_t)c.UserData) {
                      ring->inflight[i].used = 0;
                      break;
                  }
              if (ring->nr_inflight) ring->nr_inflight--;
              liburing__post(ring, (uint64_t)c.UserData, res);
          }
          ULONGLONG now = GetTickCount64();
          for (unsigned i = 0; i < ring->sq_entries; i++)
              if (ring->timeouts[i].used && ring->timeouts[i].deadline <= now
                  && liburing__post(ring, ring->timeouts[i].user_data, -ETIME) == 0)
                  ring->timeouts[i].used = 0;
      }

      static inline void liburing__track(struct io_uring *ring, uint64_t user_data, HANDLE handle)
      {
          for (unsigned i = 0; i < ring->cq_entries; i++)
              if (!ring->inflight[i].used) {
                  ring->inflight[i].user_data = user_data;
                  ring->inflight[i].handle = handle;
                  ring->inflight[i].used = 1;
                  break;
              }
          ring->nr_inflight++;
      }

      static inline int liburing__rw_sync(HANDLE h, int write, const struct iovec *iov, unsigned n, uint64_t off)
      {
          long long total = 0;
          LARGE_INTEGER pos;
          if (off == (uint64_t)-1) {
              LARGE_INTEGER zero;
              zero.QuadPart = 0;
              if (!SetFilePointerEx(h, zero, &pos, FILE_CURRENT)) return -EBADF;
              off = (uint64_t)pos.QuadPart;
          }
          for (unsigned i = 0; i < n; i++) {
              OVERLAPPED ov;
              DWORD done = 0;
              memset(&ov, 0, sizeof ov);
              ov.Offset = (DWORD)(off + total);
              ov.OffsetHigh = (DWORD)((off + total) >> 32);
              ov.hEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
              BOOL ok = write ? WriteFile(h, iov[i].iov_base, (DWORD)iov[i].iov_len, NULL, &ov)
                              : ReadFile(h, iov[i].iov_base, (DWORD)iov[i].iov_len, NULL, &ov);
              if (ok || GetLastError() == ERROR_IO_PENDING) ok = GetOverlappedResult(h, &ov, &done, TRUE);
              DWORD err = ok ? 0 : GetLastError();
              CloseHandle(ov.hEvent);
              if (!ok) {
                  if (err == ERROR_HANDLE_EOF) break;
                  return total ? (int)total : -liburing__errno(HRESULT_FROM_WIN32(err));
              }
              total += done;
              if (done < iov[i].iov_len) break;
          }
          return (int)total;
      }

      static inline HANDLE liburing__handle(int fd)
      {
          intptr_t h = _get_osfhandle(fd);
          return h == -1 ? NULL : (HANDLE)h;
      }

      /* Hand one submission queue entry to the I/O ring, or complete it here. */
      static inline void liburing__issue(struct io_uring *ring, struct io_uring_sqe *sqe, int sqe_flags)
      {
          struct liburing__api *api = liburing__get_api();
          uint64_t ud = sqe->user_data;
          int fixed_file = (sqe->flags & IOSQE_FIXED_FILE) != 0;
          liburing__handle_ref file;
          HANDLE h = NULL;
          HRESULT hr;
          memset(&file, 0, sizeof file);
          if (fixed_file) {
              file.Kind = 1;
              file.u.Index = (UINT32)sqe->fd;
          } else if (sqe->opcode != IORING_OP_NOP && sqe->opcode != IORING_OP_OPENAT && sqe->opcode != IORING_OP_TIMEOUT
                     && sqe->opcode != IORING_OP_ASYNC_CANCEL) {
              h = liburing__handle(sqe->fd);
              if (!h) { liburing__post(ring, ud, -EBADF); return; }
              file.Kind = 0;
              file.u.Handle = h;
          }

          switch (sqe->opcode) {
          case IORING_OP_NOP:
              liburing__post(ring, ud, 0);
              return;
          case IORING_OP_READ:
          case IORING_OP_WRITE:
          case IORING_OP_READ_FIXED:
          case IORING_OP_WRITE_FIXED: {
              int write = sqe->opcode == IORING_OP_WRITE || sqe->opcode == IORING_OP_WRITE_FIXED;
              liburing__buffer_ref buf;
              uint64_t off = sqe->off;
              memset(&buf, 0, sizeof buf);
              if (sqe->opcode == IORING_OP_READ_FIXED || sqe->opcode == IORING_OP_WRITE_FIXED) {
                  if (sqe->buf_index >= ring->nr_buffers) { liburing__post(ring, ud, -EFAULT); return; }
                  buf.Kind = 1;
                  buf.u.Registered.Index = sqe->buf_index;
                  buf.u.Registered.Offset = (UINT32)((char *)(uintptr_t)sqe->addr - (char *)ring->buffers[sqe->buf_index]);
              } else {
                  buf.Kind = 0;
                  buf.u.Address = (void *)(uintptr_t)sqe->addr;
              }
              if (off == (uint64_t)-1 && h) {
                  LARGE_INTEGER zero, pos;
                  zero.QuadPart = 0;
                  if (SetFilePointerEx(h, zero, &pos, FILE_CURRENT)) off = (uint64_t)pos.QuadPart;
              }
              if (write && !api->Write) {
                  struct iovec v = { (void *)(uintptr_t)sqe->addr, sqe->len };
                  liburing__post(ring, ud, fixed_file ? -EOPNOTSUPP : liburing__rw_sync(h, 1, &v, 1, off));
                  return;
              }
              hr = write ? api->Write(ring->ioring, file, buf, sqe->len, off, 0, (UINT_PTR)ud, sqe_flags)
                         : api->Read(ring->ioring, file, buf, sqe->len, off, (UINT_PTR)ud, sqe_flags);
              break;
          }
          case IORING_OP_READV:
          case IORING_OP_WRITEV:
              if (fixed_file) { liburing__post(ring, ud, -EOPNOTSUPP); return; }
              liburing__post(ring, ud, liburing__rw_sync(h, sqe->opcode == IORING_OP_WRITEV,
                                                         (const struct iovec *)(uintptr_t)sqe->addr, sqe->len, sqe->off));
              return;
          case IORING_OP_FSYNC:
              if (!api->Flush) {
                  liburing__post(ring, ud, fixed_file ? -EOPNOTSUPP : FlushFileBuffers(h) ? 0 : -EIO);
                  return;
              }
              hr = api->Flush(ring->ioring, file, (sqe->fsync_flags & IORING_FSYNC_DATASYNC) ? 1 : 0, (UINT_PTR)ud, sqe_flags);
              break;
          case IORING_OP_OPENAT: {
              int fd = _open((const char *)(uintptr_t)sqe->addr, (int)sqe->open_flags | _O_BINARY, (int)sqe->len);
              liburing__post(ring, ud, fd >= 0 ? fd : -errno);
              return;
          }
          case IORING_OP_CLOSE:
              liburing__post(ring, ud, _close(sqe->fd) == 0 ? 0 : -errno);
              return;
          case IORING_OP_TIMEOUT: {
              const struct __kernel_timespec *ts = (const struct __kernel_timespec *)(uintptr_t)sqe->addr;
              ULONGLONG ms = (ULONGLONG)ts->tv_sec * 1000 + (ULONGLONG)(ts->tv_nsec / 1000000);
              if (sqe->timeout_flags & IORING_TIMEOUT_ABS) {
                  /* Absolute timeouts are CLOCK_MONOTONIC; GetTickCount64 is the closest clock. */
                  ULONGLONG now = GetTickCount64();
                  ms = ms > now ? ms - now : 0;
              }
              for (unsigned i = 0; i < ring->sq_entries; i++)
                  if (!ring->timeouts[i].used) {
                      ring->timeouts[i].used = 1;
                      ring->timeouts[i].user_data = ud;
                      ring->timeouts[i].deadline = GetTickCount64() + ms;
                      return;
                  }
              liburing__post(ring, ud, -EBUSY);
              return;
          }
          case IORING_OP_ASYNC_CANCEL: {
              uint64_t target = sqe->addr;
              for (unsigned i = 0; i < ring->sq_entries; i++)
                  if (ring->timeouts[i].used && ring->timeouts[i].user_data == target) {
                      ring->timeouts[i].used = 0;
                      liburing__post(ring, target, -ECANCELED);
                      liburing__post(ring, ud, 0);
                      return;
                  }
              for (unsigned i = 0; i < ring->cq_entries; i++)
                  if (ring->inflight[i].used && ring->inflight[i].user_data == target) {
                      liburing__handle_ref ref;
                      memset(&ref, 0, sizeof ref);
                      ref.u.Handle = ring->inflight[i].handle;
                      hr = api->Cancel(ring->ioring, ref, (UINT_PTR)target, (UINT_PTR)ud);
                      if (FAILED(hr)) { liburing__post(ring, ud, -liburing__errno(hr)); return; }
                      liburing__track(ring, ud, NULL);
                      return;
                  }
              liburing__post(ring, ud, -ENOENT);
              return;
          }
          default:
              liburing__post(ring, ud, -EOPNOTSUPP);
              return;
          }
          if (FAILED(hr)) liburing__post(ring, ud, -liburing__errno(hr));
          else liburing__track(ring, ud, h);
      }

      /* ---- setup ---- */

      static inline int io_uring_queue_init_params(unsigned entries, struct io_uring *ring, struct io_uring_params *p)
      {
          struct liburing__api *api = liburing__get_api();
          liburing__caps caps;
          unsigned cq;
          HRESULT hr;
          memset(ring, 0, sizeof *ring);
          if (!api->Create || !api->QueryCaps || FAILED(api->QueryCaps(&caps))) return -ENOSYS;
          if (entries == 0) return -EINVAL;
          entries = liburing__pow2(entries);
          if (entries > caps.MaxSubmissionQueueSize) {
              if (!(p->flags & IORING_SETUP_CLAMP)) return -EINVAL;
              entries = caps.MaxSubmissionQueueSize;
          }
          cq = (p->flags & IORING_SETUP_CQSIZE) && p->cq_entries ? liburing__pow2(p->cq_entries) : entries * 2;
          if (cq > caps.MaxCompletionQueueSize) cq = caps.MaxCompletionQueueSize;
          hr = api->Create(caps.MaxVersion >= 300 ? 300 : caps.MaxVersion, 0, entries, cq, &ring->ioring);
          if (FAILED(hr)) return -liburing__errno(hr);
          ring->flags = p->flags;
          ring->version = caps.MaxVersion >= 300 ? 300 : caps.MaxVersion;
          ring->sq_entries = entries;
          ring->cq_entries = cq;
          ring->sqes = (struct io_uring_sqe *)calloc(entries, sizeof *ring->sqes);
          ring->cqes = (struct io_uring_cqe *)calloc(cq, sizeof *ring->cqes);
          ring->inflight = (struct liburing__inflight *)calloc(cq, sizeof *ring->inflight);
          ring->timeouts = (struct liburing__timeout *)calloc(entries, sizeof *ring->timeouts);
          if (!ring->sqes || !ring->cqes || !ring->inflight || !ring->timeouts) {
              api->Close(ring->ioring);
              free(ring->sqes); free(ring->cqes); free(ring->inflight); free(ring->timeouts);
              return -ENOMEM;
          }
          ring->event = CreateEventW(NULL, FALSE, FALSE, NULL);
          if (ring->event && (!api->SetEvent || FAILED(api->SetEvent(ring->ioring, ring->event)))) {
              CloseHandle(ring->event);
              ring->event = NULL;
          }
          p->sq_entries = entries;
          p->cq_entries = cq;
          p->features = 0;
          return 0;
      }

      static inline int io_uring_queue_init(unsigned entries, struct io_uring *ring, unsigned flags)
      {
          struct io_uring_params p;
          memset(&p, 0, sizeof p);
          p.flags = flags;
          return io_uring_queue_init_params(entries, ring, &p);
      }

      static inline void io_uring_queue_exit(struct io_uring *ring)
      {
          if (!ring->ioring) return;
          liburing__get_api()->Close(ring->ioring);
          if (ring->event) CloseHandle(ring->event);
          free(ring->sqes); free(ring->cqes); free(ring->inflight); free(ring->timeouts); free(ring->buffers);
          memset(ring, 0, sizeof *ring);
      }

      /* ---- submission ---- */

      static inline struct io_uring_sqe *io_uring_get_sqe(struct io_uring *ring)
      {
          if (ring->sq_tail - ring->sq_head >= ring->sq_entries) return NULL;
          struct io_uring_sqe *sqe = &ring->sqes[ring->sq_tail & (ring->sq_entries - 1)];
          ring->sq_tail++;
          memset(sqe, 0, sizeof *sqe);
          return sqe;
      }

      static inline unsigned io_uring_sq_ready(const struct io_uring *ring) { return ring->sq_tail - ring->sq_head; }
      static inline unsigned io_uring_sq_space_left(const struct io_uring *ring) { return ring->sq_entries - io_uring_sq_ready(ring); }

      static inline int io_uring_submit(struct io_uring *ring)
      {
          struct liburing__api *api = liburing__get_api();
          int count = 0;
          UINT32 submitted = 0;
          while (ring->sq_head != ring->sq_tail) {
              struct io_uring_sqe *sqe = &ring->sqes[ring->sq_head & (ring->sq_entries - 1)];
              /* Draining, and following a link, both wait for the operations queued before. */
              int flags = ring->version >= 300 && ((sqe->flags & IOSQE_IO_DRAIN) || ring->chained) ? 1 : 0;
              ring->chained = (sqe->flags & (IOSQE_IO_LINK | IOSQE_IO_HARDLINK)) != 0;
              liburing__issue(ring, sqe, flags);
              ring->sq_head++;
              count++;
          }
          if (count && ring->ioring) {
              HRESULT hr = api->Submit(ring->ioring, 0, 0, &submitted);
              if (FAILED(hr)) return -liburing__errno(hr);
          }
          return count;
      }

      /* Wait until at least wait_nr completions are queued, or the deadline (GetTickCount64) passes. */
      static inline int liburing__wait(struct io_uring *ring, unsigned wait_nr, ULONGLONG deadline)
      {
          for (;;) {
              liburing__reap(ring);
              if (ring->cq_tail - ring->cq_head >= wait_nr) return 0;
              ULONGLONG now = GetTickCount64(), until = deadline;
              int timers = 0;
              for (unsigned i = 0; i < ring->sq_entries; i++)
                  if (ring->timeouts[i].used) {
                      timers = 1;
                      if (ring->timeouts[i].deadline < until) until = ring->timeouts[i].deadline;
                  }
              if (!ring->nr_inflight && !timers && deadline == (ULONGLONG)-1) return -EAGAIN;
              if (now >= deadline) return -ETIME;
              DWORD ms = until == (ULONGLONG)-1 ? INFINITE : (DWORD)(until > now ? until - now : 0);
              if (ring->event) {
                  if (ring->nr_inflight) WaitForSingleObject(ring->event, ms);
                  else Sleep(ms);
              } else Sleep(ms < 1 ? 0 : 1);
          }
      }

      static inline int io_uring_submit_and_wait(struct io_uring *ring, unsigned wait_nr)
      {
          int n = io_uring_submit(ring);
          if (n < 0 || wait_nr == 0) return n;
          int r = liburing__wait(ring, wait_nr, (ULONGLONG)-1);
          return r < 0 ? r : n;
      }

      /* ---- completion ---- */

      static inline unsigned io_uring_cq_ready(const struct io_uring *ring) { return ring->cq_tail - ring->cq_head; }

      static inline int io_uring_peek_cqe(struct io_uring *ring, struct io_uring_cqe **cqe_ptr)
      {
          if (ring->cq_head == ring->cq_tail) liburing__reap(ring);
          if (ring->cq_head == ring->cq_tail) { *cqe_ptr = NULL; return -EAGAIN; }
          *cqe_ptr = &ring->cqes[ring->cq_head & (ring->cq_entries - 1)];
          return 0;
      }

      static inline int io_uring_wait_cqe_nr(struct io_uring *ring, struct io_uring_cqe **cqe_ptr, unsigned wait_nr)
      {
          int r = liburing__wait(ring, wait_nr ? wait_nr : 1, (ULONGLONG)-1);
          if (r < 0) { *cqe_ptr = NULL; return r; }
          return io_uring_peek_cqe(ring, cqe_ptr);
      }

      static inline int io_uring_wait_cqe(struct io_uring *ring, struct io_uring_cqe **cqe_ptr)
      {
          return io_uring_wait_cqe_nr(ring, cqe_ptr, 1);
      }

      static inline int io_uring_wait_cqe_timeout(struct io_uring *ring, struct io_uring_cqe **cqe_ptr, struct __kernel_timespec *ts)
      {
          ULONGLONG deadline = ts ? GetTickCount64() + (ULONGLONG)ts->tv_sec * 1000 + (ULONGLONG)(ts->tv_nsec / 1000000) : (ULONGLONG)-1;
          int r = liburing__wait(ring, 1, deadline);
          if (r < 0) { *cqe_ptr = NULL; return r; }
          return io_uring_peek_cqe(ring, cqe_ptr);
      }

      static inline int io_uring_submit_and_wait_timeout(struct io_uring *ring, struct io_uring_cqe **cqe_ptr, unsigned wait_nr,
                                                         struct __kernel_timespec *ts, void *sigmask)
      {
          (void)sigmask;
          int n = io_uring_submit(ring);
          if (n < 0) return n;
          ULONGLONG deadline = ts ? GetTickCount64() + (ULONGLONG)ts->tv_sec * 1000 + (ULONGLONG)(ts->tv_nsec / 1000000) : (ULONGLONG)-1;
          int r = liburing__wait(ring, wait_nr ? wait_nr : 1, deadline);
          if (r < 0) { *cqe_ptr = NULL; return r; }
          return io_uring_peek_cqe(ring, cqe_ptr);
      }

      static inline unsigned io_uring_peek_batch_cqe(struct io_uring *ring, struct io_uring_cqe **cqes, unsigned count)
      {
          unsigned n = 0;
          liburing__reap(ring);
          while (n < count && ring->cq_head + n != ring->cq_tail) {
              cqes[n] = &ring->cqes[(ring->cq_head + n) & (ring->cq_entries - 1)];
              n++;
          }
          return n;
      }

      static inline void io_uring_cq_advance(struct io_uring *ring, unsigned nr) { ring->cq_head += nr; }
      static inline void io_uring_cqe_seen(struct io_uring *ring, struct io_uring_cqe *cqe) { if (cqe) ring->cq_head++; }

      #define io_uring_for_each_cqe(ring, head, cqe) \
          for (head = (ring)->cq_head; \
               (cqe = (head != (ring)->cq_tail ? &(ring)->cqes[head & ((ring)->cq_entries - 1)] : NULL)); head++)

      /* ---- registration (synchronous, like io_uring_register) ---- */

      static inline int liburing__run_internal(struct io_uring *ring, HRESULT hr)
      {
          if (FAILED(hr)) return -liburing__errno(hr);
          ring->internal_done = 0;
          liburing__track(ring, LIBURING__UDATA_INTERNAL, NULL);
          hr = liburing__get_api()->Submit(ring->ioring, 0, 0, NULL);
          if (FAILED(hr)) return -liburing__errno(hr);
          while (!ring->internal_done) {
              liburing__reap(ring);
              if (!ring->internal_done) {
                  if (ring->event) WaitForSingleObject(ring->event, 10);
                  else Sleep(1);
              }
          }
          return ring->internal_res < 0 ? ring->internal_res : 0;
      }

      static inline int io_uring_register_buffers(struct io_uring *ring, const struct iovec *iovecs, unsigned nr_iovecs)
      {
          liburing__buffer_info *info = (liburing__buffer_info *)calloc(nr_iovecs ? nr_iovecs : 1, sizeof *info);
          void **bases = (void **)calloc(nr_iovecs ? nr_iovecs : 1, sizeof *bases);
          if (!info || !bases) { free(info); free(bases); return -ENOMEM; }
          for (unsigned i = 0; i < nr_iovecs; i++) {
              info[i].Address = iovecs[i].iov_base;
              info[i].Length = (UINT32)iovecs[i].iov_len;
              bases[i] = iovecs[i].iov_base;
          }
          int r = liburing__run_internal(ring, liburing__get_api()->RegisterBuffers(ring->ioring, nr_iovecs, info, (UINT_PTR)LIBURING__UDATA_INTERNAL));
          free(info);
          if (r < 0) { free(bases); return r; }
          free(ring->buffers);
          ring->buffers = bases;
          ring->nr_buffers = nr_iovecs;
          return 0;
      }

      static inline int io_uring_unregister_buffers(struct io_uring *ring)
      {
          int r = liburing__run_internal(ring, liburing__get_api()->RegisterBuffers(ring->ioring, 0, NULL, (UINT_PTR)LIBURING__UDATA_INTERNAL));
          free(ring->buffers);
          ring->buffers = NULL;
          ring->nr_buffers = 0;
          return r;
      }

      static inline int io_uring_register_files(struct io_uring *ring, const int *files, unsigned nr_files)
      {
          HANDLE *handles = (HANDLE *)calloc(nr_files ? nr_files : 1, sizeof *handles);
          if (!handles) return -ENOMEM;
          for (unsigned i = 0; i < nr_files; i++) handles[i] = files[i] < 0 ? NULL : liburing__handle(files[i]);
          int r = liburing__run_internal(ring, liburing__get_api()->RegisterFiles(ring->ioring, nr_files, handles, (UINT_PTR)LIBURING__UDATA_INTERNAL));
          free(handles);
          return r;
      }

      static inline int io_uring_unregister_files(struct io_uring *ring)
      {
          return liburing__run_internal(ring, liburing__get_api()->RegisterFiles(ring->ioring, 0, NULL, (UINT_PTR)LIBURING__UDATA_INTERNAL));
      }

      /* ---- request preparation ---- */

      static inline void io_uring_sqe_set_data(struct io_uring_sqe *sqe, void *data) { sqe->user_data = (uint64_t)(uintptr_t)data; }
      static inline void io_uring_sqe_set_data64(struct io_uring_sqe *sqe, uint64_t data) { sqe->user_data = data; }
      static inline void *io_uring_cqe_get_data(const struct io_uring_cqe *cqe) { return (void *)(uintptr_t)cqe->user_data; }
      static inline uint64_t io_uring_cqe_get_data64(const struct io_uring_cqe *cqe) { return cqe->user_data; }
      static inline void io_uring_sqe_set_flags(struct io_uring_sqe *sqe, unsigned flags) { sqe->flags = (uint8_t)flags; }

      static inline void io_uring_prep_rw(int op, struct io_uring_sqe *sqe, int fd, const void *addr, unsigned len, uint64_t offset)
      {
          sqe->opcode = (uint8_t)op;
          sqe->fd = fd;
          sqe->off = offset;
          sqe->addr = (uint64_t)(uintptr_t)addr;
          sqe->len = len;
      }

      static inline void io_uring_prep_nop(struct io_uring_sqe *sqe) { io_uring_prep_rw(IORING_OP_NOP, sqe, -1, NULL, 0, 0); }
      static inline void io_uring_prep_read(struct io_uring_sqe *sqe, int fd, void *buf, unsigned nbytes, uint64_t offset)
      { io_uring_prep_rw(IORING_OP_READ, sqe, fd, buf, nbytes, offset); }
      static inline void io_uring_prep_write(struct io_uring_sqe *sqe, int fd, const void *buf, unsigned nbytes, uint64_t offset)
      { io_uring_prep_rw(IORING_OP_WRITE, sqe, fd, buf, nbytes, offset); }
      static inline void io_uring_prep_readv(struct io_uring_sqe *sqe, int fd, const struct iovec *iovecs, unsigned nr_vecs, uint64_t offset)
      { io_uring_prep_rw(IORING_OP_READV, sqe, fd, iovecs, nr_vecs, offset); }
      static inline void io_uring_prep_writev(struct io_uring_sqe *sqe, int fd, const struct iovec *iovecs, unsigned nr_vecs, uint64_t offset)
      { io_uring_prep_rw(IORING_OP_WRITEV, sqe, fd, iovecs, nr_vecs, offset); }
      static inline void io_uring_prep_read_fixed(struct io_uring_sqe *sqe, int fd, void *buf, unsigned nbytes, uint64_t offset, int buf_index)
      { io_uring_prep_rw(IORING_OP_READ_FIXED, sqe, fd, buf, nbytes, offset); sqe->buf_index = (uint16_t)buf_index; }
      static inline void io_uring_prep_write_fixed(struct io_uring_sqe *sqe, int fd, const void *buf, unsigned nbytes, uint64_t offset, int buf_index)
      { io_uring_prep_rw(IORING_OP_WRITE_FIXED, sqe, fd, buf, nbytes, offset); sqe->buf_index = (uint16_t)buf_index; }
      static inline void io_uring_prep_fsync(struct io_uring_sqe *sqe, int fd, unsigned fsync_flags)
      { io_uring_prep_rw(IORING_OP_FSYNC, sqe, fd, NULL, 0, 0); sqe->fsync_flags = fsync_flags; }
      static inline void io_uring_prep_close(struct io_uring_sqe *sqe, int fd) { io_uring_prep_rw(IORING_OP_CLOSE, sqe, fd, NULL, 0, 0); }
      static inline void io_uring_prep_openat(struct io_uring_sqe *sqe, int dfd, const char *path, int flags, unsigned mode)
      { io_uring_prep_rw(IORING_OP_OPENAT, sqe, dfd, path, mode, 0); sqe->open_flags = (uint32_t)flags; }
      static inline void io_uring_prep_timeout(struct io_uring_sqe *sqe, struct __kernel_timespec *ts, unsigned count, unsigned flags)
      { io_uring_prep_rw(IORING_OP_TIMEOUT, sqe, -1, ts, 1, count); sqe->timeout_flags = flags; }
      static inline void io_uring_prep_cancel64(struct io_uring_sqe *sqe, uint64_t user_data, int flags)
      { io_uring_prep_rw(IORING_OP_ASYNC_CANCEL, sqe, -1, NULL, 0, 0); sqe->addr = user_data; sqe->cancel_flags = (uint32_t)flags; }
      static inline void io_uring_prep_cancel(struct io_uring_sqe *sqe, void *user_data, int flags)
      { io_uring_prep_cancel64(sqe, (uint64_t)(uintptr_t)user_data, flags); }

      static inline int io_uring_major_version(void) { return IO_URING_VERSION_MAJOR; }
      static inline int io_uring_minor_version(void) { return IO_URING_VERSION_MINOR; }
      static inline int io_uring_check_version(int major, int minor)
      { return !(major < IO_URING_VERSION_MAJOR || (major == IO_URING_VERSION_MAJOR && minor <= IO_URING_VERSION_MINOR)); }

      static inline int io_uring_opcode_supported_win(int op)
      {
          switch (op) {
          case IORING_OP_NOP: case IORING_OP_READV: case IORING_OP_WRITEV: case IORING_OP_FSYNC:
          case IORING_OP_READ_FIXED: case IORING_OP_WRITE_FIXED: case IORING_OP_TIMEOUT: case IORING_OP_ASYNC_CANCEL:
          case IORING_OP_OPENAT: case IORING_OP_CLOSE: case IORING_OP_READ: case IORING_OP_WRITE:
              return 1;
          default:
              return 0;
          }
      }

      #ifdef __cplusplus
      }
      #endif

      #endif /* LIBURING_H */
    C
    # An empty ar archive: everything is static inline in liburing.h.
    (lib/"liburing.a").write "!<arch>\n"
    (lib/"pkgconfig/liburing.pc").write <<~PC
      prefix=${pcfiledir}/../..
      includedir=${prefix}/include
      libdir=${prefix}/lib

      Name: liburing
      Description: io_uring library (on Windows I/O rings)
      Version: #{version}
      Cflags: -I${includedir}
      Libs: -L${libdir} -luring
    PC
    man3.install Dir["man/*.3"]
    man7.install Dir["man/*.7"]
    doc.install "README", "LICENSE"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the API surface instead.
    header = (include/"liburing.h").read
    %w[io_uring_queue_init io_uring_get_sqe io_uring_prep_read io_uring_submit io_uring_wait_cqe
       io_uring_cqe_seen CreateIoRing BuildIoRingReadFile].each do |sym|
      assert_match sym, header
    end
    assert_equal "!<arch>\n", (lib/"liburing.a").read
    assert_match "-luring", (lib/"pkgconfig/liburing.pc").read
    assert_path_exists man3/"io_uring_queue_init.3"
  end
end
