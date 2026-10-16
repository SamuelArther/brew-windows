class EpollShim < Formula
  desc "Epoll implementation for Windows sockets (wepoll)"
  homepage "https://github.com/piscisaureus/wepoll"
  # wepoll's official amalgamated distribution (the "dist" branch commit for v1.5.8).
  url "https://github.com/piscisaureus/wepoll/archive/0598a791bf9cbbf480793d778930fc635b044980.tar.gz"
  version "1.5.8"
  sha256 "9998a17bac4a60ce16c2052c9f2e0d22689ca57d7db03a4e44412b28cddb22eb"
  license "BSD-2-Clause"

  # epoll-shim implements epoll on kqueue for the BSDs and macOS. On Windows the epoll API comes from
  # wepoll, which implements it on top of the AFD poll driver (the same mechanism as libuv and
  # ZeroMQ). It ships as one .c/.h pair rather than a prebuilt library, so the keg carries it the
  # way epoll-shim's users expect: <sys/epoll.h> under include/libepoll-shim, an epoll-shim.pc,
  # and include/libepoll-shim/wepoll.c to compile into the program (or define EPOLL_SHIM_IMPLEMENTATION in one source
  # file before including <sys/epoll.h>). Epoll instances are HANDLEs closed with epoll_close().

  livecheck do
    url "https://github.com/piscisaureus/wepoll.git"
    regex(/^v?(\d+(?:\.\d+)+)$/i)
  end

  def install
    (include/"libepoll-shim").install "wepoll.h", "wepoll.c"
    (include/"libepoll-shim/sys/epoll.h").write <<~C
      /* epoll-shim for Windows: the epoll API from wepoll <https://github.com/piscisaureus/wepoll>.
       * Epoll instances are HANDLEs; close them with epoll_close(). Build wepoll.c into the program,
       * or define EPOLL_SHIM_IMPLEMENTATION in exactly one source file before including this header.
       * Link with -lws2_32. */
      #ifndef EPOLL_SHIM_SYS_EPOLL_H_
      #define EPOLL_SHIM_SYS_EPOLL_H_

      #ifdef EPOLL_SHIM_IMPLEMENTATION
      #include "../wepoll.c"
      #else
      #include "../wepoll.h"
      #endif

      /* wepoll handles are never inherited by child processes, so close-on-exec is implied. */
      #ifndef EPOLL_CLOEXEC
      #define EPOLL_CLOEXEC 0
      #endif

      #endif /* EPOLL_SHIM_SYS_EPOLL_H_ */
    C
    (lib/"pkgconfig/epoll-shim.pc").write <<~PC
      prefix=${pcfiledir}/../..
      includedir=${prefix}/include

      Name: epoll-shim
      Description: epoll for Windows sockets (wepoll)
      Version: #{version}
      Cflags: -I${includedir}/libepoll-shim
      Libs: -lws2_32
    PC
    doc.install "README.md", "LICENSE"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the epoll API surface instead.
    epoll_h = (include/"libepoll-shim/sys/epoll.h").read
    assert_match "EPOLL_SHIM_IMPLEMENTATION", epoll_h
    wepoll_h = (include/"libepoll-shim/wepoll.h").read
    %w[epoll_create1 epoll_ctl epoll_wait epoll_close EPOLLONESHOT].each { |sym| assert_match sym, wepoll_h }
    assert_match "epoll__create", (include/"libepoll-shim/wepoll.c").read
    assert_match "-I${includedir}/libepoll-shim", (lib/"pkgconfig/epoll-shim.pc").read
  end
end
