class Libngtcp2 < Formula
  desc "IETF QUIC protocol implementation"
  homepage "https://nghttp2.org/ngtcp2/"
  url "https://repo.msys2.org/msys/x86_64/libngtcp2-1.25.0-1-x86_64.pkg.tar.zst"
  sha256 "a4c389078632e64ac2b320473651e80d44c97f9fed7a746a232d95b400b14fe8"
  license "MIT"
  revision 1

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/ngtcp2/ngtcp2/releases/download/v1.25.0/ngtcp2-1.25.0.tar.xz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"msys-ngtcp2-16.dll"
  end
end
