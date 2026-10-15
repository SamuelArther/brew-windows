class Duktape < Formula
  desc "Embeddable Javascript engine with compact footprint"
  homepage "https://duktape.org"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-duktape-2.7.0-2-any.pkg.tar.zst"
  sha256 "9321075fef48d29f32aab10df3b70b7e8407a01c702d01b7f4498b4d1c1fcf57"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/svaarala/duktape/releases/download/v2.7.0/duktape-2.7.0.tar.xz"
    strategy :github_latest
  end

  def install
    install_tree
    install_library
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists include/"duktape.h"
    assert_match "Version: #{version}", (lib/"pkgconfig/duktape.pc").read
  end
end
