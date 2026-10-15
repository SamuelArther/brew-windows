class Libnghttp2 < Formula
  desc "HTTP/2 C Library"
  homepage "https://nghttp2.org/"
  url "https://repo.msys2.org/msys/x86_64/libnghttp2-1.70.0-1-x86_64.pkg.tar.zst"
  sha256 "9b3443ff1b26a3ece9394409efca60f3ac8eee94309e5be1e6b05706f3386110"
  license "MIT"

  # Prebuilt package from MSYS2; headers and import libraries go in the keg.

  livecheck do
    url "https://github.com/nghttp2/nghttp2/releases/download/v1.70.0/nghttp2-1.70.0.tar.gz"
  end

  def install
    install_tree
    install_library "libexec/usr"
  end

  test do
    assert_path_exists bin/"msys-nghttp2-14.dll"
  end
end
