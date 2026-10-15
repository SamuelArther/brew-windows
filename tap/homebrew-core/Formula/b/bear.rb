class Bear < Formula
  desc "Generate compilation database for clang tooling"
  homepage "https://github.com/rizsotto/Bear"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-bear-4.2.2-1-any.pkg.tar.zst"
  sha256 "0059784fbe382fa5e9b18fd8dcfbb5deb8030d08533bb08bfbb3eaf8f5177b6c"
  license "GPL-3.0-or-later"

  # Prebuilt UCRT64 package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/rizsotto/Bear/archive/refs/tags/4.2.3.tar.gz"
  end

  def install
    install_tree
    install_library
  end

  test do
    assert_path_exists bin/"bear"
  end
end
