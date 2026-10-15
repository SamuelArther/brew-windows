class Karchive < Formula
  desc "Reading, creating, and manipulating file archives"
  homepage "https://api.kde.org/karchive-index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-karchive-6.31.0-1-any.pkg.tar.zst"
  sha256 "cda5027451ae20b942c931e3ae81514407b238da5273d60a94427472400d12e0"
  license all_of: [
    "BSD-2-Clause",
    "LGPL-2.0-only",
    "LGPL-2.0-or-later",
    any_of: ["LGPL-2.0-only", "LGPL-3.0-only"],
  ]

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://download.kde.org/stable/frameworks/"
    regex(%r{href=.*?v?(\d+(?:\.\d+)+)/?["' >]}i)
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"KF6/KArchive/kar.h"
    assert_path_exists lib/"libKF6Archive.dll.a"
    assert_path_exists bin/"libKF6Archive.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/KF6Archive.pc").read
  end
end
