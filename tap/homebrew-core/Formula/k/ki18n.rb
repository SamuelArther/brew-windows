class Ki18n < Formula
  desc "KDE Gettext-based UI text internationalization"
  homepage "https://api.kde.org/ki18n-index.html"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-ki18n-6.31.0-1-any.pkg.tar.zst"
  sha256 "05565fc1f5e58206eef811474061307cee256eb174283436c0d3cb9608cbf63d"
  license all_of: [
    "BSD-3-Clause",
    "LGPL-2.0-or-later",
    any_of: ["LGPL-2.1-only", "LGPL-3.0-only"],
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
    assert_path_exists include/"KF6/KI18n/kuitsetup.h"
    assert_path_exists lib/"libKF6I18n.dll.a"
    assert_path_exists bin/"libKF6I18n.dll"
  end
end
