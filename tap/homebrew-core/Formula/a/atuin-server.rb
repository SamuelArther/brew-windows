class AtuinServer < Formula
  desc "Sync server for atuin - Improved shell history for zsh, bash, fish and nushell"
  homepage "https://atuin.sh"
  url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-atuin-server-18.23.0-1-any.pkg.tar.zst"
  sha256 "fba9f058c4d84c96f6359466ad301cbb7114cad3373a5b7a17149411b357b61d"
  license "MIT"

  # Prebuilt UCRT64 package from MSYS2; its runtime packages are pinned as resources.

  livecheck do
    url "https://github.com/atuinsh/atuin/releases/download/v18.23.0/source.tar.gz"
  end

  resource "mingw-w64-ucrt-x86_64-atuin" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-atuin-18.23.0-1-any.pkg.tar.zst"
    sha256 "cbed3f4df4cc06eb4830e6733bed5c46958ebedaaa9611f5cf790fd85cc8fba2"
  end

  resource "mingw-w64-ucrt-x86_64-sqlite3" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-sqlite3-3.54.0-1-any.pkg.tar.zst"
    sha256 "fc7100490316733bbb96d5633d305a5d6c579200c321969cfa21d5a128055e65"
  end

  resource "mingw-w64-ucrt-x86_64-tcl" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-tcl-8.6.18-1-any.pkg.tar.zst"
    sha256 "0c3a50ea5be8c5190e58a1fdc46c09f8113ad1b855745875646b7a78a02215df"
  end

  resource "mingw-w64-ucrt-x86_64-zlib" do
    url "https://repo.msys2.org/mingw/ucrt64/mingw-w64-ucrt-x86_64-zlib-1.3.2-2-any.pkg.tar.zst"
    sha256 "841401182976d2f9e17e5c0ebaac51f2a8014140ea53d67625e91c8fb3c85ea0"
  end

  def install
    msys2_install
    msys2_bin "ucrt64/bin/atuin-server.exe", "atuin-server"
  end

  test do
    assert_match "Atuin sync server", shell_output("#{bin}/atuin-server 2>&1", 2)
  end
end
