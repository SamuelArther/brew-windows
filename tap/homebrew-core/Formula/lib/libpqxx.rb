class Libpqxx < Formula
  desc "C++ connector for PostgreSQL"
  homepage "https://pqxx.org/development/libpqxx/"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-libpqxx-8.0.2-1-any.pkg.tar.zst"
  sha256 "83cff6beeeb4b9dbfc73591244190e4096f8d9769fff40ddbbff237ffe2e2b40"
  license "BSD-3-Clause"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/jtv/libpqxx/archive/refs/tags/8.0.2.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"pqxx/row.hxx"
    assert_path_exists lib/"libpqxx.dll.a"
    assert_path_exists bin/"libpqxx.dll"
    assert_match "Version: #{version}", (lib/"pkgconfig/libpqxx.pc").read
  end
end
