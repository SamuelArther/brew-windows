class LibatomicOps < Formula
  desc "Implementations for atomic memory update operations"
  homepage "https://github.com/bdwgc/libatomic_ops/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libatomic_ops-7.10.0-1-any.pkg.tar.zst"
  sha256 "564b9737040fa2d20702a1b60f3e3717c5ae3470ab90c9852f5e934db40d5328"
  license all_of: [
    "MIT",
    "Boehm-GC",         # include/atomic_ops/sysdeps/gcc/
    "GPL-2.0-or-later", # lib/libatomic_ops_gpl.*
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/bdwgc/libatomic_ops/releases/download/v7.10.0/libatomic_ops-7.10.0.tar.gz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"atomic_ops.h"
    assert_path_exists lib/"libatomic_ops.dll.a"
    assert_path_exists bin/"libatomic_ops-1.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/atomic_ops.pc").read
  end
end
