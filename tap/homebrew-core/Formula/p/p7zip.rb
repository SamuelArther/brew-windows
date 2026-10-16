class P7zip < Formula
  desc "7-Zip (high compression file archiver) implementation"
  homepage "https://github.com/p7zip-project/p7zip"
  url "https://repo.msys2.org/msys/x86_64/p7zip-17.06-1-x86_64.pkg.tar.zst"
  sha256 "ff18fb4ed239d97f94d61a409b3db3b626015b6ba16b70e267652bb9efc54faa"
  license all_of: ["LGPL-2.1-or-later", "GPL-2.0-or-later"]

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/p7zip-project/p7zip/archive/refs/tags/v17.06.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    # No system C compiler on Windows to build homebrew-core's test program;
    # check the installed files instead.
    assert_path_exists bin/"7z"
  end
end
