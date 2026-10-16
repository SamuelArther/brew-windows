class Fcft < Formula
  desc "Simple library for font loading and glyph rasterization"
  homepage "https://codeberg.org/dnkl/fcft"
  url "https://codeberg.org/dnkl/fcft/releases/download/3.3.3/fcft-3.3.3.tar.gz"
  sha256 "bb298772d625e7d917373d541456f34f1957a9e22b16f17f64158b2c3816563c"
  license "MIT"

  # fcft itself is portable C on fontconfig, FreeType, HarfBuzz and pixman, which all have Windows
  # builds; fontconfig finds the fonts in C:\Windows\Fonts. Upstream's meson build is used with a few
  # POSIX gaps in MinGW's C library filled in the source tree (meson puts it on the include path):
  # C11 <threads.h> on winpthreads, <byteswap.h> on the compiler builtins, <syslog.h> on the debug
  # output stream, and per-thread locales (newlocale/uselocale) on the UCRT's per-thread setlocale.
  # tllist (a header) and utf8proc have no Windows formulae, so they come in as resources.

  depends_on "meson" => :build
  depends_on "ninja" => :build
  depends_on "pkgconf" => :build
  depends_on "python@3.15" => :build

  depends_on "fontconfig"
  depends_on "freetype"
  depends_on "harfbuzz"
  depends_on "pixman"

  resource "tllist" do
    url "https://codeberg.org/dnkl/tllist/archive/1.1.0.tar.gz"
    sha256 "0e7b7094a02550dd80b7243bcffc3671550b0f1d8ba625e4dff52517827d5d23"
  end

  resource "libutf8proc" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libutf8proc-2.11.3-1-any.pkg.tar.zst"
    sha256 "cb4b662db4b3cfa98110ea1441f6f426c7f7ed14a677e761e937d406a93ad32a"
  end

  def install
    deps = buildpath/"deps"
    resource("tllist").stage { (deps/"include").install Dir["**/tllist.h"].first }
    (deps/"lib/pkgconfig/tllist.pc").write <<~PC
      prefix=${pcfiledir}/../..
      includedir=${prefix}/include

      Name: tllist
      Description: A C header file only implementation of a typed linked list
      Version: 1.1.0
      Cflags: -I${includedir}
    PC
    utf8proc = deps/"utf8proc"
    resource("libutf8proc").stage utf8proc
    pc = utf8proc/"ucrt64/lib/pkgconfig/libutf8proc.pc"
    pc.atomic_write pc.read.sub(/^prefix=.*$/, "prefix=${pcfiledir}/../..")
    ENV.prepend_path "PKG_CONFIG_PATH", utf8proc/"ucrt64/lib/pkgconfig"
    ENV.prepend_path "PKG_CONFIG_PATH", deps/"lib/pkgconfig"
    # generate-emoji-data.py reads UTF-8 data files.
    ENV["PYTHONUTF8"] = "1"

    (buildpath/"threads.h").write <<~'C'
      /* C11 <threads.h> mutexes and condition variables on winpthreads (MinGW has no <threads.h>). */
      #pragma once
      #include <pthread.h>

      typedef pthread_mutex_t mtx_t;
      typedef pthread_cond_t cnd_t;

      enum { thrd_success = 0, thrd_busy = 1, thrd_error = 2, thrd_nomem = 3, thrd_timedout = 4 };
      enum { mtx_plain = 0, mtx_recursive = 1, mtx_timed = 2 };

      static inline int mtx_init(mtx_t *m, int type)
      {
          pthread_mutexattr_t attr;
          int r;
          pthread_mutexattr_init(&attr);
          if (type & mtx_recursive) pthread_mutexattr_settype(&attr, PTHREAD_MUTEX_RECURSIVE);
          r = pthread_mutex_init(m, &attr);
          pthread_mutexattr_destroy(&attr);
          return r == 0 ? thrd_success : thrd_error;
      }

      static inline int mtx_lock(mtx_t *m) { return pthread_mutex_lock(m) == 0 ? thrd_success : thrd_error; }
      static inline int mtx_trylock(mtx_t *m) { return pthread_mutex_trylock(m) == 0 ? thrd_success : thrd_busy; }
      static inline int mtx_unlock(mtx_t *m) { return pthread_mutex_unlock(m) == 0 ? thrd_success : thrd_error; }
      static inline void mtx_destroy(mtx_t *m) { pthread_mutex_destroy(m); }

      static inline int cnd_init(cnd_t *c) { return pthread_cond_init(c, NULL) == 0 ? thrd_success : thrd_error; }
      static inline int cnd_signal(cnd_t *c) { return pthread_cond_signal(c) == 0 ? thrd_success : thrd_error; }
      static inline int cnd_broadcast(cnd_t *c) { return pthread_cond_broadcast(c) == 0 ? thrd_success : thrd_error; }
      static inline int cnd_wait(cnd_t *c, mtx_t *m) { return pthread_cond_wait(c, m) == 0 ? thrd_success : thrd_error; }
      static inline void cnd_destroy(cnd_t *c) { pthread_cond_destroy(c); }
    C

    (buildpath/"byteswap.h").write <<~'C'
      /* glibc's <byteswap.h> on the compiler builtins. */
      #pragma once
      #define bswap_16(x) __builtin_bswap16(x)
      #define bswap_32(x) __builtin_bswap32(x)
      #define bswap_64(x) __builtin_bswap64(x)
    C

    (buildpath/"syslog.h").write <<~'C'
      /* <syslog.h>: Windows has no syslog daemon; messages go to the debug output stream instead
       * (OutputDebugString, what DebugView and debuggers show), prefixed with their priority. */
      #pragma once
      #include <stdarg.h>
      #include <stdio.h>

      #define LOG_EMERG 0
      #define LOG_ALERT 1
      #define LOG_CRIT 2
      #define LOG_ERR 3
      #define LOG_WARNING 4
      #define LOG_NOTICE 5
      #define LOG_INFO 6
      #define LOG_DEBUG 7
      #define LOG_PID 0x01
      #define LOG_CONS 0x02
      #define LOG_NDELAY 0x08
      #define LOG_USER (1 << 3)
      #define LOG_DAEMON (3 << 3)

      __declspec(dllimport) void __stdcall OutputDebugStringA(const char *message);

      static inline void openlog(const char *ident, int option, int facility) { (void)ident; (void)option; (void)facility; }
      static inline void closelog(void) {}

      static inline void syslog(int priority, const char *format, ...)
      {
          static const char *const names[] = { "emerg", "alert", "crit", "err", "warning", "notice", "info", "debug" };
          char message[1024];
          int n = snprintf(message, sizeof(message), "%s: ", names[priority & 7]);
          va_list va;
          va_start(va, format);
          vsnprintf(message + n, sizeof(message) - (size_t)n, format, va);
          va_end(va);
          OutputDebugStringA(message);
      }
    C

    (buildpath/"fcft-win-compat.h").write <<~'C'
      /* POSIX per-thread locales, which the UCRT lacks: fcft only switches LC_NUMERIC to "C" around
       * FcNameParse. Here newlocale() hands out the "C" locale, uselocale() switches this thread to it
       * (UCRT per-thread locale) and returns the previous one, which uselocale() later restores. */
      #pragma once
      #include <locale.h>
      #include <stdlib.h>
      #include <string.h>

      typedef struct fcft_locale { char *numeric; int per_thread; } *fcft_locale_t;
      #define locale_t fcft_locale_t
      #ifndef LC_NUMERIC_MASK
      #define LC_NUMERIC_MASK (1 << LC_NUMERIC)
      #endif
      #define newlocale fcft_newlocale
      #define uselocale fcft_uselocale
      #define freelocale fcft_freelocale

      static struct fcft_locale fcft_c_locale;

      static inline locale_t fcft_newlocale(int mask, const char *name, locale_t base)
      {
          (void)mask; (void)base;
          return name != NULL && (strcmp(name, "C") == 0 || strcmp(name, "POSIX") == 0) ? &fcft_c_locale : NULL;
      }

      static inline locale_t fcft_uselocale(locale_t locale)
      {
          if (locale == &fcft_c_locale) {
              locale_t previous = calloc(1, sizeof(*previous));
              const char *current;
              if (previous == NULL) return NULL;
              previous->per_thread = _configthreadlocale(_ENABLE_PER_THREAD_LOCALE);
              current = setlocale(LC_NUMERIC, NULL);
              previous->numeric = current != NULL ? strdup(current) : NULL;
              setlocale(LC_NUMERIC, "C");
              return previous;
          }
          if (locale != NULL) {
              if (locale->numeric != NULL) setlocale(LC_NUMERIC, locale->numeric);
              _configthreadlocale(locale->per_thread);
              free(locale->numeric);
              free(locale);
          }
          return &fcft_c_locale;
      }

      static inline void fcft_freelocale(locale_t locale) { (void)locale; }
    C
    inreplace "fcft.c", "#include <locale.h>\n", "#include <locale.h>\n#include \"fcft-win-compat.h\"\n"
    # MinGW lays bit-fields out the MSVC way unless asked not to; the emoji table is a packed GCC struct.
    inreplace "generate-emoji-data.py", "__attribute__((packed))", "__attribute__((packed, gcc_struct))"

    # PE has no symbol visibility; without it every non-static symbol is exported from the DLL.
    # werror is off: MinGW's 16-bit wint_t draws -Wformat warnings for fcft's %lc log messages.
    args = %w[-Ddocs=disabled -Dwerror=false -Dsvg-backend=nanosvg -Dgrapheme-shaping=enabled -Drun-shaping=enabled]
    system "meson", "setup", "build", *args, *std_meson_args
    system "meson", "compile", "-C", "build", "--verbose"
    system "meson", "install", "-C", "build"

    # utf8proc's DLL travels with the keg, since no formula provides it; tllist was only needed to
    # build. Neither is a pkg-config module users of fcft.pc can resolve, so they leave its Requires.
    bin.install Dir[utf8proc/"ucrt64/bin/*.dll"]
    fcft_pc = lib/"pkgconfig/fcft.pc"
    fcft_pc.atomic_write fcft_pc.read.gsub(/,\s*(?:tllist|libutf8proc)(?:\s*[<>=]+\s*[\d.]+)?/, "")
    pkgshare.install "example"
  end

  test do
    (testpath/"test.c").write <<~C
      #include <stdio.h>
      #include <fcft/fcft.h>

      int main() {
        printf("%u", fcft_capabilities());
      }
    C

    pixman = Formula["pixman"]
    system ENV.cc, "test.c", "-o", "test.exe", "-I#{include}", "-I#{pixman.opt_include}/pixman-1",
           "-L#{lib}", "-lfcft"
    # Windows finds a program's DLLs beside it: fcft's and the ones bundled with it.
    cp Dir[bin/"*.dll"], testpath
    assert_equal "7", shell_output("./test.exe")
  end
end
