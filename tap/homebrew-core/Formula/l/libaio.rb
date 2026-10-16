class Libaio < Formula
  desc "Linux AIO API on Windows overlapped I/O and completion ports"
  homepage "https://pagure.io/libaio"
  url "https://deb.debian.org/debian/pool/main/liba/libaio/libaio_0.3.113.orig.tar.gz"
  sha256 "2c44d1c5fd0d43752287c9ae1eb9c023f04ef848ea8d4aafa46e9aedb678200b"
  license "LGPL-2.1-or-later"

  # libaio is the user-space side of Linux's native AIO system calls. Windows has had kernel
  # asynchronous file I/O all along: overlapped ReadFile/WriteFile with completion delivered to an
  # I/O completion port, which is what this keg implements libaio's API with. io_setup creates a
  # completion port, io_submit issues overlapped reads and writes (vectored ones through a bounce
  # buffer; fsync/fdsync are FlushFileBuffers), io_getevents collects completion packets with
  # libaio's min_nr/nr/timeout semantics, io_cancel is CancelIoEx, and io_queue_run runs
  # io_set_callback callbacks. File descriptors are C runtime descriptors (_open, _fileno): each is
  # reopened once per context as an overlapped handle. IO_CMD_POLL is refused with -EINVAL, as
  # Windows has no readiness polling of files. libaio.h keeps the upstream structures and helper
  # names (struct iocb, struct io_event, io_prep_pread ...) with 64-bit fields that suit Windows'
  # 32-bit long; the upstream tarball provides the man pages and license.

  def install
    (buildpath/"win/libaio.h").write <<~'C'
      /* libaio for Windows: the Linux AIO API (io_setup, io_submit, io_getevents ...) on overlapped I/O
       * and an I/O completion port. The structures keep libaio's field names; file descriptors are C
       * runtime descriptors (_open/_fileno), served through an overlapped handle reopened from them.
       * Link with -laio. */
      #ifndef __LIBAIO_H
      #define __LIBAIO_H

      #include <stddef.h>
      #include <string.h>
      #include <time.h>

      #ifdef __cplusplus
      extern "C" {
      #endif

      struct sockaddr;

      #if !defined(_SYS_UIO_H) && !defined(LIBAIO_HAVE_IOVEC) && !defined(_STRUCT_IOVEC)
      #define LIBAIO_HAVE_IOVEC 1
      struct iovec {
          void *iov_base;
          size_t iov_len;
      };
      #endif

      typedef struct io_context *io_context_t;

      typedef enum io_iocb_cmd {
          IO_CMD_PREAD = 0,
          IO_CMD_PWRITE = 1,
          IO_CMD_FSYNC = 2,
          IO_CMD_FDSYNC = 3,
          IO_CMD_POLL = 5,
          IO_CMD_NOOP = 6,
          IO_CMD_PREADV = 7,
          IO_CMD_PWRITEV = 8,
      } io_iocb_cmd_t;

      struct io_iocb_poll {
          int events;
          int __pad1;
      };

      struct io_iocb_sockaddr {
          struct sockaddr *addr;
          int len;
      };

      struct io_iocb_common {
          void *buf;
          unsigned long long nbytes;
          long long offset;
          long long __pad3;
          unsigned flags;
          unsigned resfd;
      };

      struct io_iocb_vector {
          const struct iovec *vec;
          int nr;
          long long offset;
      };

      struct iocb {
          void *data;
          unsigned key;
          int aio_rw_flags;
          short aio_lio_opcode;
          short aio_reqprio;
          int aio_fildes;
          union {
              struct io_iocb_common c;
              struct io_iocb_vector v;
              struct io_iocb_poll poll;
              struct io_iocb_sockaddr saddr;
          } u;
      };

      struct io_event {
          void *data;
          struct iocb *obj;
          unsigned long long res;
          unsigned long long res2;
      };

      typedef void (*io_callback_t)(io_context_t ctx, struct iocb *iocb, long res, long res2);

      /* Flags for iocb.u.c.flags. */
      #define IOCB_FLAG_RESFD (1 << 0)
      #define IOCB_FLAG_IOPRIO (1 << 1)

      /* Errors are returned as negative errno values, as libaio does. */
      extern int io_queue_init(int maxevents, io_context_t *ctxp);
      extern int io_queue_release(io_context_t ctx);
      extern int io_queue_run(io_context_t ctx);
      extern int io_setup(int maxevents, io_context_t *ctxp);
      extern int io_destroy(io_context_t ctx);
      extern int io_submit(io_context_t ctx, long nr, struct iocb *ios[]);
      extern int io_cancel(io_context_t ctx, struct iocb *iocb, struct io_event *evt);
      extern int io_getevents(io_context_t ctx, long min_nr, long nr, struct io_event *events, struct timespec *timeout);

      static inline void io_set_callback(struct iocb *iocb, io_callback_t cb)
      {
          iocb->data = (void *)cb;
      }

      static inline void io_prep_pread(struct iocb *iocb, int fd, void *buf, size_t count, long long offset)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_PREAD;
          iocb->aio_reqprio = 0;
          iocb->u.c.buf = buf;
          iocb->u.c.nbytes = count;
          iocb->u.c.offset = offset;
      }

      static inline void io_prep_pwrite(struct iocb *iocb, int fd, void *buf, size_t count, long long offset)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_PWRITE;
          iocb->aio_reqprio = 0;
          iocb->u.c.buf = buf;
          iocb->u.c.nbytes = count;
          iocb->u.c.offset = offset;
      }

      static inline void io_prep_preadv(struct iocb *iocb, int fd, const struct iovec *iov, int iovcnt, long long offset)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_PREADV;
          iocb->aio_reqprio = 0;
          iocb->u.v.vec = iov;
          iocb->u.v.nr = iovcnt;
          iocb->u.v.offset = offset;
      }

      static inline void io_prep_pwritev(struct iocb *iocb, int fd, const struct iovec *iov, int iovcnt, long long offset)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_PWRITEV;
          iocb->aio_reqprio = 0;
          iocb->u.v.vec = iov;
          iocb->u.v.nr = iovcnt;
          iocb->u.v.offset = offset;
      }

      static inline void io_prep_preadv2(struct iocb *iocb, int fd, const struct iovec *iov, int iovcnt, long long offset, int flags)
      {
          io_prep_preadv(iocb, fd, iov, iovcnt, offset);
          iocb->aio_rw_flags = flags;
      }

      static inline void io_prep_pwritev2(struct iocb *iocb, int fd, const struct iovec *iov, int iovcnt, long long offset, int flags)
      {
          io_prep_pwritev(iocb, fd, iov, iovcnt, offset);
          iocb->aio_rw_flags = flags;
      }

      static inline void io_prep_poll(struct iocb *iocb, int fd, int events)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_POLL;
          iocb->aio_reqprio = 0;
          iocb->u.poll.events = events;
      }

      static inline void io_prep_fsync(struct iocb *iocb, int fd)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_FSYNC;
          iocb->aio_reqprio = 0;
      }

      static inline void io_prep_fdsync(struct iocb *iocb, int fd)
      {
          memset(iocb, 0, sizeof(*iocb));
          iocb->aio_fildes = fd;
          iocb->aio_lio_opcode = IO_CMD_FDSYNC;
          iocb->aio_reqprio = 0;
      }

      static inline int io_poll(io_context_t ctx, struct iocb *iocb, io_callback_t cb, int fd, int events)
      {
          io_prep_poll(iocb, fd, events);
          io_set_callback(iocb, cb);
          return io_submit(ctx, 1, &iocb);
      }

      static inline int io_fsync(io_context_t ctx, struct iocb *iocb, io_callback_t cb, int fd)
      {
          io_prep_fsync(iocb, fd);
          io_set_callback(iocb, cb);
          return io_submit(ctx, 1, &iocb);
      }

      static inline int io_fdsync(io_context_t ctx, struct iocb *iocb, io_callback_t cb, int fd)
      {
          io_prep_fdsync(iocb, fd);
          io_set_callback(iocb, cb);
          return io_submit(ctx, 1, &iocb);
      }

      static inline void io_set_eventfd(struct iocb *iocb, int eventfd)
      {
          iocb->u.c.flags |= IOCB_FLAG_RESFD;
          iocb->u.c.resfd = eventfd;
      }

      #ifdef __cplusplus
      }
      #endif

      #endif /* __LIBAIO_H */
    C
    (buildpath/"win/aio.c").write <<~'C'
      /* libaio for Windows: each io_context is an I/O completion port. A C runtime file descriptor is
       * reopened once per context as an overlapped handle (ReOpenFile) bound to the port; reads and
       * writes are ReadFile/WriteFile at the iocb's offset, and io_getevents collects their completion
       * packets. Vectored requests go through one bounce buffer; fsync is FlushFileBuffers. */
      #define WIN32_LEAN_AND_MEAN
      #include <windows.h>
      #include <errno.h>
      #include <io.h>
      #include <stdlib.h>
      #include "libaio.h"

      #ifndef ECANCELED
      #define ECANCELED 105
      #endif

      struct aio_op {
          OVERLAPPED ov;
          struct iocb *cb;
          HANDLE h;
          char *bounce;
          struct aio_op *next;
      };

      struct aio_handle {
          int fd;
          HANDLE original, h;
      };

      struct io_context {
          HANDLE port;
          long max;
          long inflight;
          CRITICAL_SECTION lock;
          struct aio_handle *handles;
          int nhandles;
          struct aio_op *pending;          /* submitted, completion packet not yet collected */
          struct io_event *ready;          /* completed without a packet (sync ops and errors) */
          long nready;
      };

      static long err_from_win32(DWORD e)
      {
          switch (e) {
          case ERROR_HANDLE_EOF: return 0;
          case ERROR_OPERATION_ABORTED: return -ECANCELED;
          case ERROR_INVALID_HANDLE: return -EBADF;
          case ERROR_ACCESS_DENIED: return -EBADF;
          case ERROR_DISK_FULL: case ERROR_HANDLE_DISK_FULL: return -ENOSPC;
          case ERROR_NOT_ENOUGH_MEMORY: case ERROR_OUTOFMEMORY: return -ENOMEM;
          case ERROR_INVALID_PARAMETER: return -EINVAL;
          default: return -EIO;
          }
      }

      int io_setup(int maxevents, io_context_t *ctxp)
      {
          struct io_context *ctx;
          if (maxevents <= 0 || !ctxp) return -EINVAL;
          ctx = calloc(1, sizeof(*ctx));
          if (!ctx) return -ENOMEM;
          ctx->ready = calloc((size_t)maxevents, sizeof(struct io_event));
          ctx->port = CreateIoCompletionPort(INVALID_HANDLE_VALUE, NULL, 0, 0);
          if (!ctx->ready || !ctx->port) {
              free(ctx->ready);
              free(ctx);
              return -EAGAIN;
          }
          ctx->max = maxevents;
          InitializeCriticalSection(&ctx->lock);
          *ctxp = ctx;
          return 0;
      }

      int io_queue_init(int maxevents, io_context_t *ctxp)
      {
          if (ctxp) *ctxp = NULL;
          return io_setup(maxevents, ctxp);
      }

      int io_destroy(io_context_t ctx)
      {
          int i;
          struct aio_op *op;
          if (!ctx) return -EINVAL;
          /* Cancel what is still in flight and wait for it, so no request writes into freed memory. */
          EnterCriticalSection(&ctx->lock);
          for (op = ctx->pending; op; op = op->next) CancelIoEx(op->h, &op->ov);
          LeaveCriticalSection(&ctx->lock);
          while (ctx->pending) {
              OVERLAPPED_ENTRY entry;
              ULONG got = 0;
              if (!GetQueuedCompletionStatusEx(ctx->port, &entry, 1, &got, 1000, FALSE) || got == 0) break;
              EnterCriticalSection(&ctx->lock);
              for (struct aio_op **pp = &ctx->pending; *pp; pp = &(*pp)->next)
                  if (&(*pp)->ov == entry.lpOverlapped) {
                      struct aio_op *done = *pp;
                      *pp = done->next;
                      free(done->bounce);
                      free(done);
                      break;
                  }
              LeaveCriticalSection(&ctx->lock);
          }
          for (i = 0; i < ctx->nhandles; i++) CloseHandle(ctx->handles[i].h);
          free(ctx->handles);
          CloseHandle(ctx->port);
          DeleteCriticalSection(&ctx->lock);
          free(ctx->ready);
          free(ctx);
          return 0;
      }

      int io_queue_release(io_context_t ctx)
      {
          return io_destroy(ctx);
      }

      /* The context's overlapped handle for a C runtime descriptor (opened on first use). */
      static HANDLE overlapped_handle(struct io_context *ctx, int fd)
      {
          int i;
          HANDLE original = (HANDLE)_get_osfhandle(fd), h;
          struct aio_handle *grown;
          if (original == INVALID_HANDLE_VALUE) return INVALID_HANDLE_VALUE;
          for (i = 0; i < ctx->nhandles; i++)
              if (ctx->handles[i].fd == fd && ctx->handles[i].original == original) return ctx->handles[i].h;
          h = ReOpenFile(original, GENERIC_READ | GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_FLAG_OVERLAPPED);
          if (h == INVALID_HANDLE_VALUE)
              h = ReOpenFile(original, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_FLAG_OVERLAPPED);
          if (h == INVALID_HANDLE_VALUE)
              h = ReOpenFile(original, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, FILE_FLAG_OVERLAPPED);
          if (h == INVALID_HANDLE_VALUE) return INVALID_HANDLE_VALUE;
          if (!CreateIoCompletionPort(h, ctx->port, 0, 0)) {
              CloseHandle(h);
              return INVALID_HANDLE_VALUE;
          }
          grown = realloc(ctx->handles, sizeof(*grown) * (size_t)(ctx->nhandles + 1));
          if (!grown) {
              CloseHandle(h);
              return INVALID_HANDLE_VALUE;
          }
          ctx->handles = grown;
          ctx->handles[ctx->nhandles].fd = fd;
          ctx->handles[ctx->nhandles].original = original;
          ctx->handles[ctx->nhandles].h = h;
          ctx->nhandles++;
          return h;
      }

      static void complete_now(struct io_context *ctx, struct iocb *cb, long long res)
      {
          struct io_event *ev = &ctx->ready[ctx->nready++];
          ev->data = cb->data;
          ev->obj = cb;
          ev->res = (unsigned long long)res;
          ev->res2 = 0;
      }

      static int submit_one(struct io_context *ctx, struct iocb *cb)
      {
          HANDLE h;
          struct aio_op *op;
          unsigned long long len = 0;
          long long offset;
          char *buf;
          int i, write;
          BOOL ok;

          if (ctx->inflight + ctx->nready >= ctx->max) return -EAGAIN;
          switch (cb->aio_lio_opcode) {
          case IO_CMD_NOOP:
              complete_now(ctx, cb, 0);
              return 0;
          case IO_CMD_FSYNC:
          case IO_CMD_FDSYNC:
              h = (HANDLE)_get_osfhandle(cb->aio_fildes);
              if (h == INVALID_HANDLE_VALUE) return -EBADF;
              complete_now(ctx, cb, FlushFileBuffers(h) ? 0 : err_from_win32(GetLastError()));
              return 0;
          case IO_CMD_PREAD:
          case IO_CMD_PWRITE:
          case IO_CMD_PREADV:
          case IO_CMD_PWRITEV:
              break;
          default:
              return -EINVAL;   /* IO_CMD_POLL: Windows has no readiness polling of file descriptors */
          }
          h = overlapped_handle(ctx, cb->aio_fildes);
          if (h == INVALID_HANDLE_VALUE) return -EBADF;
          op = calloc(1, sizeof(*op));
          if (!op) return -ENOMEM;
          write = cb->aio_lio_opcode == IO_CMD_PWRITE || cb->aio_lio_opcode == IO_CMD_PWRITEV;
          if (cb->aio_lio_opcode == IO_CMD_PREAD || cb->aio_lio_opcode == IO_CMD_PWRITE) {
              buf = cb->u.c.buf;
              len = cb->u.c.nbytes;
              offset = cb->u.c.offset;
          } else {
              if (cb->u.v.nr < 0 || (cb->u.v.nr > 0 && !cb->u.v.vec)) { free(op); return -EINVAL; }
              for (i = 0; i < cb->u.v.nr; i++) len += cb->u.v.vec[i].iov_len;
              op->bounce = malloc(len ? (size_t)len : 1);
              if (!op->bounce) { free(op); return -ENOMEM; }
              if (write) {
                  char *p = op->bounce;
                  for (i = 0; i < cb->u.v.nr; i++) {
                      memcpy(p, cb->u.v.vec[i].iov_base, cb->u.v.vec[i].iov_len);
                      p += cb->u.v.vec[i].iov_len;
                  }
              }
              buf = op->bounce;
              offset = cb->u.v.offset;
          }
          if (len > 0xffffffffULL || offset < 0) { free(op->bounce); free(op); return -EINVAL; }
          op->cb = cb;
          op->h = h;
          op->ov.Offset = (DWORD)((unsigned long long)offset & 0xffffffff);
          op->ov.OffsetHigh = (DWORD)((unsigned long long)offset >> 32);
          op->next = ctx->pending;
          ctx->pending = op;
          ctx->inflight++;
          ok = write ? WriteFile(h, buf, (DWORD)len, NULL, &op->ov) : ReadFile(h, buf, (DWORD)len, NULL, &op->ov);
          if (!ok && GetLastError() != ERROR_IO_PENDING) {
              /* Failed without queueing a packet (EOF on read, a bad offset ...): complete it here. */
              DWORD e = GetLastError();
              ctx->pending = op->next;
              ctx->inflight--;
              free(op->bounce);
              free(op);
              complete_now(ctx, cb, err_from_win32(e));
          }
          return 0;
      }

      int io_submit(io_context_t ctx, long nr, struct iocb *ios[])
      {
          long i;
          int r = 0;
          if (!ctx || nr < 0 || (nr > 0 && !ios)) return -EINVAL;
          EnterCriticalSection(&ctx->lock);
          for (i = 0; i < nr; i++) {
              if (!ios[i]) { r = -EFAULT; break; }
              r = submit_one(ctx, ios[i]);
              if (r < 0) break;
          }
          LeaveCriticalSection(&ctx->lock);
          /* Like io_submit(2): the number queued, or the error if none was. */
          return i > 0 ? (int)i : r;
      }

      static struct aio_op *take_pending(struct io_context *ctx, OVERLAPPED *ov)
      {
          struct aio_op **pp;
          for (pp = &ctx->pending; *pp; pp = &(*pp)->next)
              if (&(*pp)->ov == ov) {
                  struct aio_op *op = *pp;
                  *pp = op->next;
                  ctx->inflight--;
                  return op;
              }
          return NULL;
      }

      static void finish(struct aio_op *op, DWORD bytes, struct io_event *ev)
      {
          struct iocb *cb = op->cb;
          DWORD transferred = 0;
          long long res;
          if (GetOverlappedResult(op->h, &op->ov, &transferred, FALSE)) res = transferred;
          else res = err_from_win32(GetLastError());
          (void)bytes;
          if (res > 0 && cb->aio_lio_opcode == IO_CMD_PREADV) {
              /* Scatter the bounce buffer into the iovecs. */
              const char *p = op->bounce;
              long long left = res;
              int i;
              for (i = 0; i < cb->u.v.nr && left > 0; i++) {
                  size_t n = cb->u.v.vec[i].iov_len < (size_t)left ? cb->u.v.vec[i].iov_len : (size_t)left;
                  memcpy(cb->u.v.vec[i].iov_base, p, n);
                  p += n;
                  left -= (long long)n;
              }
          }
          ev->data = cb->data;
          ev->obj = cb;
          ev->res = (unsigned long long)res;
          ev->res2 = 0;
          free(op->bounce);
          free(op);
      }

      int io_getevents(io_context_t ctx, long min_nr, long nr, struct io_event *events, struct timespec *timeout)
      {
          long n = 0;
          ULONGLONG deadline = 0;
          if (!ctx || nr < 0 || min_nr > nr || (nr > 0 && !events)) return -EINVAL;
          if (min_nr < 0) min_nr = 0;
          if (timeout) {
              if (timeout->tv_sec < 0 || timeout->tv_nsec < 0 || timeout->tv_nsec >= 1000000000L) return -EINVAL;
              deadline = GetTickCount64() + (ULONGLONG)timeout->tv_sec * 1000 + (ULONGLONG)(timeout->tv_nsec + 999999) / 1000000;
          }
          for (;;) {
              OVERLAPPED_ENTRY entries[64];
              ULONG got = 0, want, i;
              DWORD wait;
              EnterCriticalSection(&ctx->lock);
              while (n < nr && ctx->nready > 0) {
                  events[n++] = ctx->ready[0];
                  memmove(ctx->ready, ctx->ready + 1, sizeof(struct io_event) * (size_t)(--ctx->nready));
              }
              LeaveCriticalSection(&ctx->lock);
              if (n >= nr) break;
              if (n >= min_nr && n > 0) wait = 0;
              else if (!timeout) wait = INFINITE;
              else {
                  ULONGLONG now = GetTickCount64();
                  wait = now >= deadline ? 0 : (DWORD)(deadline - now);
              }
              if (ctx->inflight == 0 && n >= min_nr) break;
              want = (ULONG)(nr - n < 64 ? nr - n : 64);
              if (!GetQueuedCompletionStatusEx(ctx->port, entries, want, &got, wait, FALSE) || got == 0) {
                  if (wait == INFINITE) continue;
                  break;   /* timed out */
              }
              EnterCriticalSection(&ctx->lock);
              for (i = 0; i < got; i++) {
                  struct aio_op *op = take_pending(ctx, entries[i].lpOverlapped);
                  if (op) finish(op, entries[i].dwNumberOfBytesTransferred, &events[n++]);
              }
              LeaveCriticalSection(&ctx->lock);
              if (n >= min_nr && (n >= nr || ctx->inflight == 0)) break;
          }
          return (int)n;
      }

      int io_cancel(io_context_t ctx, struct iocb *iocb, struct io_event *evt)
      {
          struct aio_op *op;
          int found = 0;
          (void)evt;
          if (!ctx || !iocb) return -EINVAL;
          EnterCriticalSection(&ctx->lock);
          for (op = ctx->pending; op; op = op->next)
              if (op->cb == iocb) {
                  found = CancelIoEx(op->h, &op->ov) || GetLastError() != ERROR_NOT_FOUND;
                  break;
              }
          LeaveCriticalSection(&ctx->lock);
          /* Cancellation completes asynchronously: the request's event (res -ECANCELED) arrives
           * through io_getevents, as with Linux kernels that return -EINPROGRESS here. */
          return found ? -EINPROGRESS : -EAGAIN;
      }

      int io_queue_run(io_context_t ctx)
      {
          struct io_event events[64];
          struct timespec zero = {0, 0};
          int n, i, total = 0;
          while ((n = io_getevents(ctx, 0, 64, events, &zero)) > 0) {
              for (i = 0; i < n; i++) {
                  io_callback_t cb = (io_callback_t)events[i].data;
                  if (cb) cb(ctx, events[i].obj, (long)events[i].res, (long)events[i].res2);
              }
              total += n;
          }
          return n < 0 ? n : total;
      }
    C
    system ENV.cc, "-O2", "-std=gnu11", "-Iwin", "-shared", "-o", "libaio.dll", "win/aio.c",
           "-Wl,--out-implib,libaio.dll.a"
    system ENV.cc, "-O2", "-std=gnu11", "-Iwin", "-c", "-o", "aio.o", "win/aio.c"
    system "ar", "rcs", "libaio.a", "aio.o"
    include.install "win/libaio.h"
    lib.install "libaio.dll.a", "libaio.a"
    bin.install "libaio.dll"
    man3.install Dir["man/*.3"]
  end

  test do
    (testpath/"test.c").write <<~'C'
      #include <fcntl.h>
      #include <io.h>
      #include <stdio.h>
      #include <sys/stat.h>
      #include <libaio.h>

      int main(void)
      {
          io_context_t ctx = 0;
          struct iocb w, r, *list[1];
          struct io_event ev;
          char msg[] = "hello", buf[6] = {0};
          int fd = _open("data.bin", _O_RDWR | _O_CREAT | _O_TRUNC | _O_BINARY, _S_IREAD | _S_IWRITE);
          if (fd < 0 || io_setup(4, &ctx) != 0) return 1;
          io_prep_pwrite(&w, fd, msg, 5, 0);
          list[0] = &w;
          if (io_submit(ctx, 1, list) != 1 || io_getevents(ctx, 1, 1, &ev, NULL) != 1 || ev.res != 5) return 2;
          io_prep_pread(&r, fd, buf, 5, 0);
          list[0] = &r;
          if (io_submit(ctx, 1, list) != 1 || io_getevents(ctx, 1, 1, &ev, NULL) != 1 || ev.res != 5) return 3;
          printf("%s", buf);
          io_destroy(ctx);
          return 0;
      }
    C
    system ENV.cc, "test.c", "-I#{include}", "-L#{lib}", "-laio", "-o", "test.exe"
    assert_equal "hello", shell_output("./test.exe")
  end
end
