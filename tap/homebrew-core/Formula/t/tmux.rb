class Tmux < Formula
  desc "Terminal multiplexer"
  homepage "https://tmux.github.io/"
  version "3.8"
  url "https://repo.msys2.org/msys/x86_64/tmux-3.8-1-x86_64.pkg.tar.zst"
  sha256 "8ccd83d29f0f29aa5ed283f00c4770e55731a0f36e488ed05d7a5226034c2560"
  license "ISC"

  # tmux via MSYS2. Needs a pty-backed terminal such as mintty; plain Windows consoles are not supported.

  resource "gcc-libs" do
    url "https://repo.msys2.org/msys/x86_64/gcc-libs-15.3.0-1-x86_64.pkg.tar.zst"
    sha256 "0d99a122c453c05ae21ba3dcea910f2e0d93d38ae067c677a112a315b0f3cec5"
  end

  resource "libevent" do
    url "https://repo.msys2.org/msys/x86_64/libevent-2.1.13-1-x86_64.pkg.tar.zst"
    sha256 "ce967e0bbeb4cf396a42e6cd7516b6bbcef23237bf0a55099adbdfe1e9b3fce3"
  end

  resource "libopenssl" do
    url "https://repo.msys2.org/msys/x86_64/libopenssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "05f6cc946f0dde3aecc3632007fa1d874e40720226117b750327bc2a01846325"
  end

  resource "msys2-runtime" do
    url "https://repo.msys2.org/msys/x86_64/msys2-runtime-3.6.11-3-x86_64.pkg.tar.zst"
    sha256 "b6478bd1b4cb4b2e07c45db725fee93f9424c19fa248240dcb7b7f4187cd92db"
  end

  resource "ncurses" do
    url "https://repo.msys2.org/msys/x86_64/ncurses-6.6-2-x86_64.pkg.tar.zst"
    sha256 "828c92f09b17f00d3b49362fcfa4affa58e5e50e238f290ea36eb7a690da3acb"
  end

  resource "openssl" do
    url "https://repo.msys2.org/msys/x86_64/openssl-3.6.5-1-x86_64.pkg.tar.zst"
    sha256 "4dc7c1199010b26d6382c59d8b16a9dbc857d314d76d9668f0f333bc5fecdd38"
  end

  def install
    msys2_install
    msys2_bin "usr/bin/tmux.exe", "tmux"
  end

  test do
    assert_match version.to_s, shell_output("#{bin}/tmux -V")
  end
end
