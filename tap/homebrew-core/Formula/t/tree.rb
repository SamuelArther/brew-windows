class Tree < Formula
  desc "Display directories as trees (with optional color/HTML output)"
  homepage "https://oldmanprogrammer.net/source.php?dir=projects/tree"
  url "https://repo.msys2.org/msys/x86_64/tree-2.3.2-1-x86_64.pkg.tar.zst"
  sha256 "edc3011c0f0d6e42f0c5fb2769990c80318289148e720b142fd8884aa893f333"
  license "GPL-2.0-or-later"

  # tree via MSYS2 (named tree, unlike Windows' tree.com).

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/tree.exe", "tree"
  end

  test do
    system bin/"tree", testpath
  end
end
